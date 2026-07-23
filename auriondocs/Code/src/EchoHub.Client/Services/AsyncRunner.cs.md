# AsyncRunner

> **File:** `src/EchoHub.Client/Services/AsyncRunner.cs`  
> **Kind:** class

```csharp
public static class AsyncRunner
```


Eliminates repeated Task.Run/try/catch/app.Invoke(ShowError) boilerplate by consolidating the common pattern of running background work and surfacing errors to the UI. It runs the provided async work on a background thread and routes any exceptions to the UI thread for user notification.

## Remarks
AsyncRunner encapsulates a cross-cutting concern: performing asynchronous work without blocking the UI and centralizing error reporting. It uses Task.Run to execute work off the calling thread and app.Invoke to marshal the error surface back to the UI. When an exception occurs, it logs the failure with the provided context (logContext if supplied, otherwise errorPrefix) and shows a UI message using showError prefixed by errorPrefix. Because Run is fire-and-forget (it returns void), callers should not rely on it for completion or exception propagation; choose a different pattern if you need to observe results.

## Notes
- This method is fire-and-forget; exceptions are caught and surfaced but not propagated to the caller.
- The UI update and logging rely on the provided IApplication and showError delegate; ensure they are safe to call from a background thread; app.Invoke is used to marshal to the UI thread.