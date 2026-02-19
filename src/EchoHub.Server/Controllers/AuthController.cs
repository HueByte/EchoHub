using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Auth;
using EchoHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly EchoHubDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(EchoHubDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ErrorResponse("Username and password are required."));

        if (!ValidationConstants.UsernameRegex().IsMatch(request.Username))
            return BadRequest(new ErrorResponse("Username must be 3-50 characters and contain only letters, digits, underscores, or hyphens."));

        if (request.Password.Length < 6)
            return BadRequest(new ErrorResponse("Password must be at least 6 characters."));

        if (request.Password.Length > ValidationConstants.MaxPasswordLength)
            return BadRequest(new ErrorResponse($"Password must not exceed {ValidationConstants.MaxPasswordLength} characters."));

        var normalizedUsername = request.Username.ToLowerInvariant().Trim();

        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername))
            return Conflict(new ErrorResponse("Username is already taken."));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName?.Trim(),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = JwtTokenService.HashToken(refreshToken),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.Add(JwtTokenService.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, refreshToken, expiresAt, user.Username, user.DisplayName, user.NicknameColor));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ErrorResponse("Username and password are required."));

        var normalizedUsername = request.Username.ToLowerInvariant().Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponse("Invalid username or password."));

        user.LastSeenAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = JwtTokenService.HashToken(refreshToken),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.Add(JwtTokenService.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, refreshToken, expiresAt, user.Username, user.DisplayName, user.NicknameColor));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new ErrorResponse("Refresh token is required."));

        var tokenHash = JwtTokenService.HashToken(request.RefreshToken);
        var storedToken = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null)
            return Unauthorized(new ErrorResponse("Invalid or expired refresh token."));

        // Revoke old refresh token (rotation)
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var user = storedToken.User;
        user.LastSeenAt = DateTimeOffset.UtcNow;

        // Issue new token pair
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user);
        var newRefreshToken = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = JwtTokenService.HashToken(newRefreshToken),
            UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.Add(JwtTokenService.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, newRefreshToken, expiresAt, user.Username, user.DisplayName, user.NicknameColor));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new ErrorResponse("Refresh token is required."));

        var tokenHash = JwtTokenService.HashToken(request.RefreshToken);
        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}
