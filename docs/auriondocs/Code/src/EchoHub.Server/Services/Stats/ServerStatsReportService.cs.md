# ServerStatsReportService

> **File:** `src/EchoHub.Server/Services/Stats/ServerStatsReportService.cs`  
> **Kind:** class

```csharp
public sealed class ServerStatsReportService : BackgroundService
```


ServerStatsReportService is a background task that periodically snapshots server activity over the current reporting window, logs the snapshot as pretty-printed JSON (visible in the live server-logs room), and persists a ServerStatsReport to the database for historical trend analysis. The cadence and retention are controlled by StatsOptions; if IntervalHours is non-positive the service uses a 6-hour default.

## Remarks
To achieve this, the service reads the online user count from PresenceTracker, captures in-memory statistics from ServerStatsCollector, and then opens a scoped EchoHubDbContext to compute metrics such as messages sent, active members, files uploaded, and new users within the reporting window. A fresh DI scope is created per report to ensure proper EF Core lifetimes and isolation between reports. The reporting window is defined by _periodStart and periodEnd to align live counters with database-derived metrics, ensuring the report reflects the same time span across in-memory and persisted data. The pretty-printed JSON log enhances operational visibility by surfacing structured data in the logs.

## Notes
- The background loop honors cancellation by awaiting Task.Delay with the provided CancellationToken and catching OperationCanceledException to exit promptly.
- IntervalHours is validated: non-positive values fall back to 6 hours, and the interval is floored at 1 second to avoid a spinning loop.
- Each report uses its own DbContext scope (via _scopeFactory.CreateScope()) to query the database and persist the resulting ServerStatsReport, ensuring clean lifetimes and minimal cross-report contention.