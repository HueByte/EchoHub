using System.Text.Json;
using EchoHub.Core.Models;
using EchoHub.Server.Config;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Services.Stats;

/// <summary>
/// Background job that periodically snapshots server activity over a window, logs it as
/// pretty-printed JSON (which also surfaces in the live server-logs room), and persists it to
/// the database for historical trends. Interval and retention are configurable via the "Stats"
/// section; the default cadence is every 6 hours.
/// </summary>
public sealed class ServerStatsReportService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceTracker _presence;
    private readonly ServerStatsCollector _collector;
    private readonly StatsOptions _options;
    private readonly ILogger<ServerStatsReportService> _logger;

    // Start of the current reporting window. Advances to PeriodEnd after each report so that
    // the DB-derived counts and the in-memory collector counters cover the same span.
    private DateTimeOffset _periodStart;

    public ServerStatsReportService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presence,
        ServerStatsCollector collector,
        StatsOptions options,
        ILogger<ServerStatsReportService> logger)
    {
        _scopeFactory = scopeFactory;
        _presence = presence;
        _collector = collector;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        // A non-positive interval falls back to the 6h default; anything positive is honoured
        // but floored at 1s so a mis-set tiny value can't spin the loop.
        var interval = _options.IntervalHours > 0
            ? TimeSpan.FromHours(_options.IntervalHours)
            : TimeSpan.FromHours(6);
        if (interval < TimeSpan.FromSeconds(1))
            interval = TimeSpan.FromSeconds(1);
        _periodStart = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Server stats report job started — reporting every {Hours}h, retention {Days}d",
            _options.IntervalHours, _options.RetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await GenerateReportAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to generate periodic server stats report");
            }
        }
    }

    private async Task GenerateReportAsync(CancellationToken ct)
    {
        var periodStart = _periodStart;
        var periodEnd = DateTimeOffset.UtcNow;
        var onlineNow = _presence.GetOnlineUserCount();
        var counters = _collector.SnapshotAndReset(onlineNow);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var messagesSent = await db.Messages.CountAsync(m => m.SentAt >= periodStart, ct);

        var activeMembers = await db.Messages
            .Where(m => m.SentAt >= periodStart)
            .Select(m => m.SenderUserId)
            .Distinct()
            .CountAsync(ct);

        var uploadQuery = db.Attachments
            .Where(a => db.Messages.Any(m => m.Id == a.MessageId && m.SentAt >= periodStart));
        var filesUploaded = await uploadQuery.CountAsync(ct);
        var bytesUploaded = filesUploaded == 0 ? 0L : await uploadQuery.SumAsync(a => a.FileSize, ct);

        var newMembers = await db.Users.CountAsync(u => u.CreatedAt >= periodStart, ct);
        var totalMembers = await db.Users.CountAsync(ct);

        var report = new ServerStatsReport
        {
            Id = Guid.NewGuid(),
            GeneratedAt = periodEnd,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            WindowHours = Math.Round((periodEnd - periodStart).TotalHours, 2),
            MessagesSent = messagesSent,
            FilesUploaded = filesUploaded,
            BytesUploaded = bytesUploaded,
            NewMembers = newMembers,
            ActiveMembers = activeMembers,
            Connections = (int)counters.Connections,
            Disconnections = (int)counters.Disconnections,
            Kicks = (int)counters.Kicks,
            Bans = (int)counters.Bans,
            TotalMembers = totalMembers,
            OnlineNow = onlineNow,
            PeakOnline = counters.PeakOnline,
        };

        db.ServerStatsReports.Add(report);

        // Prune reports beyond the retention window (0 = keep forever).
        if (_options.RetentionDays > 0)
        {
            var cutoff = periodEnd.AddDays(-_options.RetentionDays);
            var stale = await db.ServerStatsReports
                .Where(r => r.GeneratedAt < cutoff)
                .ToListAsync(ct);
            if (stale.Count > 0)
                db.ServerStatsReports.RemoveRange(stale);
        }

        await db.SaveChangesAsync(ct);
        _periodStart = periodEnd;

        // Pretty-printed JSON so the report is readable both in the log files and the logs room.
        var json = JsonSerializer.Serialize(report, JsonOptions);
        _logger.LogInformation("Server stats report ({WindowHours}h window):\n{Report}", report.WindowHours, json);
    }
}
