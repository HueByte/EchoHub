using System.Security.Claims;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize]
[EnableRateLimiting("general")]
public class ModerationController : ControllerBase
{
    private readonly EchoHubDbContext _db;
    private readonly IChatService _chatService;
    private readonly PresenceTracker _presenceTracker;
    private readonly IEnumerable<IChatBroadcaster> _broadcasters;

    public ModerationController(
        EchoHubDbContext db,
        IChatService chatService,
        PresenceTracker presenceTracker,
        IEnumerable<IChatBroadcaster> broadcasters)
    {
        _db = db;
        _chatService = chatService;
        _presenceTracker = presenceTracker;
        _broadcasters = broadcasters;
    }

    [HttpPost("role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        if (request.Role == ServerRole.Owner)
            return BadRequest(new ErrorResponse("Cannot assign the Owner role."));

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{request.Username}' not found."));

        if (target.Role == ServerRole.Owner)
            return BadRequest(new ErrorResponse("Cannot change the server owner's role."));

        if (request.Role >= caller!.Role)
            return BadRequest(new ErrorResponse("Cannot assign a role equal to or above your own."));

        target.Role = request.Role;
        await _db.SaveChangesAsync();

        return Ok(new { Message = $"{target.Username} is now {request.Role}." });
    }

    [HttpPost("kick/{username}")]
    public async Task<IActionResult> KickUser(string username, [FromBody] KickRequest? request = null)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Mod);
        if (error is not null) return error;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{username}' not found."));

        if (target.Role >= caller!.Role)
            return BadRequest(new ErrorResponse("Cannot kick a user with equal or higher role."));

        // Broadcast kick to all channels the user is in, then clean up presence
        var channels = _presenceTracker.GetChannelsForUser(target.Username);
        foreach (var channel in channels)
        {
            await BroadcastToAllAsync(b => b.SendUserKickedAsync(channel, target.Username, request?.Reason));
        }

        // Remove from presence tracker and force disconnect all connections
        var reason = request?.Reason ?? "You have been kicked from the server.";
        await ForceDisconnectAndCleanupAsync(target.Username, reason);

        return Ok(new { Message = $"{target.Username} has been kicked." });
    }

    [HttpPost("ban/{username}")]
    public async Task<IActionResult> BanUser(string username, [FromBody] BanRequest? request = null)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{username}' not found."));

        if (target.Role >= caller!.Role)
            return BadRequest(new ErrorResponse("Cannot ban a user with equal or higher role."));

        target.IsBanned = true;
        await _db.SaveChangesAsync();

        // Broadcast ban notification, then force disconnect
        await BroadcastToAllAsync(b => b.SendUserBannedAsync(target.Username, request?.Reason));

        var reason = request?.Reason ?? "You have been banned from this server.";
        await ForceDisconnectAndCleanupAsync(target.Username, reason);

        return Ok(new { Message = $"{target.Username} has been banned." });
    }

    [HttpPost("unban/{username}")]
    public async Task<IActionResult> UnbanUser(string username)
    {
        var (_, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{username}' not found."));

        target.IsBanned = false;
        await _db.SaveChangesAsync();

        return Ok(new { Message = $"{target.Username} has been unbanned." });
    }

    [HttpPost("mute/{username}")]
    public async Task<IActionResult> MuteUser(string username, [FromBody] MuteRequest? request = null)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Mod);
        if (error is not null) return error;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{username}' not found."));

        if (target.Role >= caller!.Role)
            return BadRequest(new ErrorResponse("Cannot mute a user with equal or higher role."));

        target.IsMuted = true;
        target.MutedUntil = request?.DurationMinutes is > 0
            ? DateTimeOffset.UtcNow.AddMinutes(request.DurationMinutes.Value)
            : null;
        await _db.SaveChangesAsync();

        var durationText = request?.DurationMinutes is > 0 ? $" for {request.DurationMinutes} minutes" : "";
        return Ok(new { Message = $"{target.Username} has been muted{durationText}." });
    }

    [HttpPost("unmute/{username}")]
    public async Task<IActionResult> UnmuteUser(string username)
    {
        var (_, error) = await GetCallerAsync(ServerRole.Mod);
        if (error is not null) return error;

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
        if (target is null)
            return NotFound(new ErrorResponse($"User '{username}' not found."));

        target.IsMuted = false;
        target.MutedUntil = null;
        await _db.SaveChangesAsync();

        return Ok(new { Message = $"{target.Username} has been unmuted." });
    }

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var (_, error) = await GetCallerAsync(ServerRole.Mod);
        if (error is not null) return error;

        var message = await _db.Messages
            .Include(m => m.Channel)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message is null)
            return NotFound(new ErrorResponse("Message not found."));

        var channelName = message.Channel!.Name;
        _db.Messages.Remove(message);
        await _db.SaveChangesAsync();

        await BroadcastToAllAsync(b => b.SendMessageDeletedAsync(channelName, messageId));

        return Ok(new { Message = "Message deleted." });
    }

    [HttpDelete("channels/{channel}/nuke")]
    public async Task<IActionResult> NukeChannel(string channel)
    {
        var (_, error) = await GetCallerAsync(ServerRole.Mod);
        if (error is not null) return error;

        var channelName = channel.ToLowerInvariant().Trim();
        var dbChannel = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        var messages = await _db.Messages.Where(m => m.ChannelId == dbChannel.Id).ToListAsync();
        _db.Messages.RemoveRange(messages);
        await _db.SaveChangesAsync();

        await BroadcastToAllAsync(b => b.SendChannelNukedAsync(channelName));

        return Ok(new { Message = $"All messages in #{channelName} have been cleared." });
    }

    private async Task<(User? Caller, IActionResult? Error)> GetCallerAsync(ServerRole minimumRole)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return (null, Unauthorized(new ErrorResponse("Authentication required.")));

        var caller = await _db.Users.FindAsync(Guid.Parse(userIdClaim));
        if (caller is null)
            return (null, Unauthorized(new ErrorResponse("User not found.")));

        if (caller.Role < minimumRole)
            return (null, StatusCode(403, new ErrorResponse($"Requires {minimumRole} role or higher.")));

        return (caller, null);
    }

    /// <summary>
    /// Remove user from presence tracking, broadcast their departure from all channels,
    /// send a ForceDisconnect signal, and update their DB status.
    /// </summary>
    private async Task ForceDisconnectAndCleanupAsync(string username, string reason)
    {
        var (connectionIds, channels) = _presenceTracker.ForceRemoveUser(username);

        // Notify remaining users that this person left each channel
        foreach (var channel in channels)
        {
            await BroadcastToAllAsync(b => b.SendUserLeftAsync(channel, username));
        }

        // Signal the user's clients to disconnect
        if (connectionIds.Count > 0)
        {
            await BroadcastToAllAsync(b => b.ForceDisconnectUserAsync(connectionIds, reason));
        }

        // Mark user offline in DB
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is not null)
        {
            user.Status = UserStatus.Invisible;
            user.LastSeenAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private async Task BroadcastToAllAsync(Func<IChatBroadcaster, Task> action)
    {
        foreach (var broadcaster in _broadcasters)
        {
            try { await action(broadcaster); }
            catch { /* logged by broadcaster */ }
        }
    }
}
