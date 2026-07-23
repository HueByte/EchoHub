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


Serves an uploaded file via the GET endpoint `api/files/{fileId}` and intentionally allows anonymous access through the unguessable GUID in the URL, enabling direct browser viewing or sharing of attachments. The controller uses [`FileStorageService`](../Services/FileStorageService.cs.md) (via `_fileStorage`) to resolve the file path and returns the file with an appropriate `Content-Type`; images and audio render inline in the browser, while other types trigger a download with the original filename. 

## Remarks

FilesController decouples storage concerns from HTTP delivery, enabling shareable, tokenized links without per-request authentication. It relies on `_fileStorage.GetFilePath` to verify existence and obtain a path, while validating the input with `Guid.TryParse` to guard against malformed requests. The design relies on the GUID in the URL as an access token, so the security model hinges on the token being effectively unguessable to limit exposure of attachments. 

## Notes

- Anonymous access means links can be shared; treat the `fileId` as a security token and rotate or revoke links as needed. 
- The MIME type is derived from the file extension via `Path.GetExtension`; ensure file extensions are correct to avoid misrepresented MIME types or unintended inline rendering. 
- Images and audio render inline (`Content-Type` starts with `image/` or `audio/`); all other files are delivered as attachments with the file name.