# Adding a new controller

> *Workflow template auto-derived from 6 existing exemplar(s).*

Adding a new controller is the pattern to reach for when you need to expose a new set of HTTP endpoints in the server API. Use an existing controller in src/EchoHub.Server/Controllers as a model: the reference controller shows the typical attributes, base class, route pattern, and constructor-based dependency injection that a new controller instance should follow.

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

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, contentType, fileName);
    }
}
```

## Where it lives

Controllers live under src/EchoHub.Server/Controllers. Files are named with a Controller suffix (for example, FilesController.cs) and the primary type inside follows the same naming (for example, FilesController). The exemplars in that folder show the same [ApiController] attribute usage and ControllerBase inheritance.

## Wiring

A registration or composition site was not detected in the provided wiring-site list. Inspect the existing controllers in src/EchoHub.Server/Controllers to see how they are referenced and instantiated by the application; use those exemplars as the immediate models for how a new controller is structured and how it expects dependencies to be supplied.

## Existing examples

- [`AuthController`](../../Code/src/EchoHub.Server/Controllers/AuthController.cs.md)
- [`ChannelsController`](../../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md)
- [`FilesController`](../../Code/src/EchoHub.Server/Controllers/FilesController.cs.md)
- [`ModerationController`](../../Code/src/EchoHub.Server/Controllers/ModerationController.cs.md)
- [`ServerController`](../../Code/src/EchoHub.Server/Controllers/ServerController.cs.md)
- [`UsersController`](../../Code/src/EchoHub.Server/Controllers/UsersController.cs.md)

Controllers are ordinary ASP.NET Core controller classes that derive from ControllerBase, expose routes with attributes like [Route] and [HttpGet], and receive services via constructor injection (the reference shows FileStorageService injected into FilesController). Follow the exemplars for attribute usage, route patterns, and how dependencies are consumed when adding a new controller.

---
*Synthesised by Aurion on 2026-07-08 17:09:56 UTC*
