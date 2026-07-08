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


Exposes a GET endpoint to serve a stored file by GUID at /api/files/{fileId}. It validates the fileId, resolves the on-disk path via FileStorageService, and returns the file with a content-type inferred from the extension (falling back to application/octet-stream); if the file is missing it yields 404 and if the ID is invalid it yields 400.

## Remarks
By centralizing file serving in this controller, the system gains a single, testable boundary for binary assets and consistent error handling via ErrorResponse. It delegates path resolution to FileStorageService, decoupling storage concerns from the controller and enabling swapable storage backends or easier unit testing. The action is decorated with ApiController, Authorize, and rate-limiting attributes to enforce authenticated access and controlled usage.

## Notes
- Content-type is determined by file extension; unrecognized extensions default to application/octet-stream. If you introduce new file types, extend the mapping accordingly.
- The endpoint depends on FileStorageService for path resolution; ensure its behavior is deterministic in tests and that it guards against exposing unintended filesystem paths.