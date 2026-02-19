using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("general")]
public class UsersController(EchoHubDbContext db, ImageToAsciiService asciiService) : ControllerBase
{
    [HttpGet("{username}/profile")]
    public async Task<IActionResult> GetProfile(string username)
    {
        var normalizedUsername = username.ToLowerInvariant().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

        if (user is null)
            return NotFound(new ErrorResponse("User not found."));

        return Ok(ToProfileDto(user));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        var user = await db.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new ErrorResponse("User not found."));

        if (request.DisplayName is not null)
        {
            if (request.DisplayName.Length > ValidationConstants.MaxDisplayNameLength)
                return BadRequest(new ErrorResponse($"Display name must not exceed {ValidationConstants.MaxDisplayNameLength} characters."));
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Bio is not null)
        {
            if (request.Bio.Length > ValidationConstants.MaxBioLength)
                return BadRequest(new ErrorResponse($"Bio must not exceed {ValidationConstants.MaxBioLength} characters."));
            user.Bio = request.Bio.Trim();
        }

        if (request.NicknameColor is not null)
        {
            var color = request.NicknameColor.Trim();
            if (color.Length > 0 && !ValidationConstants.HexColorRegex().IsMatch(color))
                return BadRequest(new ErrorResponse("Nickname color must be a valid hex color (e.g. #FF5500)."));
            user.NicknameColor = color.Length > 0 ? color : null;
        }

        await db.SaveChangesAsync();

        return Ok(ToProfileDto(user));
    }

    [HttpPost("avatar")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> UploadAvatar()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        var user = await db.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new ErrorResponse("User not found."));

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new ErrorResponse("No file uploaded."));

        var file = Request.Form.Files[0];

        if (file.Length > HubConstants.MaxAvatarSizeBytes)
            return BadRequest(new ErrorResponse($"File size exceeds maximum of {HubConstants.MaxAvatarSizeBytes / (1024 * 1024)} MB."));

        using var stream = file.OpenReadStream();

        if (!FileValidationHelper.IsValidImage(stream))
            return BadRequest(new ErrorResponse("File is not a valid image. Supported formats: JPEG, PNG, GIF, WebP."));

        var asciiArt = asciiService.ConvertToAscii(stream);

        user.AvatarAscii = asciiArt;
        await db.SaveChangesAsync();

        return Ok(new AvatarUploadResponse(asciiArt));
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
