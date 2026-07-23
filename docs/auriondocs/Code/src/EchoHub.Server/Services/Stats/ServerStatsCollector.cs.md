# ServerStatsCollector.cs

> **Source:** `src/EchoHub.Server/Services/Stats/ServerStatsCollector.cs`

## Contents

- [ServerStatsCollector](#serverstatscollector)
- [StatsCounters](#statscounters)

---

## ServerStatsCollector
> **File:** `src/EchoHub.Server/Services/Stats/ServerStatsCollector.cs`  
> **Kind:** class

```csharp
public sealed class ServerStatsCollector
```


ServerStatsCollector is a thread-safe, in-memory accumulator for server activity counters that have no natural database timestamp (connections, disconnections, kicks, bans, and peak concurrency). It updates counters via lock-free increments and uses a periodic SnapshotAndReset to emit a windowed StatsCounters and prepare the next window, including resetting the peak to the current online count.

## Remarks

Because it is registered as a singleton, multiple threads can record events without blocking. The class uses Interlocked and Volatile to implement a lock-free maximum-tracking algorithm for PeakOnline; SnapshotAndReset atomically drains all counters and resets PeakOnline to the provided onlineNow, which defines the starting point for the next window. This design favors low-latency updates in hot paths while deferring aggregation to the reporting window.

## Notes

- The next window's PeakOnline baseline is reset to the supplied onlineNow; if that baseline is lower than the actual concurrency at snapshot time, the subsequent peak may undercount.
- SnapshotAndReset resets the per-window counters to zero (except PeakOnline, which is reset to onlineNow); ensure you call it on the cadence that matches your reporting window to align with dashboards.

---

## StatsCounters
> **File:** `src/EchoHub.Server/Services/Stats/ServerStatsCollector.cs`  
> **Kind:** record

```csharp
public readonly record struct StatsCounters(
    long Connections,
    long Disconnections,
    long Kicks,
    long Bans,
    int PeakOnline)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Connections` | `long` | — |
| `Disconnections` | `long` | — |
| `Kicks` | `long` | — |
| `Bans` | `long` | — |
| `PeakOnline` | `int` | — |


StatsCounters is an immutable snapshot of the counters held by ServerStatsCollector. It captures the total connections, disconnections, kicks, bans, and the peak online count at a single moment, enabling safe sharing and logging without mutating the underlying counters.

## Remarks
StatCounters uses a readonly record struct to provide value semantics, meaning two instances with the same values compare equal and it can be passed by value without side effects. It is intended to be produced by the ServerStatsCollector and consumed by telemetry, dashboards, or loggers that need a stable view of current activity. Because it is immutable, readers can snapshot and transport it across threads without additional synchronization concerns.

## Example
```csharp
// Create a snapshot of current counters
var snapshot = new StatsCounters(Connections: 1024, Disconnections: 64, Kicks: 3, Bans: 0, PeakOnline: 128);

// Deconstruct to access individual values
var (connections, disconnections, kicks, bans, peakOnline) = snapshot;
```

## Dependencies
- ServerStatsCollector

---