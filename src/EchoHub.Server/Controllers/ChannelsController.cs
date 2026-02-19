using System.Security.Claims;
using EchoHub.Core.Constants;
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
[Route("api/channels")]
[Authorize]
[EnableRateLimiting("general")]
public class ChannelsController : ControllerBase
{
    private readonly EchoHubDbContext _db;
    private readonly FileStorageService _fileStorage;
    private readonly ImageToAsciiService _asciiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatService _chatService;

    public ChannelsController(
        EchoHubDbContext db,
        FileStorageService fileStorage,
        ImageToAsciiService asciiService,
        IHttpClientFactory httpClientFactory,
        IChatService chatService)
    {
        _db = db;
        _fileStorage = fileStorage;
        _asciiService = asciiService;
        _httpClientFactory = httpClientFactory;
        _chatService = chatService;
    }
    [HttpGet]
    public async Task<IActionResult> GetChannels([FromQuery] int offset = 0, [FromQuery] int limit = 50)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        offset = Math.Max(0, offset);
        limit = Math.Clamp(limit, 1, 100);

        // Public channels + private channels the user has joined
        var query = _db.Channels.Where(c =>
            c.IsPublic || _db.ChannelMemberships.Any(m => m.ChannelId == c.Id && m.UserId == userId));
        var total = await query.CountAsync();

        var channels = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Topic,
                c.IsPublic,
                c.Messages.Count,
                c.CreatedAt))
            .ToListAsync();

        return Ok(new PaginatedResponse<ChannelDto>(channels, total, offset, limit));
    }

    [HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("Channel name is required."));

        var channelName = request.Name.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return BadRequest(new ErrorResponse("Channel name must be 2-100 characters and contain only letters, digits, underscores, or hyphens."));

        if (await _db.Channels.AnyAsync(c => c.Name == channelName))
            return Conflict(new ErrorResponse($"Channel '{channelName}' already exists."));

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = channelName,
            Topic = request.Topic?.Trim(),
            IsPublic = request.IsPublic,
            CreatedByUserId = Guid.Parse(userIdClaim),
        };

        _db.Channels.Add(channel);

        // Creator automatically becomes a member
        _db.ChannelMemberships.Add(new ChannelMembership
        {
            UserId = Guid.Parse(userIdClaim),
            ChannelId = channel.Id,
        });

        await _db.SaveChangesAsync();

        var dto = new ChannelDto(channel.Id, channel.Name, channel.Topic, channel.IsPublic, 0, channel.CreatedAt);
        if (channel.IsPublic)
            await _chatService.BroadcastChannelUpdatedAsync(dto);

        return Created($"/api/channels/{channelName}", dto);
    }

    [HttpPut("{channel}/topic")]
    public async Task<IActionResult> UpdateTopic(string channel, [FromBody] UpdateTopicRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var channelName = channel.ToLowerInvariant().Trim();
        var dbChannel = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

        if (dbChannel is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        if (dbChannel.CreatedByUserId != Guid.Parse(userIdClaim))
            return StatusCode(403, new ErrorResponse("Only the channel creator can update the topic."));

        if (request.Topic is not null && request.Topic.Length > ValidationConstants.MaxChannelTopicLength)
            return BadRequest(new ErrorResponse($"Topic must not exceed {ValidationConstants.MaxChannelTopicLength} characters."));

        dbChannel.Topic = request.Topic?.Trim();
        await _db.SaveChangesAsync();

        var messageCount = await _db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id);
        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, messageCount, dbChannel.CreatedAt);
        await _chatService.BroadcastChannelUpdatedAsync(dto, channelName);

        return Ok(dto);
    }

    [HttpDelete("{channel}")]
    public async Task<IActionResult> DeleteChannel(string channel)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var channelName = channel.ToLowerInvariant().Trim();

        if (channelName == HubConstants.DefaultChannel)
            return BadRequest(new ErrorResponse($"The '{HubConstants.DefaultChannel}' channel cannot be deleted."));

        var dbChannel = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

        if (dbChannel is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        var userId = Guid.Parse(userIdClaim);
        var caller = await _db.Users.FindAsync(userId);
        if (dbChannel.CreatedByUserId != userId && (caller is null || caller.Role < ServerRole.Admin))
            return StatusCode(403, new ErrorResponse("Only the channel creator or an admin can delete the channel."));

        _db.Channels.Remove(dbChannel);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{channel}/upload")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> Upload(string channel, [FromQuery] string? size = null)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usernameClaim = User.FindFirstValue("username");
        if (userIdClaim is null || usernameClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        var channelName = channel.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return BadRequest(new ErrorResponse("Invalid channel name format."));

        var dbChannel = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new ErrorResponse("No file uploaded."));

        var file = Request.Form.Files[0];

        if (file.Length > HubConstants.MaxFileSizeBytes)
            return BadRequest(new ErrorResponse($"File size exceeds maximum of {HubConstants.MaxFileSizeBytes / (1024 * 1024)} MB."));

        // Detect if file is an image by checking magic bytes
        using var stream = file.OpenReadStream();
        var isImage = FileValidationHelper.IsValidImage(stream);

        var (fileId, filePath) = await _fileStorage.SaveFileAsync(stream, file.FileName);

        var messageType = isImage ? MessageType.Image : MessageType.File;
        string content;

        if (isImage)
        {
            var (w, h) = ImageToAsciiService.GetDimensions(size);
            using var imageStream = System.IO.File.OpenRead(filePath);
            content = _asciiService.ConvertToAscii(imageStream, w, h);
        }
        else
        {
            content = file.FileName;
        }

        var attachmentUrl = $"/api/files/{fileId}";
        var sender = await _db.Users.FindAsync(userId);

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

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

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

        await _chatService.BroadcastMessageAsync(channelName, messageDto);

        return Ok(messageDto);
    }

    [HttpPost("{channel}/send-url")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> SendUrl(string channel, [FromBody] SendUrlRequest request, [FromQuery] string? size = null)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usernameClaim = User.FindFirstValue("username");
        if (userIdClaim is null || usernameClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var userId = Guid.Parse(userIdClaim);
        var channelName = channel.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return BadRequest(new ErrorResponse("Invalid channel name format."));

        var dbChannel = await _db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new ErrorResponse("URL is required."));

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new ErrorResponse("Invalid URL. Only http and https are supported."));

        // Download image from URL
        byte[] imageBytes;
        string fileName;
        try
        {
            using var client = _httpClientFactory.CreateClient("ImageDownload");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > HubConstants.MaxFileSizeBytes)
                return BadRequest(new ErrorResponse($"File size exceeds maximum of {HubConstants.MaxFileSizeBytes / (1024 * 1024)} MB."));

            imageBytes = await response.Content.ReadAsByteArrayAsync();

            if (imageBytes.Length > HubConstants.MaxFileSizeBytes)
                return BadRequest(new ErrorResponse($"File size exceeds maximum of {HubConstants.MaxFileSizeBytes / (1024 * 1024)} MB."));

            fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                var ext = contentType switch
                {
                    "image/png" => ".png",
                    "image/jpeg" or "image/jpg" => ".jpg",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".bin"
                };
                fileName = $"download{ext}";
            }
        }
        catch (TaskCanceledException)
        {
            return BadRequest(new ErrorResponse("Download timed out. The URL may be unreachable."));
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new ErrorResponse($"Failed to download from URL: {ex.Message}"));
        }

        // Validate it's actually an image
        using var memoryStream = new MemoryStream(imageBytes);
        if (!FileValidationHelper.IsValidImage(memoryStream))
            return BadRequest(new ErrorResponse("The URL does not point to a valid image. Supported formats: JPEG, PNG, GIF, WebP."));

        // Save file and convert to ASCII
        var (fileId, filePath) = await _fileStorage.SaveFileAsync(memoryStream, fileName);

        string content;
        var (w, h) = ImageToAsciiService.GetDimensions(size);
        using (var imageStream = System.IO.File.OpenRead(filePath))
        {
            content = _asciiService.ConvertToAscii(imageStream, w, h);
        }

        var attachmentUrl = $"/api/files/{fileId}";
        var sender = await _db.Users.FindAsync(userId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            Type = MessageType.Image,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = fileName,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = dbChannel.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            message.Content,
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            MessageType.Image,
            attachmentUrl,
            fileName,
            message.SentAt);

        await _chatService.BroadcastMessageAsync(channelName, messageDto);

        return Ok(messageDto);
    }
}
