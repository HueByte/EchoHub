# Adding a new controller

> *Workflow template auto-derived from 7 existing exemplar(s).*

This template describes how to add a new HTTP controller to the server: reach for this pattern when you need a new API surface implemented as an [ApiController] class that exposes routes and actions. Use the reference controller below as the concrete shape to copy (attributes, base class, constructor injection, and action patterns), and consult the existing examples to match naming and placement.

## Reference implementation

```csharp
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
```

## Where it lives

Controllers in this codebase are placed under src/EchoHub.Server/Controllers, and exemplar files use names such as AuthController.cs, ChannelsController.cs, FilesController.cs, InvitesController.cs, ModerationController.cs, ServerController.cs, and UsersController.cs with corresponding public classes named AuthController, ChannelsController, FilesController, InvitesController, ModerationController, ServerController, and UsersController. Use that same folder and naming pattern when adding a new controller file.

## Wiring

A specific registration site for controllers was not detected in the symbol graph provided. Inspect the existing controllers listed below to see how they are referenced in the project and to follow the same runtime usage patterns used by the application.

## Existing examples

- [`AuthController`](../../Code/src/EchoHub.Server/Controllers/AuthController.cs.md)
- [`ChannelsController`](../../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md)
- [`FilesController`](../../Code/src/EchoHub.Server/Controllers/FilesController.cs.md)
- [`InvitesController`](../../Code/src/EchoHub.Server/Controllers/InvitesController.cs.md)
- [`ModerationController`](../../Code/src/EchoHub.Server/Controllers/ModerationController.cs.md)
- [`ServerController`](../../Code/src/EchoHub.Server/Controllers/ServerController.cs.md)
- [`UsersController`](../../Code/src/EchoHub.Server/Controllers/UsersController.cs.md)

---
*Synthesised by Aurion on 2026-07-23 05:55:15 UTC*
