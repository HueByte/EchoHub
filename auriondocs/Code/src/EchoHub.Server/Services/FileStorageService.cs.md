# FileStorageService

> **File:** `src/EchoHub.Server/Services/FileStorageService.cs`  
> **Kind:** class

```csharp
public class FileStorageService
```


FileStorageService is a lightweight on-disk storage helper for uploaded files. It derives its storage location from configuration under `Storage:Path` (defaulting to an `uploads` directory next to the application's base directory), ensures the directory exists, and provides basic operations to save, locate, enumerate, and delete files.

## Remarks
FileStorageService centers on a GUID-based identity for each stored file and writes files to a single storage directory as `"{fileId}{extension}"`. Retrieval by id uses a wildcard extension, so callers do not need to know the original file name or extension at lookup time. The `GetStoredFileIds` method performs one directory scan to produce the set of ids (filenames without extensions), enabling bulk checks of attachments without issuing a separate filesystem glob per id. The design hides actual file names from callers while preserving the original extension on disk to help downstream consumers infer content type. The constructor's path resolution and directory creation ensure a usable store is available up front, reducing boilerplate for calling code.

## Notes
- Initialization may throw if the configured storage path is invalid or cannot be created due to permissions.
- `DeleteFile` is safe to call for non-existent files; it simply becomes a no-op.
- `GetFilePath` relies on a pattern `"{fileId}.*"`; if multiple matches exist (e.g., due to external tampering), the first match is returned, which should be rare given the GUID-based ids.
