using EchoHub.Core.Contracts;
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
    private readonly IUserService _userService;

    public AuthController(EchoHubDbContext db, JwtTokenService jwt, IUserService userService)
    {
        _db = db;
        _jwt = jwt;
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterUserAsync(request.Username, request.Password, request.DisplayName);
        if (!result.IsSuccess)
            return MapUserError(result);

        var profile = result.User!;
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(profile);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = JwtTokenService.HashToken(refreshToken),
            UserId = profile.Id,
            ExpiresAt = DateTimeOffset.UtcNow.Add(JwtTokenService.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, refreshToken, expiresAt, profile.Username, profile.DisplayName, profile.NicknameColor));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.AuthenticateUserAsync(request.Username, request.Password);
        if (!result.IsSuccess)
            return MapUserError(result);

        var profile = result.User!;
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(profile);
        var refreshToken = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = JwtTokenService.HashToken(refreshToken),
            UserId = profile.Id,
            ExpiresAt = DateTimeOffset.UtcNow.Add(JwtTokenService.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, refreshToken, expiresAt, profile.Username, profile.DisplayName, profile.NicknameColor));
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

    private IActionResult MapUserError(UserOperationResult result) => result.Error switch
    {
        UserError.ValidationFailed => BadRequest(new ErrorResponse(result.ErrorMessage!)),
        UserError.AlreadyExists => Conflict(new ErrorResponse(result.ErrorMessage!)),
        UserError.NotFound => NotFound(new ErrorResponse(result.ErrorMessage!)),
        UserError.InvalidCredentials => Unauthorized(new ErrorResponse(result.ErrorMessage!)),
        UserError.Banned => Unauthorized(new ErrorResponse(result.ErrorMessage!)),
        _ => BadRequest(new ErrorResponse(result.ErrorMessage ?? "Unknown error.")),
    };
}
