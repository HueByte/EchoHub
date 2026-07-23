# UpdateChecker

> **File:** `src/EchoHub.Client/Services/UpdateChecker.cs`  
> **Kind:** class

```csharp
public sealed class UpdateChecker : IDisposable
```


Checks for application updates in the background, presents a TUI confirmation dialog when a new version is available, and defers the actual download/extract/restart work until after the terminal UI has been shut down. Use this class when the host application runs a Terminal.Gui main loop and needs a safe way to offer in-place updates without deadlocking the console or performing heavy I/O while the TUI still owns the terminal.

## Remarks
This class encapsulates polling and manual update checks via an internal Updater instance and marshals user interaction back onto the provided IApplication main loop using _app.Invoke. When the user confirms an update, UpdateChecker does not perform the network/download work immediately; instead it sets PendingUpdate to an awaitable callback (ApplyUpdateAsync), stores the selected version, and requests the TUI to stop. The host is expected to call PendingUpdate after the main loop exits and the console has been restored so the update process can safely run headless (the Updater's update flow may restart the process and call Environment.Exit).

## Example
```csharp
// During application startup
var updateChecker = new UpdateChecker(app);
updateChecker.Start(); // starts periodic checks in RELEASE builds

// ... run Terminal.Gui main loop ...

// After the main loop exits and the console is restored, run any pending update
if (updateChecker.PendingUpdate != null)
{
    await updateChecker.PendingUpdate();
}
```

## Notes
- Start only activates the background poller in RELEASE builds (the Start method is no-op in non-RELEASE builds).
- PendingUpdate is deliberately set to a Task-returning delegate and intended to be invoked by the host after the TUI has fully stopped; running it while the TUI still owns the console can deadlock the restart flow.
- ApplyUpdateAsync attempts to create a pre-update backup with UpdateBackupService.CreateBackup; backup creation failures are logged and the update continues.
- ApplyUpdateAsync sets Console.OutputEncoding = UTF8 but swallows exceptions (useful when stdout is redirected or non-interactive).
- CurrentVersion reads the assembly version and falls back to "0.0.0" if unavailable.
