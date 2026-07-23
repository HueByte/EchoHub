# ServerLogsService.cs

> **Source:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsService.cs`

*Figure: How ServerLogsService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
ServerLogsService["Start ReadBacklog()"]
ServerLogsOptions["Load ServerLogsOptions (LogDirectory, LogFilePattern, BacklogLines)"]
LogBacklogEntry["Return IReadOnlyList of LogBacklogEntry (backlog or empty)"]

ServerLogsService -->|"Resolve full path of LogDirectory"| ServerLogsOptions
ServerLogsOptions -->|"If directory does not exist -> return empty list"| LogBacklogEntry
ServerLogsOptions -->|"Find newest file matching LogFilePattern (order by LastWriteTimeUtc)"| ServerLogsService
ServerLogsService -->|"If no newest file -> return empty list"| LogBacklogEntry
ServerLogsService -->|"Open FileStream(newest.FullName, FileMode.Open, FileAccess.Read, FileShare ReadWrite and Delete)"| ServerLogsOptions
ServerLogsOptions -->|"Determine if stream.Length > TailReadBytes (seeked)"| ServerLogsService
ServerLogsService -->|"If seeked -> stream.Seek(-TailReadBytes, SeekOrigin.End)"| ServerLogsOptions
ServerLogsOptions -->|"Read remainder with StreamReader and split into lines"| ServerLogsService
ServerLogsService -->|"Call GroupIntoEntries(lines, skipLeadingContinuations: seeked, BacklogLines)"| LogBacklogEntry
ServerLogsService -->|"On any exception -> return empty list"| LogBacklogEntry
```

## Contents

- [ServerLogsService](#serverlogsservice)
- [LogBacklogEntry](#logbacklogentry)

---

## ServerLogsService
> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsService.cs`  
> **Kind:** class

```csharp
public sealed class ServerLogsService
```


Provides utilities for exposing a read-only, live server log room: it knows the room identity and access gate and can read the tail of the current rolling log file into `LogBacklogEntry` items for display. Use `ServerLogsService` when you need to determine whether a channel is the configured logs room, check whether a role may view logs, or retrieve a best-effort backlog snapshot from the most recent log file (rather than relying on persisted messages).

## Remarks
`ServerLogsService` centralizes the concerns around presenting live server logs without persisting log lines as messages. It uses the configured [`ServerLogsOptions`](../../Config/ServerLogsOptions.cs.md) to decide whether logging is enabled, to match a channel name (`NormalizedRoomName`) in `IsLogsChannel`, and to gate access with `CanView` based on `MinRole`. For backlog retrieval, `ReadBacklog` opens the newest file matching `LogFilePattern` in `LogDirectory` with `FileShare.ReadWrite | FileShare.Delete` (to cooperate with a rolling sink like Serilog), reads up to `TailReadBytes` from the file end, and converts raw lines into `LogBacklogEntry` instances via `GroupIntoEntries`. The `GroupIntoEntries` method is public to allow unit testing of the timestamp-based grouping logic.

## Notes
- `IsLogsChannel` calls `Trim()` on the provided `channelName`; passing `null` will throw a `NullReferenceException` — callers should ensure they pass a non-null string or guard accordingly.
- `ReadBacklog` is intentionally best-effort: it catches all exceptions and returns an empty list on any I/O or parsing failure. This prevents join failures but can hide filesystem problems; monitor logs or surface errors elsewhere if you need diagnostics.
- The grouping logic depends on lines that start with the timestamp format defined by `TimestampFormat`. If your log sink uses a different timestamp template, `GroupIntoEntries` will treat those timestamped lines as continuations and entries will be merged incorrectly.
- When the newest file is larger than `TailReadBytes`, `ReadBacklog` seeks into the file and sets `skipLeadingContinuations` so a partial entry at the seek boundary is dropped. This is deliberate to avoid presenting truncated entries but means very long single entries near the file end can be partially excluded.

---

## LogBacklogEntry
> **File:** `src/EchoHub.Server/Services/ServerLogs/ServerLogsService.cs`  
> **Kind:** record

```csharp
public record LogBacklogEntry(DateTimeOffset Timestamp, string Content)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Timestamp` | `DateTimeOffset` | — |
| `Content` | `string` | — |


LogBacklogEntry is an immutable value object that captures a backlog entry read from the log file. It consists of a timestamp (`Timestamp`) and the associated content (`Content`), representing the first line of the backlog entry plus any continuation lines (such as exception stack traces) that followed it.

## Remarks
Because this is a `record`, it provides value-based equality and deconstruction, which simplifies comparing backlog entries and passing them through the processing pipeline without mutation. It acts as a lightweight data carrier that decouples raw log parsing from higher-level log aggregation or display concerns, allowing the server logs service to operate on coherent chunks of log data.

## Notes
- The `Content` may be large and contain newline characters representing multi-line stack traces; treat it as an opaque blob when storing or transmitting.

---