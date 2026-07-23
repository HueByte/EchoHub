# ServerStatsReportService

> **File:** `src/EchoHub.Server/Services/Stats/ServerStatsReportService.cs`  
> **Kind:** class

```csharp
public sealed class ServerStatsReportService : BackgroundService
```


Background service that periodically snapshots server activity over a rolling window, logs a pretty-printed JSON snapshot (which surfaces in the live server-logs room), and persists the results to the database for historical trends. It reads cadence and retention from [`StatsOptions`](../../Config/StatsOptions.cs.md) and coordinates with [`PresenceTracker`](../PresenceTracker.cs.md), [`ServerStatsCollector`](ServerStatsCollector.cs.md), and [`EchoHubDbContext`](../../Data/EchoHubDbContext.cs.md) to compute windowed metrics such as messages sent, active members, attachments uploaded, and user counts.

## Remarks
This symbol acts as an orchestration point between live state, in-memory counters, and durable storage to provide a stable, windowed view of server activity. It builds a [`ServerStatsReport`](../../../EchoHub.Core/Models/ServerStatsReport.cs.md) for each interval and uses a dedicated scope to query [`EchoHubDbContext`](../../Data/EchoHubDbContext.cs.md), ensuring isolation from other requests. By anchoring the window to `_periodStart` and `periodEnd`, it aligns in-memory counters with database-derived counts to avoid drift.

## Notes
- The interval is computed from `StatsOptions.IntervalHours`; non-positive values default to 6 hours and the interval is clamped to at least 1 second to prevent a runaway loop.
- Restarting the service resets the reporting window; data prior to the restart belongs to the previous period and will not be included in the new interval unless recalculated by the next run.