using EchoHub.Core.DTOs;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
[EnableRateLimiting("general")]
public class FilesController : ControllerBase
{
    private readonly FileStorageService _fileStorage;

    public FilesController(FileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Serves an uploaded file. Anonymous by design: the unguessable GUID in the URL is the
    /// access token (Discord-CDN-style capability URL), so attachment links can be opened
    /// directly in a browser and shared to IRC clients. E2E-encrypted room blobs are
    /// ciphertext at rest, so anonymous access reveals nothing for those channels.
    /// </summary>
    [HttpGet("{fileId}")]
    [AllowAnonymous]
    public IActionResult GetFile(string fileId)
    {
        if (!Guid.TryParse(fileId, out _))
            return BadRequest(new ErrorResponse("Invalid file identifier."));

        var filePath = _fileStorage.GetFilePath(fileId);

        if (filePath is null)
            return NotFound(new ErrorResponse("File not found."));

        var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            ".wma" => "audio/x-ms-wma",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        // Images and audio render inline so a browser displays them instead of
        // downloading; everything else keeps the attachment disposition.
        if (contentType.StartsWith("image/") || contentType.StartsWith("audio/"))
            return PhysicalFile(filePath, contentType);

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }
}
