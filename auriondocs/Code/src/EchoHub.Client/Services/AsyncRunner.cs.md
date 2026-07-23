# AsyncRunner

> **File:** `src/EchoHub.Client/Services/AsyncRunner.cs`  
> **Kind:** class

```csharp
public static class AsyncRunner
```


Runs the provided asynchronous work on a background thread and routes exceptions to the UI, eliminating boilerplate like `Task.Run`/try/catch/`app.Invoke(ShowError)`.

`AsyncRunner.Run` takes an `IApplication` (`app`), a `Func<Task>` representing the work, an `Action<string>` (`showError`), a string (`errorPrefix`) used in the user-facing error, and an optional `string? logContext` to enrich logs; if an exception occurs, it logs with `Log.Error` and invokes the UI thread to display the error via `showError`.

This pattern centralizes background execution and UI-error reporting, so callers need only supply the work and error message components and can rely on consistent logging and user feedback.

## Remarks
This abstraction isolates the cross-cutting concerns of background execution and UI error presentation. By encapsulating this pattern, it avoids duplicating boilerplate across call sites and ensures errors are logged with contextual information and surfaced on the UI thread via `IApplication.Invoke`.

## Notes
- This method is fire-and-forget: it launches the work and does not return a `Task`; callers cannot await completion or observe exceptions from the caller's context. If you need completion signaling, consider returning a `Task` or providing a completion callback.