# FileStorageService

> **File:** `src/EchoHub.Server/Services/FileStorageService.cs`  
> **Kind:** class

```csharp
public class FileStorageService
```


FileStorageService persists uploaded data to a local disk storage location, creating the directory if it does not exist and selecting the path from configuration (Storage:Path) or defaulting to an uploads folder beside the application. Each saved file is assigned a GUID-based fileId and stored with its original extension; the API supports SaveFileAsync, GetFilePath, GetStoredFileIds, and DeleteFile for common lifecycle operations.

## Remarks

By centralizing disk interactions, this service hides filesystem details from callers and provides a single, testable abstraction for storing attachments. It guarantees the storage directory exists and maps between a stable fileId and the corresponding on-disk file (preserving the extension). The GetStoredFileIds method performs a single directory scan to facilitate bulk checks across many files without per-file I/O.

## Notes

- The storage path is captured at construction time; changes to configuration after construction won't affect this instance.
- No validation of the incoming stream’s content type or size is performed here; enforce validation at call sites if needed.
- DeleteFile uses GetFilePath to locate the file before deleting and becomes a no-op if the file does not exist.