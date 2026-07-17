using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.Models;
using EchoHub.Core.Services;
using EchoHub.Core.DTOs;
using EchoHub.Server.Config;
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
public class UsersController : ControllerBase
{
    /// <summary>
    /// Username tombstoned messages are re-attributed to after account deletion.
    /// Reserved — <see cref="UserService"/> refuses to register it.
    /// </summary>
    public const string DeletedUserName = "deleted-user";

    private readonly IUserService _userService;
    private readonly ImageToAsciiService _asciiService;
    private readonly UploadLimits _uploadLimits;
    private readonly EchoHubDbContext _db;
    private readonly IMessageEncryptionService _encryption;
    private readonly FileStorageService _fileStorage;
    private readonly PresenceTracker _presenceTracker;
    private readonly IEnumerable<IChatBroadcaster> _broadcasters;
    private readonly IConfiguration _config;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ImageToAsciiService asciiService,
        UploadLimits uploadLimits,
        EchoHubDbContext db,
        IMessageEncryptionService encryption,
        FileStorageService fileStorage,
        PresenceTracker presenceTracker,
        IEnumerable<IChatBroadcaster> broadcasters,
        IConfiguration config,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _asciiService = asciiService;
        _uploadLimits = uploadLimits;
        _db = db;
        _encryption = encryption;
        _fileStorage = fileStorage;
        _presenceTracker = presenceTracker;
        _broadcasters = broadcasters;
        _config = config;
        _logger = logger;
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

        if (file.Length > _uploadLimits.MaxAvatarSizeBytes)
            return BadRequest(new ErrorResponse($"File size exceeds maximum of {_uploadLimits.MaxAvatarSizeBytes / (1024 * 1024)} MB."));

        using var stream = file.OpenReadStream();

        if (!FileValidationHelper.IsValidImage(stream))
            return BadRequest(new ErrorResponse("File is not a valid image. Supported formats: JPEG, PNG, GIF, WebP."));

        var asciiArt = _asciiService.ConvertToAscii(stream);

        var result = await _userService.SetAvatarAsync(Guid.Parse(userIdClaim), asciiArt);
        if (!result.IsSuccess)
            return MapUserError(result);

        return Ok(new AvatarUploadResponse(asciiArt));
    }

    /// <summary>
    /// Everything the server stores about the caller, as stored: profile, their messages
    /// (end-to-end encrypted room content stays ciphertext — the server never had plaintext),
    /// and metadata of their uploaded attachments. "You own the data" made demonstrable.
    /// </summary>
    [HttpGet("me/export")]
    public async Task<IActionResult> ExportMyData()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        var profile = await _userService.GetUserByIdAsync(userId);
        if (profile is null)
            return Unauthorized(new ErrorResponse("User not found."));

        var messages = await _db.Messages
            .Where(m => m.SenderUserId == userId)
            .OrderBy(m => m.SentAt)
            .Join(_db.Channels, m => m.ChannelId, c => c.Id, (m, c) => new { m, ChannelName = c.Name })
            .ToListAsync();

        var messageIds = messages.Select(x => x.m.Id).ToList();
        var attachments = await _db.Attachments
            .Where(a => messageIds.Contains(a.MessageId))
            .ToListAsync();
        var messageById = messages.ToDictionary(x => x.m.Id, x => x);

        var export = new UserDataExportDto(
            DateTimeOffset.UtcNow,
            _config["Server:Name"] ?? "EchoHub Server",
            profile,
            messages.Select(x => new ExportedMessageDto(
                x.m.Id,
                x.ChannelName,
                x.m.SentAt,
                // Strip only the server's at-rest layer; room ciphertext passes through as-is
                _encryption.Decrypt(x.m.Content),
                x.m.ReplyToMessageId)).ToList(),
            attachments.Select(a =>
            {
                var owner = messageById[a.MessageId];
                return new ExportedAttachmentDto(
                    a.FileName, a.Url, a.FileSize, a.Kind.ToString(),
                    owner.ChannelName, owner.m.SentAt);
            }).ToList());

        _logger.LogInformation("{User} exported their data ({Messages} messages, {Attachments} attachments)",
            profile.Username, export.Messages.Count, export.Attachments.Count);

        return Ok(export);
    }

    /// <summary>
    /// Self-service account deletion (password re-confirmed). Removes the account, its refresh
    /// tokens and memberships (FK cascade), and every attachment blob the user uploaded.
    /// Their messages are kept but tombstoned to <see cref="DeletedUserName"/> — deleting them
    /// outright would silently gut other people's conversations.
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var user = await _db.Users.FindAsync(Guid.Parse(userIdClaim));
        if (user is null)
            return Unauthorized(new ErrorResponse("User not found."));

        if (string.IsNullOrEmpty(request.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponse("Password is incorrect."));

        if (user.Role == ServerRole.Owner
            && !await _db.Users.AnyAsync(u => u.Role == ServerRole.Owner && u.Id != user.Id))
        {
            return BadRequest(new ErrorResponse(
                "You are the only Owner of this server. Promote another Owner (or shut the server down) before deleting this account."));
        }

        var username = user.Username;

        // Their uploaded blobs: attachments hanging off their messages
        var attachmentInfo = await _db.Messages
            .Where(m => m.SenderUserId == user.Id)
            .SelectMany(m => m.Attachments)
            .Select(a => new { a.Id, a.Url })
            .ToListAsync();

        foreach (var fileId in attachmentInfo
                     .Select(a => a.Url.Split('/').LastOrDefault())
                     .Where(id => !string.IsNullOrEmpty(id)))
        {
            try { _fileStorage.DeleteFile(fileId!); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete blob {FileId} during account deletion", fileId); }
        }

        var attachmentIds = attachmentInfo.Select(a => a.Id).ToList();
        await _db.Attachments.Where(a => attachmentIds.Contains(a.Id)).ExecuteDeleteAsync();

        // Tombstone their messages, then remove the account (cascades tokens + memberships)
        await _db.Messages
            .Where(m => m.SenderUserId == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.SenderUserId, Guid.Empty)
                .SetProperty(m => m.SenderUsername, DeletedUserName));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Kick their live sessions and clear them from user lists everywhere
        var (connectionIds, channels) = _presenceTracker.ForceRemoveUser(username);
        foreach (var channel in channels)
            await BroadcastToAllAsync(b => b.SendUserLeftAsync(channel, username));
        if (connectionIds.Count > 0)
            await BroadcastToAllAsync(b => b.ForceDisconnectUserAsync(connectionIds, "Account deleted."));

        _logger.LogInformation("Account '{User}' self-deleted ({Attachments} attachment blobs removed)",
            username, attachmentIds.Count);

        return Ok();
    }

    private async Task BroadcastToAllAsync(Func<IChatBroadcaster, Task> action)
    {
        foreach (var broadcaster in _broadcasters)
        {
            try { await action(broadcaster); }
            catch { /* logged by broadcaster */ }
        }
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
