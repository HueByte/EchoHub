namespace EchoHub.Server.Services.Stats;

/// <summary>
/// Thread-safe, in-memory accumulator for server-activity counters that have no natural
/// database timestamp to query after the fact — session connects/disconnects, moderation
/// actions, and peak concurrency. The periodic stats-report job snapshots and resets these
/// once per reporting window. Registered as a singleton; every increment is lock-free so it
/// is safe to call from hot paths (connect/disconnect).
/// </summary>
public sealed class ServerStatsCollector
{
    private long _connections;
    private long _disconnections;
    private long _kicks;
    private long _bans;
    private int _peakOnline;

    /// <summary>Record a session coming online, updating the running peak.</summary>
    public void RecordConnection(int onlineNow)
    {
        Interlocked.Increment(ref _connections);
        RecordOnline(onlineNow);
    }

    /// <summary>Record a session going offline, updating the running peak.</summary>
    public void RecordDisconnection(int onlineNow)
    {
        Interlocked.Increment(ref _disconnections);
        RecordOnline(onlineNow);
    }

    /// <summary>Record a kick action.</summary>
    public void RecordKick() => Interlocked.Increment(ref _kicks);

    /// <summary>Record a ban action.</summary>
    public void RecordBan() => Interlocked.Increment(ref _bans);

    /// <summary>Update the running maximum of concurrent online users (lock-free).</summary>
    public void RecordOnline(int onlineNow)
    {
        int current;
        while (onlineNow > (current = Volatile.Read(ref _peakOnline)))
        {
            if (Interlocked.CompareExchange(ref _peakOnline, onlineNow, current) == current)
                break;
        }
    }

    /// <summary>
    /// Atomically read all counters and reset them for the next reporting window. The peak is
    /// reset to <paramref name="onlineNow"/> so the next window's peak starts from the current
    /// concurrency rather than zero.
    /// </summary>
    public StatsCounters SnapshotAndReset(int onlineNow) => new(
        Connections: Interlocked.Exchange(ref _connections, 0),
        Disconnections: Interlocked.Exchange(ref _disconnections, 0),
        Kicks: Interlocked.Exchange(ref _kicks, 0),
        Bans: Interlocked.Exchange(ref _bans, 0),
        PeakOnline: Interlocked.Exchange(ref _peakOnline, onlineNow));
}

/// <summary>Immutable snapshot of the counters held by <see cref="ServerStatsCollector"/>.</summary>
public readonly record struct StatsCounters(
    long Connections,
    long Disconnections,
    long Kicks,
    long Bans,
    int PeakOnline);
