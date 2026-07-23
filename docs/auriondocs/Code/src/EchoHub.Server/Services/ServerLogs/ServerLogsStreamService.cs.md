# ServerLogsStreamService

> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsStreamService : BackgroundService
```


Description:

Streams queued log events to the live log room as ephemeral SignalR messages, ensuring the room exists before each publish and recreating it on demand if it was removed. The streaming path is intentionally non-logging to avoid recursive logging and potential message sprawl. Use this service when you want real-time, in-memory broadcasts of server log events to connected clients without persisting those lines to a database.

## Remarks

This symbol acts as the dedicated conduit between the server-side log sink and the live chat hub. It coordinates with a channel service to guarantee the existence of the log room (and to recreate it if it disappears), throttling such housekeeping to at most once every 15 seconds to avoid excessive churn. Messages are encrypted before transmission and delivered to the room group via a lazily-resolved `HubContext`, which is intentionally retrieved only after the host has fully configured its DI graph. The static `Format` helper is public for tests, enabling validation of the exact, client-rendered payload without instantiating the streaming pipeline. This separation keeps streaming concerns isolated from the rest of the logging infrastructure and prevents per-event logging from leaking into the stream itself.

## Notes

- The service reads from `ServerLogsSink.Reader` and, for each event, ensures the destination room exists, formats the log event, encrypts the payload, and sends it to the [`ChatHub`](../../Hubs/ChatHub.cs.md) group corresponding to the room name.
- Room creation/verification is throttled by `EnsureInterval` (15 seconds) to avoid excessive calls during high-frequency log bursts; failed attempts are silently retried on the next interval.
- The streaming path is guarded to swallow non-cancellation exceptions to prevent re-entrancy into the logging pipeline.
- The `Format` method is intentionally public for testability, and truncates messages to `HubConstants.MaxMessageLength` with an ellipsis when necessary.

## Example

```csharp
// The example demonstrates formatting a log event for client rendering and ensuring the message is wrapped for transport.
var logEvent = new LogEvent(/* parameters omitted for brevity */);
var payload = ServerLogsStreamService.Format(logEvent);
// payload is then embedded in a [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md), encrypted, and sent to the SignalR hub.
```

## Dependencies

- SignalR, BackgroundService, MessageDto, StringBuilder, TimeSpan, DateTimeOffset, Reader, Guid

## Dependency APIs (verified signatures)

The REAL, parser-verified API surface of this symbol's collaborators:

- record [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) (`src/EchoHub.Core/DTOs/ChatDtos.cs`)
- property `Reader` (`src/EchoHub.Server/Services/ServerLogs/ServerLogsSink.cs`)
- class [`ServerLogsService`](ServerLogsService.cs.md) (`src/EchoHub.Server/Services/ServerLogs/ServerLogsService.cs`)
  - field `string SenderName`
  - field `string RoomTopic`
  - field `string TimestampFormat`
  - field `int TailReadBytes`
  - `ServerLogsService(ServerLogsOptions options)`
  - property `ServerLogsOptions Options`
  - `bool IsLogsChannel(string channelName)`
  - `bool CanView(ServerRole role)`
  - `IReadOnlyList<LogBacklogEntry> ReadBacklog()`
  - `IReadOnlyList<LogBacklogEntry> GroupIntoEntries(IReadOnlyList<string> lines, bool skipLeadingContinuations, int maxEntries)`
  - `bool TryParseTimestamp(string line, out DateTimeOffset timestamp, out string rest)`
- property `HubContext` (`src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs`)
- class [`HubConstants`](../../../EchoHub.Core/Constants/HubConstants.cs.md) (`src/EchoHub.Core/Constants/HubConstants.cs`)
  - field `string ChatHubPath`
  - field `string DefaultChannel`
  - field `string IrcConnectionIdPrefix`
  - field `int DefaultHistoryCount`
  - field `int MaxMessageLength`
  - field `int MaxImageSizeBytes`
  - field `int MaxAudioFileSizeBytes`
  - field `int MaxFileSizeBytes`
  - field `int MaxAvatarSizeBytes`
  - field `int MaxMessageNewlines`
  - field `int MaxAttachmentsPerMessage`
  - field `int MaxConsecutiveNewlines`
  - …and 7 more member(s) not shown

## Symbol To Document
- Name: `ServerLogsStreamService`
- Kind: class
- File: `src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs`
- Language: `csharp`
- ID: 24389698-e5ce-4385-b392-f34e08edf31f
