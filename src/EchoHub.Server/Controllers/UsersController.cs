using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("general")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ImageToAsciiService _asciiService;

    public UsersController(IUserService userService, ImageToAsciiService asciiService)
    {
        _userService = userService;
        _asciiService = asciiService;
    }

    [HttpGet("{username}/profile")]
    public async Task<IActionResult> GetProfile(string username)
    {
        var profile = await _userService.GetUserProfileAsync(username);

        if (profile is null)
            return NotFound(new ErrorResponse("User not found."));

        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var result = await _userService.UpdateProfileAsync(
            Guid.Parse(userIdClaim), request.DisplayName, request.Bio, request.NicknameColor);

        if (!result.IsSuccess)
            return MapUserError(result);

        return Ok(result.User!);
    }

    [HttpPost("avatar")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> UploadAvatar()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new ErrorResponse("No file uploaded."));

        var file = Request.Form.Files[0];

        if (file.Length > HubConstants.MaxAvatarSizeBytes)
            return BadRequest(new ErrorResponse($"File size exceeds maximum of {HubConstants.MaxAvatarSizeBytes / (1024 * 1024)} MB."));

        using var stream = file.OpenReadStream();

        if (!FileValidationHelper.IsValidImage(stream))
            return BadRequest(new ErrorResponse("File is not a valid image. Supported formats: JPEG, PNG, GIF, WebP."));

        var asciiArt = _asciiService.ConvertToAscii(stream);

        var result = await _userService.SetAvatarAsync(Guid.Parse(userIdClaim), asciiArt);
        if (!result.IsSuccess)
            return MapUserError(result);

        return Ok(new AvatarUploadResponse(asciiArt));
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
