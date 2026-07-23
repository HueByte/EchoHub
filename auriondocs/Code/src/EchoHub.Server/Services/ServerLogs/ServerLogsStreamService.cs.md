# ServerLogsStreamService

> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsStreamService : BackgroundService
```


Streams queued log events to the live log room as ephemeral SignalR messages, never persisting them to the IRC gateway or a database, and with a guard against introducing new logging from the streaming path itself. It runs as a background service, ensuring the destination room exists before sending each event and recreating it if needed, so live viewers can always join the stream without manual intervention.

## Remarks
This symbol acts as a thin, resilient bridge between the server-side log sink and the real-time chat hub. It separates the streaming path from log persistence, enforcing a no-log-from-stream policy to avoid feedback loops where streaming would itself generate more log lines. By lazily resolving the hub context, it avoids tight coupling during service construction and ensures the ChatHub context is available when streaming begins. The class also enforces room existence in a lightweight, interval-bounded way to tolerate transient room removal without blocking the live stream.

## Notes
- The streaming path must never emit logs of its own activity; per-event logging is explicitly suppressed to prevent cascading streams.
- TryEnsureRoomAsync re-checks the room at most once per EnsureInterval (15 seconds) to balance responsiveness with avoiding repeated recreation attempts.
- Messages are formatted and then encrypted before sending; the client receives an encrypted payload and is responsible for decrypting it, mirroring the design that prioritizes privacy and transport safety. The public Format method is exposed for tests, reflecting a desire to validate formatting behavior in isolation.
- If room recreation fails, events are streamed to a group with no members until the next interval, ensuring that the streaming pipeline remains non-blocking and resilient to transient failures.

## Example
- Not included: non-obvious usage from the signature; the behavior is exercised through the background streaming loop and the TryEnsureRoomAsync room-recovery logic. See the source for exact flow and state transitions. 

## Dependency APIs (verified signatures)
The REAL, parser-verified API surface of this symbol's collaborators:

- MessageDto (src/EchoHub.Core/DTOs/ChatDtos.cs)
- Reader (src/EchoHub.Server/Services/ServerLogs/ServerLogsSink.cs)
- ServerLogsService (src/EchoHub.Server/Services/ServerLogs/ServerLogsService.cs)
  - SenderName, RoomTopic, TimestampFormat, TailReadBytes
  - ServerLogsService(ServerLogsOptions options)
  - Options, IsLogsChannel, CanView(ServerRole)
  - ReadBacklog(), GroupIntoEntries(`IReadOnlyList<string>`, bool, int)
  - TryParseTimestamp(string, out DateTimeOffset, out string)
- HubContext (src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs)
- HubConstants (src/EchoHub.Core/Constants/HubConstants.cs)
  - ChatHubPath, DefaultChannel, IrcConnectionIdPrefix, DefaultHistoryCount, MaxMessageLength
  - MaxImageSizeBytes, MaxAudioFileSizeBytes, MaxFileSizeBytes, MaxAvatarSizeBytes
  - MaxMessageNewlines, MaxAttachmentsPerMessage, MaxConsecutiveNewlines
  - …and 7 more member(s) not shown

## Symbol To Document
- Name: ServerLogsStreamService
- Kind: class
- File: src/EchoHub.Server/Services/ServerLogs/ServerLogsStreamService.cs
- Language: csharp
- ID: fe54ac96-642e-4dbe-af25-3d2559e01299