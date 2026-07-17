using EchoHub.Core.Constants;
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
/// <see cref="UserService"/> authentication, profile reads, profile-update validation, and
/// avatar. (Registration-gate behavior is covered by <see cref="UserServiceRegistrationTests"/>.)
/// Runs against a real SQLite in-memory database with real BCrypt hashing.
/// </summary>
public sealed class UserServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<EchoHubDbContext>(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<EchoHubDbContext>().Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Server:Registration"] = "open" })
            .Build();
        _service = new UserService(_provider.GetRequiredService<IServiceScopeFactory>(), config);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private async Task<UserProfileDto> RegisterAsync(string username = "alice", string password = "password1")
    {
        var result = await _service.RegisterUserAsync(username, password);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.User!;
    }

    private EchoHubDbContext Db() =>
        _provider.GetRequiredService<IServiceScopeFactory>()
            .CreateScope().ServiceProvider.GetRequiredService<EchoHubDbContext>();

    // ── Authentication ────────────────────────────────────────────────

    [Fact]
    public async Task Authenticate_CorrectCredentials_Succeeds()
    {
        await RegisterAsync("alice", "password1");

        var result = await _service.AuthenticateUserAsync("alice", "password1");

        Assert.True(result.IsSuccess);
        Assert.Equal("alice", result.User!.Username);
    }

    [Fact]
    public async Task Authenticate_IsCaseInsensitiveOnUsername()
    {
        await RegisterAsync("alice", "password1");

        var result = await _service.AuthenticateUserAsync("ALICE", "password1");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_InvalidCredentials()
    {
        await RegisterAsync("alice", "password1");

        var result = await _service.AuthenticateUserAsync("alice", "wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Authenticate_UnknownUser_InvalidCredentials()
    {
        var result = await _service.AuthenticateUserAsync("nobody", "password1");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Authenticate_EmptyInput_ValidationFailed()
    {
        var result = await _service.AuthenticateUserAsync("", "");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task Authenticate_BannedUser_ReturnsBanned()
    {
        var profile = await RegisterAsync("alice", "password1");
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
            var user = await db.Users.FindAsync(profile.Id);
            user!.IsBanned = true;
            await db.SaveChangesAsync();
        }

        var result = await _service.AuthenticateUserAsync("alice", "password1");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.Banned, result.Error);
    }

    // ── Profile reads ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfile_KnownUser_ReturnsProfile()
    {
        await RegisterAsync("alice", "password1");

        var profile = await _service.GetUserProfileAsync("alice");

        Assert.NotNull(profile);
        Assert.Equal("alice", profile!.Username);
    }

    [Fact]
    public async Task GetUserProfile_UnknownUser_ReturnsNull()
    {
        Assert.Null(await _service.GetUserProfileAsync("ghost"));
    }

    [Fact]
    public async Task GetUserById_RoundTrips()
    {
        var profile = await RegisterAsync("alice", "password1");

        var byId = await _service.GetUserByIdAsync(profile.Id);

        Assert.NotNull(byId);
        Assert.Equal("alice", byId!.Username);
    }

    [Fact]
    public async Task GetUserById_UnknownId_ReturnsNull()
    {
        Assert.Null(await _service.GetUserByIdAsync(Guid.NewGuid()));
    }

    // ── Profile updates ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ValidFields_Persisted()
    {
        var profile = await RegisterAsync("alice", "password1");

        var result = await _service.UpdateProfileAsync(profile.Id, "Alice A", "hi there", "#FF5500");

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice A", result.User!.DisplayName);
        Assert.Equal("hi there", result.User.Bio);
        Assert.Equal("#FF5500", result.User.NicknameColor);
    }

    [Fact]
    public async Task UpdateProfile_InvalidHexColor_Rejected()
    {
        var profile = await RegisterAsync("alice", "password1");

        var result = await _service.UpdateProfileAsync(profile.Id, null, null, "red");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task UpdateProfile_EmptyColor_ClearsIt()
    {
        var profile = await RegisterAsync("alice", "password1");
        await _service.UpdateProfileAsync(profile.Id, null, null, "#FF5500");

        var result = await _service.UpdateProfileAsync(profile.Id, null, null, "");

        Assert.True(result.IsSuccess);
        Assert.Null(result.User!.NicknameColor);
    }

    [Fact]
    public async Task UpdateProfile_DisplayNameTooLong_Rejected()
    {
        var profile = await RegisterAsync("alice", "password1");

        var result = await _service.UpdateProfileAsync(
            profile.Id, new string('x', ValidationConstants.MaxDisplayNameLength + 1), null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task UpdateProfile_BioTooLong_Rejected()
    {
        var profile = await RegisterAsync("alice", "password1");

        var result = await _service.UpdateProfileAsync(
            profile.Id, null, new string('x', ValidationConstants.MaxBioLength + 1), null);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task UpdateProfile_UnknownUser_NotFound()
    {
        var result = await _service.UpdateProfileAsync(Guid.NewGuid(), "x", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.NotFound, result.Error);
    }

    // ── Avatar ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAvatar_Persisted()
    {
        var profile = await RegisterAsync("alice", "password1");

        var result = await _service.SetAvatarAsync(profile.Id, "{F:FF0000}art");

        Assert.True(result.IsSuccess);
        var stored = await Db().Users.FindAsync(profile.Id);
        Assert.Equal("{F:FF0000}art", stored!.AvatarAscii);
    }

    [Fact]
    public async Task SetAvatar_UnknownUser_NotFound()
    {
        var result = await _service.SetAvatarAsync(Guid.NewGuid(), "art");

        Assert.False(result.IsSuccess);
        Assert.Equal(UserError.NotFound, result.Error);
    }
}
