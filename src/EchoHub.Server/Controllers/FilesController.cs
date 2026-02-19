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
    [HttpGet("{fileId}")]
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
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }
}
