# Update management

> Data and update flow: backup prior to updates and update checks.

Update management

This topic covers the client-side update workflow: detecting available versions from the running Terminal.Gui app, deferring heavy update work until the UI has shut down, and snapshotting the application state so you can roll back if an update goes wrong. The two files coordinate a safe in-place updater by separating user interaction and terminal ownership (in UpdateChecker) from the filesystem snapshot and metadata (in UpdateBackupService).

## UpdateBackupService.cs
Provides backup of user data before updates.

The file declares three related symbols that implement pre-update snapshotting. [BackupJsonContext](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) is an internal, source-generated JsonSerializerContext that supplies reflection-free JSON metadata for serializing the on-disk metadata type. The public [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) static class performs the actual backup/rollback responsibilities: it creates a ZIP snapshot of the running application under ~/.echohub/update-backup/ (backup.zip) and writes a companion backup-info.json (the [BackupInfo](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) contract) that records the version, application directory, and UTC timestamp. The service exposes operations to CreateBackup before applying an update, to check presence via BackupExists, and to read metadata with GetBackupInfo; it also exposes an IsPostUpdate flag that lets startup logic detect a recent update backup and react accordingly. Because the JSON context is internal and generated, callers within the assembly configure JsonSerializerOptions with BackupJsonContext when they read or write backup-info.json.

The file is used by the update coordination logic in [UpdateChecker](../Code/src/EchoHub.Client/Services/UpdateChecker.cs.md): the checker defers the updater work but relies on UpdateBackupService to attempt a pre-update snapshot when the update is actually applied.

## UpdateChecker.cs
Checks for updates and coordinates update flow.

[UpdateChecker](../Code/src/EchoHub.Client/Services/UpdateChecker.cs.md) is a disposable helper that runs background polling and supports manual checks, while keeping all user interaction on the provided Terminal.Gui IApplication main loop. Its responsibilities are: poll for newer versions via an internal Updater, present a TUI confirmation dialog by marshalling callbacks with _app.Invoke, and — crucially — avoid performing download/extract/restart while the TUI still owns the terminal. When the user accepts an update, UpdateChecker sets PendingUpdate to an awaitable delegate (the internal ApplyUpdateAsync) and captures the chosen version, then signals the TUI to stop; the host is expected to call PendingUpdate after the main loop exits so the update can run headless and safely restart the process.

Concrete behaviors documented in the class include: Start() only activates the periodic poller in RELEASE builds; PendingUpdate is intentionally a Task-returning delegate to be invoked by the host after the console is restored; ApplyUpdateAsync attempts to create a pre-update backup by calling [UpdateBackupService.CreateBackup](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) and logs but does not fail the update flow if backup creation fails; ApplyUpdateAsync also sets Console.OutputEncoding = UTF8 while swallowing exceptions for non-interactive stdout; and CurrentVersion reads the assembly version with a fallback of "0.0.0".

How the pieces fit

- Update detection and user confirmation happen inside [UpdateChecker](../Code/src/EchoHub.Client/Services/UpdateChecker.cs.md) running on the Terminal.Gui main loop; when the user accepts an update, the checker defers the actual work by setting PendingUpdate and requesting the TUI to stop.
- The deferred update work (ApplyUpdateAsync) calls into [UpdateBackupService](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) to snapshot the application: it writes backup.zip and backup-info.json (the serialized [BackupInfo](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md) using [BackupJsonContext](../Code/src/EchoHub.Client/Services/UpdateBackupService.cs.md)). Backup creation failures are logged but do not block the update.
- The host is responsible for invoking PendingUpdate only after the TUI main loop has fully exited and the console is restored, at which point the update runs headless (and may restart the process).

---
*Covers 2 of 2 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:54:19 UTC*
