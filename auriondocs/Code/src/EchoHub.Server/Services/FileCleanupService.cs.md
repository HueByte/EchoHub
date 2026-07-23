# FileCleanupService

> **File:** `src/EchoHub.Server/Services/FileCleanupService.cs`  
> **Kind:** class

```csharp
public sealed class FileCleanupService : BackgroundService
```


FileCleanupService is a hosted background worker that periodically deletes files older than a configured retention window from a storage directory determined by configuration. It reads its interval and retention settings from configuration, selects a path (configured path or a sensible default under the application base directory), and logs its activity while reliably continuing after errors.

## Remarks
FileCleanupService encapsulates cleanup policy behind a dedicated background service, so cleanup logic is not sprinkled across the app. It uses dependency-injected `IConfiguration` and `ILogger<FileCleanupService>` to stay configurable and observable, and it handles exceptions without bringing down the service. The cleanup operation is intentionally conservative: files are deleted only if their creation time UTC is older than the computed cutoff, and per-file errors are logged and do not stop processing of the rest.

## Notes
- If the storage path does not exist or is not configured, the cleanup is skipped gracefully.
- Defaults are applied when configuration values are missing or invalid: `Storage:CleanupIntervalHours` defaults to 1, `Storage:RetentionDays` defaults to 30.
- If files are in use or cannot be deleted due to permissions, the service logs a warning and continues with the remaining files.
