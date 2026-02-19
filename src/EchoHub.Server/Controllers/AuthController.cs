using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Auth;
using EchoHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(EchoHubDbContext db, JwtTokenService jwt) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Error = "Username and password are required." });

        if (request.Username.Length < 3 || request.Username.Length > 50)
            return BadRequest(new { Error = "Username must be between 3 and 50 characters." });

        if (request.Password.Length < 6)
            return BadRequest(new { Error = "Password must be at least 6 characters." });

        var normalizedUsername = request.Username.ToLowerInvariant().Trim();

        if (await db.Users.AnyAsync(u => u.Username == normalizedUsername))
            return Conflict(new { Error = "Username is already taken." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName?.Trim(),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwt.GenerateToken(user);

        return Ok(new LoginResponse(token, user.Username, user.DisplayName, user.NicknameColor));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Error = "Username and password are required." });

        var normalizedUsername = request.Username.ToLowerInvariant().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        user.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var token = jwt.GenerateToken(user);

        return Ok(new LoginResponse(token, user.Username, user.DisplayName, user.NicknameColor));
    }
}
