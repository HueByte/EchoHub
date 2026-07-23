# FilesController

> **File:** `src/EchoHub.Server/Controllers/FilesController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/files")]
[Authorize]
[EnableRateLimiting("general")]
public class FilesController : ControllerBase
```


Serves an uploaded file anonymously by design: the unguessable GUID in the URL is the access token (Discord-CDN-style capability URL), so attachment links can be opened directly in a browser and shared to IRC clients. E2E-encrypted room blobs are ciphertext at rest, so anonymous access reveals nothing for those channels.

## Remarks
This symbol provides a minimal, token-based file access surface that does not require user authentication. It delegates path resolution to FileStorageService and consolidates content-type handling in one place, so callers can rely on consistent delivery behavior across file types. The design emphasizes shareable, browser-friendly links while safeguarding sensitive payloads behind the GUID-based URL.

## Notes
- The endpoint validates the fileId as a GUID before attempting any storage access; invalid IDs yield a BadRequest response.
- Content types are determined by file extension with a broad fallback to application/octet-stream; unknown extensions will download as a generic binary.
- Images and audio files are rendered inline in the browser, while other types are delivered as attachments with the original file name.
