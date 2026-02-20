using System.IdentityModel.Tokens.Jwt;
using EchoHub.Core.Models;
using EchoHub.Server.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EchoHub.Tests;

public class JwtTokenServiceTests
{
    private const string TestSecret = "this_is_a_test_secret_key_that_is_long_enough_for_hmac_sha256";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private static JwtTokenService CreateService(
        string? secret = null, string? issuer = null, string? audience = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret ?? TestSecret,
                ["Jwt:Issuer"] = issuer ?? TestIssuer,
                ["Jwt:Audience"] = audience ?? TestAudience,
            })
            .Build();

        return new JwtTokenService(config);
    }

    private static User CreateUser(
        string username = "alice",
        ServerRole role = ServerRole.Member,
        string? displayName = null) => new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = "hash",
            DisplayName = displayName,
            Role = role,
        };

    // ── Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_MissingSecret_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
    }

    [Fact]
    public void Constructor_MissingIssuer_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:Audience"] = TestAudience,
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
    }

    [Fact]
    public void Constructor_MissingAudience_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:Issuer"] = TestIssuer,
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
    }

    // ── GenerateAccessToken ───────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var service = CreateService();
        var user = CreateUser(username: "bob", role: ServerRole.Admin, displayName: "Bob Smith");

        var (token, _) = service.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("bob", jwt.Claims.First(c => c.Type == "username").Value);
        Assert.Equal("Bob Smith", jwt.Claims.First(c => c.Type == "display_name").Value);
        Assert.Equal("Admin", jwt.Claims.First(c => c.Type == "role").Value);
        Assert.NotNull(jwt.Claims.FirstOrDefault(c => c.Type == "jti"));
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn15Minutes()
    {
        var service = CreateService();
        var user = CreateUser();

        var (_, expiresAt) = service.GenerateAccessToken(user);
        var diff = expiresAt - DateTimeOffset.UtcNow;

        // Should be approximately 15 minutes (allow 30s tolerance)
        Assert.InRange(diff.TotalMinutes, 14.5, 15.5);
    }

    [Fact]
    public void GenerateAccessToken_DifferentTokensForSameUser()
    {
        var service = CreateService();
        var user = CreateUser();

        var (token1, _) = service.GenerateAccessToken(user);
        var (token2, _) = service.GenerateAccessToken(user);

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateAccessToken_DisplayNameFallsBackToUsername()
    {
        var service = CreateService();
        var user = CreateUser(username: "alice"); // DisplayName is null

        var (token, _) = service.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("alice", jwt.Claims.First(c => c.Type == "display_name").Value);
    }

    // ── GenerateRefreshToken ──────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_Returns88CharBase64()
    {
        var token = JwtTokenService.GenerateRefreshToken();

        // 64 bytes → 88 base64 characters
        Assert.Equal(88, token.Length);
        // Should be valid base64
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_UniqueBetweenCalls()
    {
        var token1 = JwtTokenService.GenerateRefreshToken();
        var token2 = JwtTokenService.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    // ── HashToken ─────────────────────────────────────────────────────

    [Fact]
    public void HashToken_DeterministicForSameInput()
    {
        var hash1 = JwtTokenService.HashToken("test-token");
        var hash2 = JwtTokenService.HashToken("test-token");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentForDifferentInput()
    {
        var hash1 = JwtTokenService.HashToken("token-a");
        var hash2 = JwtTokenService.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashToken_ReturnsBase64String()
    {
        var hash = JwtTokenService.HashToken("test-token");

        // SHA256 → 32 bytes → 44 base64 characters
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(32, bytes.Length);
    }
}
