# ServerLogsOptions

> **File:** `src/EchoHub.Server/Config/ServerLogsOptions.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsOptions
```


Live server-log room configuration is encapsulated by this strongly-typed options class. It binds to the ServerLogs config section and supports environment overrides, controlling whether the live streaming channel is created and who can view it. When Enabled is true, a read-only system channel is auto-created and log events are streamed in real time; log lines themselves are not stored as messages in the database, with persistence remaining in the rolling Serilog log files.