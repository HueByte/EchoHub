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

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/channels")]
[Authorize]
[EnableRateLimiting("general")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly EchoHubDbContext _db;
    private readonly FileStorageService _fileStorage;
    private readonly ImageToAsciiService _asciiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatService _chatService;
    private readonly IMessageEncryptionService _encryption;

    public ChannelsController(
        IChannelService channelService,
        EchoHubDbContext db,
        FileStorageService fileStorage,
        ImageToAsciiService asciiService,
        IHttpClientFactory httpClientFactory,
        IChatService chatService,
        IMessageEncryptionService encryption)
    {
        _channelService = channelService;
        _db = db;
        _fileStorage = fileStorage;
        _asciiService = asciiService;
        _httpClientFactory = httpClientFactory;
        _chatService = chatService;
        _encryption = encryption;
    }

    [HttpGet]
    public async Task<IActionResult> GetChannels([FromQuery] int offset = 0, [FromQuery] int limit = 50)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        offset = Math.Max(0, offset);
        limit = Math.Clamp(limit, 1, 100);

        var result = await _channelService.GetChannelsAsync(Guid.Parse(userIdClaim), offset, limit);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var result = await _channelService.CreateChannelAsync(
            Guid.Parse(userIdClaim), request.Name, request.Topic, request.IsPublic);
        if (!result.IsSuccess)
            return MapChannelError(result);

        if (result.Channel!.IsPublic)
            await _chatService.BroadcastChannelUpdatedAsync(result.Channel);

        return Created($"/api/channels/{result.Channel.Name}", result.Channel);
    }

    [HttpPut("{channel}/topic")]
    public async Task<IActionResult> UpdateTopic(string channel, [FromBody] UpdateTopicRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var result = await _channelService.UpdateTopicAsync(
            Guid.Parse(userIdClaim), channel, request.Topic);
        if (!result.IsSuccess)
            return MapChannelError(result);

        await _chatService.BroadcastChannelUpdatedAsync(result.Channel!, channel.ToLowerInvariant().Trim());
        return Ok(result.Channel);
    }

    [HttpDelete("{channel}")]
    public async Task<IActionResult> DeleteChannel(string channel)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var result = await _channelService.DeleteChannelAsync(Guid.Parse(userIdClaim), channel);
        if (!result.IsSuccess)
            return MapChannelError(result);

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

        var channelDto = await _channelService.GetChannelByNameAsync(channelName);
        if (channelDto is null)
            return NotFound(new ErrorResponse($"Channel '{channelName}' does not exist."));

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new ErrorResponse("No file uploaded."));

        var file = Request.Form.Files[0];

        // Detect file type early so we can apply the correct size limit
        var isAudioByExtension = FileValidationHelper.IsAudioFile(file.FileName);
        var maxSize = isAudioByExtension ? HubConstants.MaxAudioFileSizeBytes : HubConstants.MaxFileSizeBytes;

        if (file.Length > maxSize)
            return BadRequest(new ErrorResponse($"File size exceeds maximum of {maxSize / (1024 * 1024)} MB."));

        // Detect file type: image (magic bytes), audio (extension), or generic file
        using var stream = file.OpenReadStream();
        var isImage = FileValidationHelper.IsValidImage(stream);
        var isAudio = !isImage && isAudioByExtension;

        var (fileId, filePath) = await _fileStorage.SaveFileAsync(stream, file.FileName);

        var messageType = isImage ? MessageType.Image
            : isAudio ? MessageType.Audio
            : MessageType.File;
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
        var dbContent = _encryption.EncryptDatabaseEnabled ? _encryption.Encrypt(content) : content;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = dbContent,
            Type = messageType,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = file.FileName,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channelDto.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Encrypt for transport — clients decrypt
        var messageDto = new MessageDto(
            message.Id,
            _encryption.Encrypt(content),
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

        var channelDto = await _channelService.GetChannelByNameAsync(channelName);
        if (channelDto is null)
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
        var dbContent = _encryption.EncryptDatabaseEnabled ? _encryption.Encrypt(content) : content;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = dbContent,
            Type = MessageType.Image,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = fileName,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channelDto.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Encrypt for transport — clients decrypt
        var messageDto = new MessageDto(
            message.Id,
            _encryption.Encrypt(content),
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

    private IActionResult MapChannelError(ChannelOperationResult result) => result.Error switch
    {
        ChannelError.ValidationFailed => BadRequest(new ErrorResponse(result.ErrorMessage!)),
        ChannelError.AlreadyExists => Conflict(new ErrorResponse(result.ErrorMessage!)),
        ChannelError.NotFound => NotFound(new ErrorResponse(result.ErrorMessage!)),
        ChannelError.Forbidden => StatusCode(403, new ErrorResponse(result.ErrorMessage!)),
        ChannelError.Protected => BadRequest(new ErrorResponse(result.ErrorMessage!)),
        _ => BadRequest(new ErrorResponse(result.ErrorMessage ?? "Unknown error.")),
    };
}
