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


The ServerStatsCollector is a thread-safe, in-memory accumulator for server-activity counters that have no natural database timestamp to query after the fact—such as session connects/disconnections, moderation actions, and peak concurrent users. It exposes methods to record connections, disconnections, kicks, and bans, and maintains a running, lock-free estimate of the current peak online. A periodic stats-reporting job calls SnapshotAndReset to atomically capture and reset the window’s counters, seeding the next window’s peak with the provided online count. It is registered as a singleton and is designed to be updated from hot paths like connect/disconnect.

## Remarks
Architecturally, it provides a low-latency in-memory sink that decouples event counting from persistence, enabling a single, atomic snapshot per reporting window for the server’s activity data. The snapshot resets all counters and optically seeds the next window’s peak with the current online count, maintaining continuity of peak tracking across windows.

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


StatsCounters is an immutable snapshot of the counters held by `ServerStatsCollector`. It records the current values of the counters `Connections`, `Disconnections`, `Kicks`, `Bans`, and the peak online figure `PeakOnline` at the moment of creation. Use this type when you need a read-only view of these statistics or to pass them between components without exposing mutable state.

## Remarks
By design, `StatsCounters` decouples consumers from the mutable internal state of `ServerStatsCollector`, offering a stable, shareable view of statistics. As a `readonly record struct`, it provides value-based equality and cheap copies, ensuring a snapshot can be produced and transported without synchronization concerns.

## Notes
- This type is immutable; you cannot modify its fields after construction. If you need an updated view, obtain a new `StatsCounters` from the collector.
- Copying a `StatsCounters` instance is cheap because it is a value type, making it safe to pass across threads or components without locking.
- A snapshot reflects the state at the moment it was created; subsequent updates to the collector will not affect already-captured instances.

---