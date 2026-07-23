# FileCleanupService

> **File:** `src/EchoHub.Server/Services/FileCleanupService.cs`  
> **Kind:** class

```csharp
public sealed class FileCleanupService : BackgroundService
```


FileCleanupService is a hosted background service that periodically deletes files in a configured storage directory that are older than a configured retention period. It reads settings from configuration, chooses the target path, and logs progress while running until cancellation.

## Remarks
The cleanup logic is encapsulated in a dedicated BackgroundService to centralize disk-space hygiene and keep it decoupled from request-driven code. It relies on dependency-injected IConfiguration and ILogger to determine interval, retention, and storage path, and to report status and errors. Cleanup runs in a cancellation-friendly loop and uses UTC timestamps to compare age, making behavior predictable across servers.

## Notes
- It only considers files directly within storagePath; subdirectories are not scanned. If you need recursive cleanup, switch to Directory.GetFiles(storagePath, "*", SearchOption.AllDirectories) and adjust the cutoff logic accordingly.
- File age is determined by GetCreationTimeUtc; if your deployment uses different semantics (e.g., files moved or uploaded), consider using GetLastWriteTimeUtc or metadata-based age checks.