# AsyncRunner

> **File:** `src/EchoHub.Client/Services/AsyncRunner.cs`  
> **Kind:** class

```csharp
public static class AsyncRunner
```


AsyncRunner runs a unit of asynchronous work on a background thread and routes any exception to the UI via the application's Invoke path, while logging a contextual error. Use it to replace repetitive Task.Run/try/catch/Invoke boilerplate when you need fire-and-forget background work with user-facing error reporting.

## Remarks
By centralizing this pattern, AsyncRunner decouples the background execution from UI error presentation and ensures a consistent logging context. It assumes a UI framework where IApplication.Invoke marshals work back to the UI thread; the error prefix and optional logContext produce readable, contextual messages in logs and UI.

## Notes
- This method does not guard against exceptions thrown by the UI invocation path or the showError delegate; those exceptions will propagate on the UI thread or crash the background task.
- The caller cannot cancel or await the background work; it's fire-and-forget.
- If work completes successfully, nothing is observed back to the caller.