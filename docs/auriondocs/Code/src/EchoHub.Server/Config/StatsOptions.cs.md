# StatsOptions

> **File:** `src/EchoHub.Server/Config/StatsOptions.cs`  
> **Kind:** class

```csharp
public sealed class StatsOptions
```


StatsOptions is a bound configuration object that governs the periodic server-stats reporter. When Enabled is true, a background job periodically snapshots server activity, logs the snapshot as pretty-printed JSON, and persists it to the database; IntervalHours controls cadence, and RetentionDays controls how long reports are kept. The environment override Stats__Enabled allows turning the reporter on or off via environment configuration without changing code.

## Remarks
StatsOptions serves as a simple, sealed data contract that the configuration system binds to at startup, providing a single source of truth for the reporter settings. Centralizing these knobs here avoids scattering config keys throughout the code and makes it easy to swap configuration providers or add validation in one place. The defaults (Enabled = true, IntervalHours = 6, RetentionDays = 90) define the out-of-the-box behavior and can be overridden by environment or configuration.

## Notes
- RetentionDays: 0 means keep reports indefinitely; any positive number prunes older entries.
- IntervalHours is a double; fractional values (e.g., 1.5) are allowed, but scheduling resolution depends on the hosting environment.
- Enabled acts as the master switch for the background job; disabling it stops snapshots until re-enabled.