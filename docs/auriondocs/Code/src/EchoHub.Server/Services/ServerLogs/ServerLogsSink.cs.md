# ServerLogsSink

> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsSink.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsSink : ILogEventSink
```


ServerLogsSink is a Serilog sink that feeds the live log room by buffering log events in a bounded channel and exposing them to the streaming pipeline without persisting them. It enforces a minimum log level, filters out internal sources to avoid feedback loops, and writes accepted events into a single-reader queue consumed by the live broadcast path. This sink thus serves as a lightweight, non-persistent conduit for real-time visibility of logging activity.

## Remarks
The sink decouples log emission from the live broadcast pathway, providing backpressure via a bounded channel (capacity 512) with drop-oldest semantics to prevent unbounded memory growth. Internal pipeline events are culled by inspecting the SourceContext and excluding known internal prefixes, which prevents the log → broadcast → log feedback loop. By not writing to a database, the component prioritizes timely visibility for operators and clients over long-term auditing.

## Notes
- When the channel is full, TryWrite may return false and the log event will be dropped, ensuring the application does not stall due to logging backpressure.
- The channel is configured with SingleReader = true, so there is a single consumer in the streaming path; additional readers would not receive the full event sequence.
- Only events that pass the MinLevel filter and do not originate from excluded internal sources are enqueued for broadcast.