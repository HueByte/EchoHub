# StatsOptions

> **File:** `src/EchoHub.Server/Config/StatsOptions.cs`  
> **Kind:** class

```csharp
public sealed class StatsOptions
```


StatsOptions is a configuration-bound class that governs the periodic server-stats reporting behavior of the application. It binds from the Stats config section (with environment overrides like Stats__Enabled) and, when Enabled is true, drives a background job that periodically snapshots server activity, logs the snapshot as pretty-printed JSON, and persists it to the database. Developers would adjust IntervalHours to change how often reports are generated and RetentionDays to control how long reports are kept, or toggle Enabled to enable/disable the reporting; defaults are Enabled = true, IntervalHours = 6, and RetentionDays = 90.

## Remarks
This class serves as the configuration object consumed by the background stats collection service, isolating configuration from implementation and enabling the Stats job to be controlled entirely via config.