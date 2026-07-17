using EchoHub.Core.Constants;
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
/// General <see cref="ChannelService"/> CRUD, validation, password gates, and role/creator
/// authorization. (System-channel behavior lives in <see cref="ChannelServiceSystemChannelTests"/>.)
/// Runs against a real SQLite in-memory database so the guarded queries and FK relationships
/// behave as in production.
/// </summary>
public sealed class ChannelServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    // Default options → reserved name "server-logs"; feature enabled but never targeted here.
    private readonly ServerLogsService _serverLogs = new(new ServerLogsOptions());

    public ChannelServiceTests()
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
        new SpamGuard(new SpamOptions { Enabled = false }),
        _serverLogs,
        NullLogger<ChannelService>.Instance);

    private async Task<Guid> SeedUserAsync(ServerRole role = ServerRole.Member)
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

    // ── Create: happy path + membership ───────────────────────────────

    [Fact]
    public async Task CreateChannel_Valid_SucceedsAndAddsCreatorMembership()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, "dev-talk", "About dev", isPublic: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("dev-talk", result.Channel!.Name);
        Assert.True(result.Channel.IsPublic);

        var channel = await Db().Channels.SingleAsync(c => c.Name == "dev-talk");
        Assert.True(await Db().ChannelMemberships.AnyAsync(m => m.ChannelId == channel.Id && m.UserId == creator));
    }

    [Fact]
    public async Task CreateChannel_LowercasesName()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, "DevTalk", null, isPublic: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("devtalk", result.Channel!.Name);
    }

    // ── Create: validation ────────────────────────────────────────────

    [Theory]
    [InlineData("a")]              // too short (< 2)
    [InlineData("has space")]      // invalid character
    [InlineData("bang!")]          // invalid character
    public async Task CreateChannel_InvalidName_Rejected(string name)
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, name, null, isPublic: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task CreateChannel_DuplicateName_Rejected()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "dupe", null, isPublic: true);

        var result = await service.CreateChannelAsync(creator, "dupe", null, isPublic: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.AlreadyExists, result.Error);
    }

    [Fact]
    public async Task CreateChannel_ShortPassword_Rejected()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, "locked", null, isPublic: true, password: "ab");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task CreateChannel_WithPassword_IsMarkedProtectedAndHashed()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, "locked", null, isPublic: true, password: "secret");

        Assert.True(result.IsSuccess);
        Assert.True(result.Channel!.IsProtected);
        var stored = await Db().Channels.SingleAsync(c => c.Name == "locked");
        Assert.NotNull(stored.PasswordHash);
        Assert.NotEqual("secret", stored.PasswordHash); // hashed, not plaintext
    }

    [Fact]
    public async Task CreateChannel_EncryptionEnvelopeWithoutPassword_Rejected()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();

        var result = await service.CreateChannelAsync(creator, "e2e", null, isPublic: false,
            password: null, encryptionSalt: "salt", wrappedRoomKey: "wrapped");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task CreateChannel_EncryptedChannel_ExposesCryptoMetadataButNotKey()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "e2e", null, isPublic: false,
            password: "passphrase", encryptionSalt: "the-salt", wrappedRoomKey: "the-wrapped-key");

        var crypto = await service.GetChannelCryptoAsync("e2e");
        var (salt, wrapped) = await service.GetChannelKeyEnvelopeAsync("e2e");

        Assert.True(crypto!.IsEncrypted);
        Assert.Equal("the-salt", crypto.EncryptionSalt);
        Assert.Equal("the-salt", salt);
        Assert.Equal("the-wrapped-key", wrapped);
    }

    // ── Visibility ────────────────────────────────────────────────────

    [Fact]
    public async Task GetChannels_ShowsPublicAndOwnPrivate_HidesOthersPrivate()
    {
        var service = CreateService();
        var owner = await SeedUserAsync();
        var outsider = await SeedUserAsync();
        await service.CreateChannelAsync(owner, "public-room", null, isPublic: true);
        await service.CreateChannelAsync(owner, "private-room", null, isPublic: false);

        var outsiderView = await service.GetChannelsAsync(outsider, 0, 50);

        Assert.Contains(outsiderView.Items, c => c.Name == "public-room");
        Assert.DoesNotContain(outsiderView.Items, c => c.Name == "private-room");

        var ownerView = await service.GetChannelsAsync(owner, 0, 50);
        Assert.Contains(ownerView.Items, c => c.Name == "private-room");
    }

    // ── Membership + password gate ────────────────────────────────────

    [Fact]
    public async Task EnsureMembership_ProtectedChannel_RequiresCorrectPassword()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "vault", null, isPublic: true, password: "opensesame");
        var joiner = await SeedUserAsync();

        var noPassword = await service.EnsureChannelMembershipAsync(joiner, "vault");
        Assert.False(noPassword.Success);
        Assert.True(noPassword.PasswordRequired);

        var wrongPassword = await service.EnsureChannelMembershipAsync(joiner, "vault", "nope");
        Assert.False(wrongPassword.Success);
        Assert.True(wrongPassword.PasswordRequired);

        var correct = await service.EnsureChannelMembershipAsync(joiner, "vault", "opensesame");
        Assert.True(correct.Success);
    }

    [Fact]
    public async Task EnsureMembership_ExistingMember_NoPasswordNeeded()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "vault", null, isPublic: true, password: "opensesame");

        // Creator already has membership from creation → re-join needs no password.
        var result = await service.EnsureChannelMembershipAsync(creator, "vault");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task EnsureMembership_NonexistentChannel_Fails()
    {
        var service = CreateService();
        var user = await SeedUserAsync();

        var result = await service.EnsureChannelMembershipAsync(user, "ghost");

        Assert.False(result.Success);
        Assert.False(result.PasswordRequired);
    }

    [Fact]
    public async Task EnsureMembership_DefaultChannel_AutoRecreatedIfMissing()
    {
        var service = CreateService();
        var user = await SeedUserAsync();

        var result = await service.EnsureChannelMembershipAsync(user, HubConstants.DefaultChannel);

        Assert.True(result.Success);
        Assert.True(await Db().Channels.AnyAsync(c => c.Name == HubConstants.DefaultChannel));
    }

    // ── Topic ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTopic_Creator_Succeeds()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "room", null, isPublic: true);

        var result = await service.UpdateTopicAsync(creator, "room", "new topic");

        Assert.True(result.IsSuccess);
        Assert.Equal("new topic", result.Channel!.Topic);
    }

    [Fact]
    public async Task UpdateTopic_NonCreator_Forbidden()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        var other = await SeedUserAsync(ServerRole.Admin); // even an admin isn't the creator
        await service.CreateChannelAsync(creator, "room", null, isPublic: true);

        var result = await service.UpdateTopicAsync(other, "room", "hijack");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Forbidden, result.Error);
    }

    [Fact]
    public async Task UpdateTopic_TooLong_Rejected()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "room", null, isPublic: true);

        var result = await service.UpdateTopicAsync(creator, "room",
            new string('x', ValidationConstants.MaxChannelTopicLength + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.ValidationFailed, result.Error);
    }

    // ── Password management ───────────────────────────────────────────

    [Fact]
    public async Task SetChannelPassword_Admin_CanSetOnAnothersChannel()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        var admin = await SeedUserAsync(ServerRole.Admin);
        await service.CreateChannelAsync(creator, "room", null, isPublic: true);

        var result = await service.SetChannelPasswordAsync(admin, "room", "newpass");

        Assert.True(result.IsSuccess);
        Assert.True(result.Channel!.IsProtected);
    }

    [Fact]
    public async Task SetChannelPassword_UnprivilegedNonCreator_Forbidden()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        var member = await SeedUserAsync(ServerRole.Member);
        await service.CreateChannelAsync(creator, "room", null, isPublic: true);

        var result = await service.SetChannelPasswordAsync(member, "room", "newpass");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Forbidden, result.Error);
    }

    [Fact]
    public async Task SetChannelPassword_EncryptedChannel_Refused()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "e2e", null, isPublic: false,
            password: "passphrase", encryptionSalt: "salt", wrappedRoomKey: "wrapped");

        var result = await service.SetChannelPasswordAsync(creator, "e2e", "newpass");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Protected, result.Error);
    }

    // ── Delete ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteChannel_DefaultChannel_Protected()
    {
        var service = CreateService();
        var owner = await SeedUserAsync(ServerRole.Owner);
        // GetChannels auto-creates #general.
        await service.GetChannelsAsync(owner, 0, 50);

        var result = await service.DeleteChannelAsync(owner, HubConstants.DefaultChannel);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Protected, result.Error);
    }

    [Fact]
    public async Task DeleteChannel_Creator_Succeeds()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        await service.CreateChannelAsync(creator, "temp", null, isPublic: true);

        var result = await service.DeleteChannelAsync(creator, "temp");

        Assert.True(result.IsSuccess);
        Assert.False(await Db().Channels.AnyAsync(c => c.Name == "temp"));
    }

    [Fact]
    public async Task DeleteChannel_UnprivilegedNonCreator_Forbidden()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        var member = await SeedUserAsync(ServerRole.Member);
        await service.CreateChannelAsync(creator, "temp", null, isPublic: true);

        var result = await service.DeleteChannelAsync(member, "temp");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChannelError.Forbidden, result.Error);
    }

    // ── Queries ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetChannelByName_UnknownChannel_ReturnsNull()
    {
        var service = CreateService();

        Assert.Null(await service.GetChannelByNameAsync("nope"));
    }

    [Fact]
    public async Task GetChannelMeta_ReturnsMessageCountAndFlags()
    {
        var service = CreateService();
        var creator = await SeedUserAsync();
        var create = await service.CreateChannelAsync(creator, "room", null, isPublic: true, password: "secret");
        var channelId = create.Channel!.Id;

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
            db.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                Content = "hello",
                SentAt = DateTimeOffset.UtcNow,
                ChannelId = channelId,
                SenderUserId = creator,
                SenderUsername = "someone",
            });
            await db.SaveChangesAsync();
        }

        var meta = await service.GetChannelMetaAsync("room");

        Assert.NotNull(meta);
        Assert.Equal(1, meta!.MessageCount);
        Assert.True(meta.IsProtected);
        Assert.False(meta.IsEncrypted);
    }
}
