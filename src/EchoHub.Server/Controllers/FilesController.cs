using EchoHub.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(FileStorageService fileStorage) : ControllerBase
{
    [HttpGet("{fileId}")]
    public IActionResult GetFile(string fileId)
    {
        var filePath = fileStorage.GetFilePath(fileId);

        if (filePath is null)
            return NotFound(new { Error = "File not found." });

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
