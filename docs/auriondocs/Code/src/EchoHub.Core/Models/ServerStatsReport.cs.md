# ServerStatsReport

> **File:** `src/EchoHub.Core/Models/ServerStatsReport.cs`  
> **Kind:** class

```csharp
public class ServerStatsReport
```


ServerStatsReport is a snapshot of server activity for a single reporting window, produced periodically by the `stats-report` background job. It records when the report was generated, the start and end of the window, the window length in hours, and a set of per-window activity counters (`MessagesSent`, `FilesUploaded`, `BytesUploaded`, `NewMembers`, `ActiveMembers`, `Connections`, `Disconnections`, `Kicks`, `Bans`) as well as end-of-window totals (`TotalMembers`, `OnlineNow`, `PeakOnline`); the report is serialized as pretty-printed JSON and persisted for historical trend analysis. The window is defined as "since the previous report" (or since startup for the first report).

## Remarks
ServerStatsReport serves as the canonical persisted unit for time-bounded server activity, decoupling the reporting job from storage and analytics. It combines both within-window activity and end-of-window aggregates to support dashboards, trend charts, and anomaly detection across multiple windows. As a plain data container, it is populated by the reporting process and then written to the data store; its structure is stable to ensure reliable longitudinal comparisons.

## Notes
- GeneratedAt is intended to reflect the moment the window ended; ensure GeneratedAt is kept in sync with PeriodEnd to avoid confusion (GeneratedAt should effectively equal PeriodEnd when the report is produced).
- PeriodEnd should be greater than or equal to PeriodStart; WindowHours should be non-negative.
- BytesUploaded uses a 64-bit signed integer; extremely large attachment activity should still stay within `BytesUploaded`'s range to avoid overflow.
