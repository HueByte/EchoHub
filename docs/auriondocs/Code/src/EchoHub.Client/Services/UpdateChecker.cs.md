# UpdateChecker

> **File:** `src/EchoHub.Client/Services/UpdateChecker.cs`  
> **Kind:** class

```csharp
public sealed class UpdateChecker : IDisposable
```


UpdateChecker is a sealed class that coordinates the in-app update flow for the EchoHub client. It uses a dedicated Updater to fetch a version manifest from a remote URL and, when an update is available, guides the user through confirmation, a pre-update backup, and the download-and-apply process. It surfaces progress via UpdateProgressDialog and centralizes the update UX so callers do not need to duplicate the orchestration logic. The CurrentVersion helper exposes the installed version derived from the executing assembly.

## Remarks
UpdateChecker serves as the UI-facing mediator between the low-level Updater service and the user experience. By encapsulating the policy around updating (user confirmation, backup creation, and progress reporting), it keeps the update flow consistent across the application. It relies on IApplication to marshal UI work to the main thread, maintaining responsiveness even during lengthy downloads.

## Example
```csharp
// Example usage: trigger a manual update check.
var updater = new UpdateChecker(app);
await updater.CheckNowAsync();
```

## Notes
- The OnUpdateAvailable handler is async void, which can lead to unobserved exceptions; be aware that exceptions raised inside this handler may not propagate to the caller.
- Start() is compiled only in RELEASE builds; in debug builds, automatic update checks are not started and you must initiate checks manually (e.g., via CheckNowAsync).
- The backup step occurs before the update download; if backup creation fails, the user is prompted whether to continue, and selecting Cancel aborts the update.