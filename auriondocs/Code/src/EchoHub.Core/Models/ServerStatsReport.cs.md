# ServerStatsReport

> **File:** `src/EchoHub.Core/Models/ServerStatsReport.cs`  
> **Kind:** class

```csharp
public class ServerStatsReport
```


Represents a snapshot of server activity for a single reporting window, produced periodically by the stats-report background job. It captures timing data (PeriodStart, PeriodEnd, WindowHours, GeneratedAt) and per-window metrics (MessagesSent, FilesUploaded, BytesUploaded, NewMembers, ActiveMembers, Connections, Disconnections, Kicks, Bans) as well as end-of-window totals (TotalMembers, OnlineNow, PeakOnline) for persistence as pretty-printed JSON.

## Remarks
Serves as a stable, serializable container for periodic server activity, enabling dashboards and trend analyses to compare windows over time. By separating window semantics (start/end, duration) from generation time, it supports reliable aggregation and rhythm-based alerts when metrics diverge.

## Notes
- GeneratedAt is intended to equal PeriodEnd; ensure synchronization when populating the model. The default initializer uses DateTimeOffset.UtcNow, which may diverge if PeriodEnd is set to a different value.

## Dependencies
- DateTimeOffset (System) — used for all timestamp properties on the model.