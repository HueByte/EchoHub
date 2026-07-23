# ServerLogsSink

> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsSink.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsSink : ILogEventSink
```


ServerLogsSink is a Serilog sink that buffers recent `LogEvent`s into a bounded, single-reader [`Channel<LogEvent>`](../../../EchoHub.Core/Models/Channel.cs.md) and exposes a `Reader` for the live log streaming path. It enforces a minimum level via the `ServerLogsOptions.MinLevel` and filters out internal streaming sources using `ExcludedSourcePrefixes` to prevent a feedback loop where a log would broadcast and re-log itself.

## Remarks
Serving as a bridge between Serilog and the live log room, `ServerLogsSink` deliberately does not write to a database; events are queued for streaming consumption by [`ServerLogsStreamService`](ServerLogsStreamService.cs.md). The channel is sized with a capacity of 512 and uses `BoundedChannelFullMode.DropOldest` with `SingleReader = true`, which preserves the most recent events while avoiding unbounded memory growth. The internal filtering — checking `Constants.SourceContextPropertyName` and skipping any source that starts with entries in `ExcludedSourcePrefixes` — protects against recursive logging from the streaming infrastructure.

## Notes
- The bound buffer capacity is 512 and uses `BoundedChannelFullMode.DropOldest`; when full, the oldest buffered events are dropped to make room for newer ones.
- `Emit` uses `TryWrite` and ignores the return value; under load, logs may be dropped if the consumer lags behind.
- Internal sources are excluded by prefix; adding new internal namespaces requires updating `ExcludedSourcePrefixes` to avoid self-logging.
