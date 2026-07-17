using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Config;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using EchoHub.Server.Services.ServerLogs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// System-channel behavior of <see cref="ChannelService"/> (the live server-log room):
/// name reservation, role-gated visibility + top ordering, delete protection, and the
/// join-time role gate with auto-recreation. Runs against a real SQLite in-memory database.
/// </summary>
public sealed class ChannelServiceSystemChannelTests : IDisposable
{
    private const string LogRoom = "server-logs";

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly ServerLogsService _serverLogs = new(new ServerLogsOptions
    {
        Enabled = true,
        RoomName = LogRoom,
        MinRole = ServerRole.Mod,
    });

    public ChannelServiceSystemChannelTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<EchoHubDbContext>(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<EchoHubDbContext>().Database.EnsureCreated();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private ChannelService CreateService() => new(
        _provider.GetRequiredService<IServiceScopeFactory>(),
        new PresenceTracker(),
        // Disable the spam guard so channel-create throttling never interferes with assertions.
        new SpamGuard(new SpamOptions { Enabled = false }),
        _serverLogs,
        NullLogger<ChannelService>.Instance);

    private async Task<Guid> SeedUserAsync(ServerRole role)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user-" + Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "x",
            Role = role,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private EchoHubDbContext Db() =>
        _provider.GetRequiredService<IServiceScopeFactory>()
            .CreateScope().ServiceProvider.GetRequiredService<EchoHubDbContext>();

    // ── Name reservation ──────────────────────────────────────────────

    [Fact]
    public async Task CreateChannel_WithReservedLogRoomName_IsRejected()
    {
        var service = CreateService();
        var userId = await SeedUserAsync(ServerRole.Owner);

        var result = await service.CreateChannelAsync(userId, LogRoom, null, isPublic: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.ValidationFailed, result.Error);
    }

    // ── EnsureSystemChannelAsync ──────────────────────────────────────

    [Fact]
    public async Task EnsureSystemChannel_CreatesPrivateSystemChannel()
    {
        var service = CreateService();

        var dto = await service.EnsureSystemChannelAsync(LogRoom, "Live server logs");

        Assert.True(dto.IsSystem);
        Assert.False(dto.IsPublic);

        var stored = await Db().Channels.SingleAsync(c => c.Name == LogRoom);
        Assert.True(stored.IsSystem);
        Assert.False(stored.IsPublic);
    }

    [Fact]
    public async Task EnsureSystemChannel_IsIdempotent()
    {
        var service = CreateService();

        var first = await service.EnsureSystemChannelAsync(LogRoom);
        var second = await service.EnsureSystemChannelAsync(LogRoom);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await Db().Channels.CountAsync(c => c.Name == LogRoom));
    }

    [Fact]
    public async Task EnsureSystemChannel_ClaimsExistingRegularChannelOfSameName()
    {
        // A regular channel squatting on the name (created while the feature was off) must be
        // reclaimed so server content never streams into a user-owned room.
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
            db.Channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                Name = LogRoom,
                IsPublic = true,
                IsSystem = false,
                PasswordHash = "hash",
                CreatedByUserId = Guid.NewGuid(),
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService();
        var dto = await service.EnsureSystemChannelAsync(LogRoom);

        Assert.True(dto.IsSystem);
        var stored = await Db().Channels.SingleAsync(c => c.Name == LogRoom);
        Assert.True(stored.IsSystem);
        Assert.False(stored.IsPublic);
        Assert.Null(stored.PasswordHash);
    }

    // ── Visibility + ordering ─────────────────────────────────────────

    [Fact]
    public async Task GetChannels_HidesSystemChannelFromMembers()
    {
        var service = CreateService();
        await service.EnsureSystemChannelAsync(LogRoom);
        var memberId = await SeedUserAsync(ServerRole.Member);

        var page = await service.GetChannelsAsync(memberId, 0, 50);

        Assert.DoesNotContain(page.Items, c => c.Name == LogRoom);
    }

    [Fact]
    public async Task GetChannels_ShowsSystemChannelToMods_PinnedAtTop()
    {
        var service = CreateService();
        await service.EnsureSystemChannelAsync(LogRoom);
        var modId = await SeedUserAsync(ServerRole.Mod);

        var page = await service.GetChannelsAsync(modId, 0, 50);

        var logRoom = Assert.Single(page.Items, c => c.Name == LogRoom);
        Assert.True(logRoom.IsSystem);
        // Sorts above 'general' (auto-created, alphabetically after 's' would normally lose).
        Assert.Equal(LogRoom, page.Items[0].Name);
    }

    // ── Delete protection ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteChannel_SystemChannel_IsRefused()
    {
        var service = CreateService();
        await service.EnsureSystemChannelAsync(LogRoom);
        var ownerId = await SeedUserAsync(ServerRole.Owner);

        var result = await service.DeleteChannelAsync(ownerId, LogRoom);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Protected, result.Error);
        Assert.True(await Db().Channels.AnyAsync(c => c.Name == LogRoom));
    }

    // ── Join-time role gate + auto-recreate ───────────────────────────

    [Fact]
    public async Task EnsureMembership_LogRoom_RejectsMember()
    {
        var service = CreateService();
        await service.EnsureSystemChannelAsync(LogRoom);
        var memberId = await SeedUserAsync(ServerRole.Member);

        var (success, error, _) = await service.EnsureChannelMembershipAsync(memberId, LogRoom);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task EnsureMembership_LogRoom_AllowsModAndRecreatesIfMissing()
    {
        // No prior EnsureSystemChannelAsync — a Mod joining a missing log room recreates it.
        var service = CreateService();
        var modId = await SeedUserAsync(ServerRole.Mod);

        var (success, error, _) = await service.EnsureChannelMembershipAsync(modId, LogRoom);

        Assert.True(success);
        Assert.Null(error);
        var stored = await Db().Channels.SingleAsync(c => c.Name == LogRoom);
        Assert.True(stored.IsSystem);
    }
}
