# UpdateChecker

> **File:** `src/EchoHub.Client/Services/UpdateChecker.cs`  
> **Kind:** class

```csharp
public sealed class UpdateChecker : IDisposable
```


Checks for application updates on a background schedule and coordinates a safe, post-TUI update process. Use `UpdateChecker` when you want automatic or on-demand update checks inside a Terminal.Gui-based host but need the actual download/extract/restart work to run after the UI main loop has exited.

## Remarks
`UpdateChecker` encapsulates the interaction between the UI, a periodic `Updater` and the host process that must perform the actual update. It listens for `Updater` events and, when the user confirms an update via `UpdateConfirmDialog.Show`, sets the public [`PendingUpdate`](../AppOrchestrator.cs.md) delegate and requests the UI to stop so the host can perform the heavy work on a plain console. This design avoids the console deadlock that would occur if the updating process tried to restart while the Terminal.Gui main loop still owned the console. `CurrentVersion` exposes the assembly version used in the confirmation UI.

## Example
```csharp
// During application startup
var checker = new UpdateChecker(app);
checker.Start(); // starts periodic checks in RELEASE builds

// Trigger a manual check from UI or command handler
await checker.CheckNowAsync();

// After the Terminal.Gui main loop exits, the host should run any pending update
if (checker.PendingUpdate != null)
{
    await checker.PendingUpdate(); // will download/extract and may restart the process
}
```

## Notes
- [`PendingUpdate`](../AppOrchestrator.cs.md) is only set when the user confirms an available update via `UpdateConfirmDialog.Show`; the host must check and invoke [`PendingUpdate`](../AppOrchestrator.cs.md) after the TUI main loop exits.
- `Start()` is conditional on the `RELEASE` build symbol — in non-RELEASE builds the periodic checker does not run.
- Invoking the [`PendingUpdate`](../AppOrchestrator.cs.md) delegate runs the updater on a plain console and may end by restarting the app (the code calls into the `Updater` which performs download/extract/restart). The host should not expect normal process continuation after the update completes.
- `CurrentVersion` reads the assembly version and will return `"0.0.0"` if the assembly version cannot be determined.
- Backup creation is attempted via `UpdateBackupService.CreateBackup()` before applying an update; failures are logged and the update continues without a backup.