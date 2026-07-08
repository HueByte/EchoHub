# FileStorageService

> **File:** `src/EchoHub.Server/Services/FileStorageService.cs`  
> **Kind:** class

```csharp
public class FileStorageService
```


FileStorageService provides a lightweight, filesystem-backed mechanism to persist uploaded streams to disk. It reads a storage path from configuration (Storage:Path) and ensures the directory exists, creating it if necessary. SaveFileAsync stores the provided stream as a new file using a GUID as the file name (preserving the original extension) and returns both the generated fileId and the actual file path. GetFilePath looks up a file by its fileId and returns the path if found, otherwise null. DeleteFile removes the file associated with a given fileId.

## Remarks
The service isolates disk I/O from higher layers, enabling swapping storage implementations behind this simple API. It derives its storage location from IConfiguration and guarantees the storage directory exists at initialization, making it ready for immediate use. File identities are GUID-based to avoid collisions and to avoid exposing full filesystem paths to clients. GetFilePath provides a lightweight lookup by id, while DeleteFile performs a best-effort removal of the targeted file.

## Example
```csharp
// Example usage of FileStorageService
var storage = new FileStorageService(configuration);

using var stream = File.OpenRead("path/to/source.png");
var (id, path) = await storage.SaveFileAsync(stream, "source.png");
// id is the GUID-based identifier; path is the stored location on disk

var storedPath = storage.GetFilePath(id);

storage.DeleteFile(id);
```

## Notes
- This implementation writes to the local filesystem and is best suited for single-host deployments. For multi-node or cloud deployments, use shared storage or an alternative backend.
- GetFilePath returns the first match for the given id (there should only be one); if multiple matches somehow exist, the behavior is to return the first discovered file.
- DeleteFile performs a best-effort deletion; if permissions or locks prevent removal, an exception may be thrown by the underlying IO calls.