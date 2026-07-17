using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// Registration gate tests: open/invite/closed modes, invite-code consumption,
/// first-user bootstrap, and the reserved tombstone username. Runs against a real
/// SQLite in-memory database because the gate includes a guarded UPDATE.
/// </summary>
public sealed class UserServiceRegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public UserServiceRegistrationTests()
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

    private UserService CreateService(string? registrationMode = null)
    {
        var settings = new Dictionary<string, string?>();
        if (registrationMode is not null)
            settings["Server:Registration"] = registrationMode;

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new UserService(_provider.GetRequiredService<IServiceScopeFactory>(), config);
    }

    private EchoHubDbContext Db()
    {
        // Root-scope context is fine here: the connection is shared, EnsureCreated ran
        return _provider.GetRequiredService<IServiceScopeFactory>()
            .CreateScope().ServiceProvider.GetRequiredService<EchoHubDbContext>();
    }

    private async Task<InviteCode> SeedInviteAsync(int maxUses = 1, DateTimeOffset? expiresAt = null, int useCount = 0)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
        var invite = new InviteCode
        {
            Id = Guid.NewGuid(),
            Code = "TEST-CODE",
            CreatedByUserId = Guid.NewGuid(),
            CreatedByUsername = "admin",
            ExpiresAt = expiresAt,
            MaxUses = maxUses,
            UseCount = useCount,
        };
        db.InviteCodes.Add(invite);
        await db.SaveChangesAsync();
        return invite;
    }

    private async Task SeedOwnerAsync()
    {
        // First registration is always allowed and becomes Owner
        var service = CreateService("open");
        var result = await service.RegisterUserAsync("owner", "password123");
        Assert.True(result.IsSuccess);
    }

    // ── Open mode ─────────────────────────────────────────────────────

    [Fact]
    public async Task OpenMode_RegistersWithoutCode()
    {
        await SeedOwnerAsync();
        var service = CreateService("open");

        var result = await service.RegisterUserAsync("alice", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal(ServerRole.Member, result.User!.Role);
    }

    [Fact]
    public async Task DefaultMode_IsOpen()
    {
        await SeedOwnerAsync();
        var service = CreateService(registrationMode: null);

        var result = await service.RegisterUserAsync("alice", "password123");

        Assert.True(result.IsSuccess);
    }

    // ── Closed mode ───────────────────────────────────────────────────

    [Fact]
    public async Task ClosedMode_RefusesRegistration()
    {
        await SeedOwnerAsync();
        var service = CreateService("closed");

        var result = await service.RegisterUserAsync("alice", "password123");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.ValidationFailed, result.Error);
        Assert.Contains("closed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClosedMode_FirstUserBootstrap_StillAllowed()
    {
        var service = CreateService("closed");

        var result = await service.RegisterUserAsync("owner", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal(ServerRole.Owner, result.User!.Role);
    }

    // ── Invite mode ───────────────────────────────────────────────────

    [Fact]
    public async Task InviteMode_NoCode_Refused()
    {
        await SeedOwnerAsync();
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("alice", "password123");

        Assert.False(result.IsSuccess);
        Assert.Contains("invite", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InviteMode_ValidCode_RegistersAndConsumesUse()
    {
        await SeedOwnerAsync();
        await SeedInviteAsync(maxUses: 2);
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("alice", "password123", inviteCode: "test-code");

        Assert.True(result.IsSuccess);
        using var db = Db();
        Assert.Equal(1, (await db.InviteCodes.SingleAsync()).UseCount);
    }

    [Fact]
    public async Task InviteMode_ExhaustedCode_Refused()
    {
        await SeedOwnerAsync();
        await SeedInviteAsync(maxUses: 1, useCount: 1);
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("alice", "password123", inviteCode: "TEST-CODE");

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid invite", result.ErrorMessage);
    }

    [Fact]
    public async Task InviteMode_ExpiredCode_Refused()
    {
        await SeedOwnerAsync();
        await SeedInviteAsync(expiresAt: DateTimeOffset.UtcNow.AddHours(-1));
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("alice", "password123", inviteCode: "TEST-CODE");

        Assert.False(result.IsSuccess);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InviteMode_WrongCode_Refused()
    {
        await SeedOwnerAsync();
        await SeedInviteAsync();
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("alice", "password123", inviteCode: "WRONG-ONE");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task InviteMode_UsernameTaken_DoesNotConsumeCode()
    {
        await SeedOwnerAsync();
        await SeedInviteAsync(maxUses: 1);
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("owner", "password123", inviteCode: "TEST-CODE");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.AlreadyExists, result.Error);
        using var db = Db();
        Assert.Equal(0, (await db.InviteCodes.SingleAsync()).UseCount);
    }

    [Fact]
    public async Task InviteMode_FirstUserBootstrap_NeedsNoCode()
    {
        var service = CreateService("invite");

        var result = await service.RegisterUserAsync("owner", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal(ServerRole.Owner, result.User!.Role);
    }

    // ── Reserved username ─────────────────────────────────────────────

    [Fact]
    public async Task ReservedTombstoneUsername_Refused()
    {
        await SeedOwnerAsync();
        var service = CreateService("open");

        var result = await service.RegisterUserAsync("deleted-user", "password123");

        Assert.False(result.IsSuccess);
        Assert.Contains("reserved", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
