# ServerLogsOptions

> **File:** `src/EchoHub.Server/Config/ServerLogsOptions.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsOptions
```


`ServerLogsOptions` is a configuration model that binds to the `ServerLogs` configuration section (and supports environment overrides via keys like `ServerLogs__Enabled`). When `Enabled` is true, the system automatically creates a read-only system channel named from `RoomName` (default `server-logs`) and streams log events to that channel in real time; log lines are never persisted as messages in the database, with persistence limited to the rolling `Serilog` log files. It exposes several tunables: `RoomName` sets the auto-created channel name and must satisfy the normal channel-name rules; the name is reserved so users cannot create a channel with it. `MinRole` defines the minimum server role that can see and join the log room (default `ServerRole.Mod`); `MinLevel` selects the minimum log event level to stream (default `LogEventLevel.Information`); `BacklogLines` controls how many recent log entries are replayed from the log files when the room is opened (default 100); `LogDirectory` and `LogFilePattern` point to where the rolling log files live and how they are named (defaults `logs` and `echohub-server-*.log`). The derived `NormalizedRoomName` provides a lowercased, trimmed variant of `RoomName` for comparisons.

## Remarks
The `ServerLogsOptions` abstraction centralizes live-log streaming behind a configuration object, separating real-time visibility from persistent message storage. It ensures a consistent, auto-created channel for server logs (named by `RoomName`, default `server-logs`) and uses `MinRole`/`MinLevel` to control who and what they can see, without requiring code changes to enable the feature. The `NormalizedRoomName` aids robust comparisons elsewhere in the system.

## Notes
- Live-streaming may expose sensitive information; ensure `MinRole` and `MinLevel` align with privacy expectations.
- Backlog replay relies on the Serilog file sink configuration; ensure `LogDirectory` exists and matches `LogFilePattern`.