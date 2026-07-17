namespace EchoHub.Core.Models;

/// <summary>
/// A snapshot of server activity over one reporting window, produced periodically by the
/// stats-report background job. Each report is logged as pretty-printed JSON and persisted
/// for historical trend analysis. The window is "since the previous report" (or since startup
/// for the first report of a run).
/// </summary>
public class ServerStatsReport
{
    public Guid Id { get; set; }

    /// <summary>When this report was generated (equals <see cref="PeriodEnd"/>).</summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Start of the reporting window.</summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <summary>End of the reporting window.</summary>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>Length of the reporting window in hours.</summary>
    public double WindowHours { get; set; }

    // ── Activity during the window ──────────────────────────────────────────
    /// <summary>Messages sent during the window.</summary>
    public int MessagesSent { get; set; }

    /// <summary>Attachments (files/images/audio) uploaded during the window.</summary>
    public int FilesUploaded { get; set; }

    /// <summary>Total bytes across all attachments uploaded during the window.</summary>
    public long BytesUploaded { get; set; }

    /// <summary>Accounts registered during the window ("new members joined").</summary>
    public int NewMembers { get; set; }

    /// <summary>Distinct users who sent at least one message during the window.</summary>
    public int ActiveMembers { get; set; }

    /// <summary>Session connects during the window (per-connection, across SignalR + IRC).</summary>
    public int Connections { get; set; }

    /// <summary>Session disconnects during the window ("members left" sessions).</summary>
    public int Disconnections { get; set; }

    /// <summary>Users kicked during the window.</summary>
    public int Kicks { get; set; }

    /// <summary>Users banned during the window.</summary>
    public int Bans { get; set; }

    // ── Point-in-time totals at window end ──────────────────────────────────
    /// <summary>Total registered (unique) members at window end.</summary>
    public int TotalMembers { get; set; }

    /// <summary>Distinct users online at window end.</summary>
    public int OnlineNow { get; set; }

    /// <summary>Peak distinct users online observed during the window.</summary>
    public int PeakOnline { get; set; }
}
