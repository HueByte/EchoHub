# FileCleanupService

> **File:** `src/EchoHub.Server/Services/FileCleanupService.cs`  
> **Kind:** class

```csharp
public sealed class FileCleanupService : BackgroundService
```


FileCleanupService runs as a background service and periodically removes files from a configured storage directory that are older than a configured retention window. It reads CleanupIntervalHours and RetentionDays from configuration (defaulting to 1 hour and 30 days) and uses Storage:Path (or a default uploads folder under AppContext.BaseDirectory) as the target directory, logging its startup parameters.

## Remarks
Why this abstraction exists: it isolates file housekeeping from request handling, ensuring the uploads directory doesn't grow uncontrolled while keeping runtime behavior adjustable via configuration. The service uses UTC timestamps to compare ages, handles per-file deletion failures gracefully, and continues operation even if some deletions fail. It currently operates only on the top-level files in the storage path (no recursive traversal), which is important to understand when you organize files into subfolders.

## Notes
- Only top-level files are cleaned; subdirectories are ignored, so old files inside subfolders won't be purged.
- Deletion failures for individual files are logged at warning level, but do not stop the cleanup loop.
- Creation time is used to determine age; be aware that copied/moved files may preserve their original creation time.