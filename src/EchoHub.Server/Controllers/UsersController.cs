using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(EchoHubDbContext db, ImageToAsciiService asciiService) : ControllerBase
{
    [HttpGet("{username}/profile")]
    public async Task<IActionResult> GetProfile(string username)
    {
        var normalizedUsername = username.ToLowerInvariant().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

        if (user is null)
            return NotFound(new { Error = "User not found." });

        return Ok(ToProfileDto(user));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var user = await db.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new { Error = "User not found." });

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName.Trim();

        if (request.Bio is not null)
            user.Bio = request.Bio.Trim();

        if (request.NicknameColor is not null)
            user.NicknameColor = request.NicknameColor.Trim();

        await db.SaveChangesAsync();

        return Ok(ToProfileDto(user));
    }

    [HttpPost("avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var user = await db.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new { Error = "User not found." });

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new { Error = "No file uploaded." });

        var file = Request.Form.Files[0];

        if (file.Length > HubConstants.MaxAvatarSizeBytes)
            return BadRequest(new { Error = $"File size exceeds maximum of {HubConstants.MaxAvatarSizeBytes / (1024 * 1024)} MB." });

        using var stream = file.OpenReadStream();
        var asciiArt = asciiService.ConvertToAscii(stream);

        user.AvatarAscii = asciiArt;
        await db.SaveChangesAsync();

        return Ok(new { AvatarAscii = asciiArt });
    }

    private static UserProfileDto ToProfileDto(Core.Models.User user) => new(
        user.Id,
        user.Username,
        user.DisplayName,
        user.Bio,
        user.NicknameColor,
        user.AvatarAscii,
        user.Status,
        user.StatusMessage,
        user.CreatedAt,
        user.LastSeenAt);
}
