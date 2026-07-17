namespace EchoHub.Server.Config;

/// <summary>
/// Periodic server-stats report settings, bound from the "Stats" config section (env override:
/// <c>Stats__Enabled</c> etc.). When enabled, a background job periodically snapshots server
/// activity, logs it as pretty-printed JSON, and persists it to the database.
/// </summary>
public sealed class StatsOptions
{
    /// <summary>Master switch for the periodic stats report job.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often a report is generated, in hours. Default: every 6 hours.</summary>
    public double IntervalHours { get; set; } = 6;

    /// <summary>
    /// How long persisted reports are kept before being pruned, in days. Set to 0 to keep
    /// reports indefinitely. Default: 90 days.
    /// </summary>
    public int RetentionDays { get; set; } = 90;
}
