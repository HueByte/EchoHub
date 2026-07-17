using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.Security;
using EchoHub.Core.Services;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Config;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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
    private readonly UploadLimits _uploadLimits;

    public ChannelsController(
        IChannelService channelService,
        EchoHubDbContext db,
        FileStorageService fileStorage,
        ImageToAsciiService asciiService,
        IHttpClientFactory httpClientFactory,
        IChatService chatService,
        IMessageEncryptionService encryption,
        UploadLimits uploadLimits)
    {
        _channelService = channelService;
        _db = db;
        _fileStorage = fileStorage;
        _asciiService = asciiService;
        _httpClientFactory = httpClientFactory;
        _chatService = chatService;
        _encryption = encryption;
        _uploadLimits = uploadLimits;
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
            Guid.Parse(userIdClaim), request.Name, request.Topic, request.IsPublic, request.Password,
            request.EncryptionSalt, request.WrappedRoomKey);
        if (!result.IsSuccess)
            return MapChannelError(result);

        if (result.Channel!.IsPublic)
            await _chatService.BroadcastChannelUpdatedAsync(result.Channel);

        return Created($"/api/channels/{result.Channel.Name}", result.Channel);
    }

    /// <summary>
    /// Public crypto metadata for a channel: whether it is end-to-end encrypted and the
    /// PBKDF2 salt clients need to derive their join credential. Never returns the
    /// wrapped room key — that is only handed out after a successful join.
    /// </summary>
    [HttpGet("{channel}/crypto")]
    public async Task<IActionResult> GetChannelCrypto(string channel)
    {
        var crypto = await _channelService.GetChannelCryptoAsync(channel);
        if (crypto is null)
            return NotFound(new ErrorResponse($"Channel '{channel}' does not exist."));

        return Ok(crypto);
    }

    /// <summary>
    /// Human-facing summary of a channel (message count, unique posters, estimated size,
    /// created date, room id). Available for encrypted channels too — these are metadata the
    /// server tracks even though it cannot read the messages themselves.
    /// </summary>
    [HttpGet("{channel}/meta")]
    public async Task<IActionResult> GetChannelMeta(string channel)
    {
        var meta = await _channelService.GetChannelMetaAsync(channel);
        if (meta is null)
            return NotFound(new ErrorResponse($"Channel '{channel}' does not exist."));

        return Ok(meta);
    }

    /// <summary>
    /// Changes an encrypted channel's passphrase by re-wrapping its room key.
    /// The caller proves knowledge of the old passphrase via the old auth key;
    /// history is never re-encrypted (the room content key does not change).
    /// </summary>
    [HttpPost("{channel}/rekey")]
    public async Task<IActionResult> RekeyChannel(string channel, [FromBody] RekeyChannelRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return Unauthorized(new ErrorResponse("Authentication required."));

        var result = await _channelService.RekeyChannelAsync(
            Guid.Parse(userIdClaim), channel,
            request.OldPassword, request.NewPassword,
            request.NewEncryptionSalt, request.NewWrappedRoomKey);
        if (!result.IsSuccess)
            return MapChannelError(result);

        return Ok(result.Channel);
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

        await _chatService.BroadcastChannelDeletedAsync(channel.ToLowerInvariant().Trim());

        return NoContent();
    }

    /// <summary>
    /// Sends one message carrying optional text (<c>content</c> form field) plus zero or more
    /// file attachments (Discord-style). For non-encrypted channels the server sniffs each file's
    /// kind and renders ASCII previews for images. For end-to-end encrypted channels the client
    /// uploads ciphertext blobs and declares each file's kind (<c>kind</c>) and pre-rendered,
    /// room-encrypted preview (<c>preview</c>), aligned by file order — the server never inspects them.
    /// </summary>
    // Request-body and multipart limits are applied at runtime from the configured UploadLimits
    // (see below) rather than via [RequestSizeLimit]/[RequestFormLimits], which require
    // compile-time constants and so couldn't honor the "Uploads" configuration section.
    [HttpPost("{channel}/messages")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> SendMessageWithAttachments(string channel, [FromQuery] string? size = null)
    {
        // Raise this request's body ceiling to the configured maximum before the body is read.
        var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is not null && !bodySizeFeature.IsReadOnly)
            bodySizeFeature.MaxRequestBodySize = _uploadLimits.MaxRequestBodyBytes;

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

        if (channelDto.IsSystem)
            return StatusCode(403, new ErrorResponse("This channel is read-only."));

        if (!Request.HasFormContentType)
            return BadRequest(new ErrorResponse("Expected multipart form data."));

        var files = Request.Form.Files;
        if (files.Count == 0)
            return BadRequest(new ErrorResponse("At least one attachment is required. Send plain text over the chat connection."));
        if (files.Count > _uploadLimits.MaxAttachmentsPerMessage)
            return BadRequest(new ErrorResponse($"A message may carry at most {_uploadLimits.MaxAttachmentsPerMessage} attachments."));

        var sender = await _db.Users.FindAsync(userId);
        if (sender is not null && sender.IsMuted && (sender.MutedUntil is null || sender.MutedUntil > DateTimeOffset.UtcNow))
            return StatusCode(403, new ErrorResponse("You are muted and cannot send messages."));

        // Caption: plaintext for normal channels, $RC1$ room-ciphertext for encrypted ones.
        // Decrypt() is a pass-through when there is no transport prefix.
        var content = _encryption.Decrypt(Request.Form["content"].ToString());
        var isRoomCiphertext = RoomCrypto.IsRoomCiphertext(content);
        if (!isRoomCiphertext && content.Length > HubConstants.MaxMessageLength)
            return BadRequest(new ErrorResponse($"Message exceeds maximum length of {HubConstants.MaxMessageLength} characters."));

        var declaredKinds = Request.Form["kind"];
        var declaredPreviews = Request.Form["preview"];

        var attachmentEntities = new List<Attachment>();
        var attachmentDtos = new List<AttachmentDto>();

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            AttachmentKind kind;
            string? previewPlain;
            string fileId;

            if (channelDto.IsEncrypted)
            {
                // Ciphertext blob — trust the client's declared kind + room-encrypted preview.
                // Client sends one kind + preview per file in order; empty preview means none.
                kind = ParseKind(i < declaredKinds.Count ? declaredKinds[i] : null);
                previewPlain = i < declaredPreviews.Count ? declaredPreviews[i] : null;
                if (string.IsNullOrEmpty(previewPlain))
                    previewPlain = null;

                if (file.Length > _uploadLimits.MaxForKind(kind))
                    return BadRequest(new ErrorResponse($"'{file.FileName}' exceeds the maximum size."));

                using var encryptedStream = file.OpenReadStream();
                (fileId, _) = await _fileStorage.SaveFileAsync(encryptedStream, file.FileName);
            }
            else
            {
                using var stream = file.OpenReadStream();
                var isImage = FileValidationHelper.IsValidImage(stream);
                var isAudio = !isImage && FileValidationHelper.IsAudioFile(file.FileName);
                kind = isImage ? AttachmentKind.Image : isAudio ? AttachmentKind.Audio : AttachmentKind.File;

                if (file.Length > _uploadLimits.MaxForKind(kind))
                    return BadRequest(new ErrorResponse($"'{file.FileName}' exceeds the maximum size of {_uploadLimits.MaxForKind(kind) / (1024 * 1024)} MB."));

                string filePath;
                (fileId, filePath) = await _fileStorage.SaveFileAsync(stream, file.FileName);

                if (isImage)
                {
                    var (w, h) = ImageToAsciiService.GetDimensions(size);
                    using var imageStream = System.IO.File.OpenRead(filePath);
                    previewPlain = _asciiService.ConvertToAscii(imageStream, w, h);
                }
                else
                {
                    previewPlain = null;
                }
            }

            var url = $"/api/files/{fileId}";
            attachmentEntities.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Url = url,
                FileName = file.FileName,
                FileSize = file.Length,
                AsciiPreview = _encryption.EncryptDatabaseEnabled ? _encryption.EncryptNullable(previewPlain) : previewPlain,
            });
            attachmentDtos.Add(new AttachmentDto(kind, url, file.FileName, file.Length,
                _encryption.EncryptNullable(previewPlain)));
        }

        var dbContent = _encryption.EncryptDatabaseEnabled ? _encryption.Encrypt(content) : content;
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = dbContent,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channelDto.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
            Attachments = attachmentEntities,
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            _encryption.Encrypt(content),
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            message.SentAt,
            attachmentDtos,
            SenderDisplayName: sender?.DisplayName);

        await _chatService.BroadcastMessageAsync(channelName, messageDto);

        return Ok(messageDto);
    }

    private static AttachmentKind ParseKind(string? kind) => kind?.ToLowerInvariant() switch
    {
        "image" => AttachmentKind.Image,
        "audio" => AttachmentKind.Audio,
        _ => AttachmentKind.File,
    };

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

        if (channelDto.IsSystem)
            return StatusCode(403, new ErrorResponse("This channel is read-only."));

        if (channelDto.IsEncrypted)
            return BadRequest(new ErrorResponse(
                "Sending images by URL is not available in end-to-end encrypted channels — download the image and /send the file instead."));

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
            if (contentLength > _uploadLimits.MaxImageSizeBytes)
                return BadRequest(new ErrorResponse($"File size exceeds maximum of {_uploadLimits.MaxImageSizeBytes / (1024 * 1024)} MB."));

            imageBytes = await response.Content.ReadAsByteArrayAsync();

            if (imageBytes.Length > _uploadLimits.MaxImageSizeBytes)
                return BadRequest(new ErrorResponse($"File size exceeds maximum of {_uploadLimits.MaxImageSizeBytes / (1024 * 1024)} MB."));

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

        string preview;
        var (w, h) = ImageToAsciiService.GetDimensions(size);
        using (var imageStream = System.IO.File.OpenRead(filePath))
        {
            preview = _asciiService.ConvertToAscii(imageStream, w, h);
        }

        var attachmentUrl = $"/api/files/{fileId}";
        var sender = await _db.Users.FindAsync(userId);

        // A URL-shared image is a message with no caption and one image attachment.
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            Kind = AttachmentKind.Image,
            Url = attachmentUrl,
            FileName = fileName,
            FileSize = imageBytes.Length,
            AsciiPreview = _encryption.EncryptDatabaseEnabled ? _encryption.Encrypt(preview) : preview,
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = string.Empty,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channelDto.Id,
            SenderUserId = userId,
            SenderUsername = usernameClaim,
            Attachments = [attachment],
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            _encryption.Encrypt(string.Empty),
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            message.SentAt,
            [new AttachmentDto(AttachmentKind.Image, attachmentUrl, fileName, imageBytes.Length, _encryption.Encrypt(preview))],
            SenderDisplayName: sender?.DisplayName);

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
