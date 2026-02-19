using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using EchoHub.Server.Hubs;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/channels")]
[Authorize]
public class ChannelsController(
    EchoHubDbContext db,
    FileStorageService fileStorage,
    ImageToAsciiService asciiService,
    IHubContext<ChatHub, IEchoHubClient> hubContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetChannels()
    {
        var channels = await db.Channels
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Topic,
                c.Messages.Count,
                c.CreatedAt))
            .ToListAsync();

        return Ok(channels);
    }

    [HttpPost("{channel}/upload")]
    public async Task<IActionResult> Upload(string channel)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usernameClaim = User.FindFirstValue("username");
        if (userIdClaim is null || usernameClaim is null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var channelName = channel.ToLowerInvariant().Trim();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return NotFound(new { Error = $"Channel '{channelName}' does not exist." });

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new { Error = "No file uploaded." });

        var file = Request.Form.Files[0];

        if (file.Length > HubConstants.MaxFileSizeBytes)
            return BadRequest(new { Error = $"File size exceeds maximum of {HubConstants.MaxFileSizeBytes / (1024 * 1024)} MB." });

        using var stream = file.OpenReadStream();
        var (fileId, filePath) = await fileStorage.SaveFileAsync(stream, file.FileName);

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = imageExtensions.Contains(extension);

        var messageType = isImage ? MessageType.Image : MessageType.File;
        var content = isImage
            ? asciiService.ConvertToAscii(System.IO.File.OpenRead(filePath))
            : file.FileName;
        var attachmentUrl = $"/api/files/{fileId}";

        var sender = await db.Users.FindAsync(userId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            Type = messageType,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = file.FileName,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = dbChannel.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            message.Content,
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            messageType,
            attachmentUrl,
            file.FileName,
            message.SentAt);

        // Broadcast to all clients in the channel via SignalR
        await hubContext.Clients.Group(channelName).ReceiveMessage(messageDto);

        return Ok(messageDto);
    }
}
