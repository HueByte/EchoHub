# Adding a new controller

> *Workflow template auto-derived from 7 existing exemplar(s).*

Adding a new controller in this codebase means adding an ASP.NET Core API controller class under src/EchoHub.Server/Controllers that follows the shape shown in the reference FilesController. A developer would reach for this pattern when they need to expose a new HTTP API surface: controllers are decorated with controller attributes, derive from ControllerBase, and implement actions (HttpGet/HttpPost/etc.) that the framework routes to.

## Reference implementation

Real code from src/EchoHub.Server/Controllers/FilesController.cs that you can model a new controller on:

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

Controllers in this project appear under src/EchoHub.Server/Controllers and use the Controller naming form (for example FilesController, AuthController, ChannelsController, etc.). Each controller is a class that carries the [ApiController] attribute and derives from ControllerBase; routing is provided with [Route("...")] on the class and action attributes like [HttpGet] on methods.

## Wiring

A registration/composition site for controllers was not detected in the provided wiring list. To see how controllers are used and how their action surface looks in practice, inspect the existing controllers listed in "Existing examples" below and model your new controller on those files.

## Existing examples

- [`AuthController`](../../Code/src/EchoHub.Server/Controllers/AuthController.cs.md)
- [`ChannelsController`](../../Code/src/EchoHub.Server/Controllers/ChannelsController.cs.md)
- [`FilesController`](../../Code/src/EchoHub.Server/Controllers/FilesController.cs.md)
- [`InvitesController`](../../Code/src/EchoHub.Server/Controllers/InvitesController.cs.md)
- [`ModerationController`](../../Code/src/EchoHub.Server/Controllers/ModerationController.cs.md)
- [`ServerController`](../../Code/src/EchoHub.Server/Controllers/ServerController.cs.md)
- [`UsersController`](../../Code/src/EchoHub.Server/Controllers/UsersController.cs.md)

---
*Synthesised by AurionDocs on 2026-07-23 09:35:00 UTC*
