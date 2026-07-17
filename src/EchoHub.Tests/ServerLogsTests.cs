using EchoHub.Core.Constants;
using EchoHub.Core.Models;
using EchoHub.Server.Config;
using EchoHub.Server.Services.ServerLogs;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace EchoHub.Tests;

public class ServerLogsTests
{
    private static readonly MessageTemplateParser Parser = new();

    private static LogEvent MakeEvent(LogEventLevel level, string message,
        string? sourceContext = null, Exception? exception = null)
    {
        var props = new List<LogEventProperty>();
        if (sourceContext is not null)
            props.Add(new LogEventProperty(Constants.SourceContextPropertyName, new ScalarValue(sourceContext)));
        return new LogEvent(DateTimeOffset.UnixEpoch, level, exception, Parser.Parse(message), props);
    }

    // ── Options binding ───────────────────────────────────────────────

    [Fact]
    public void Options_Defaults_EnabledModInformation()
    {
        var options = new ServerLogsOptions();

        Assert.True(options.Enabled);
        Assert.Equal("server-logs", options.RoomName);
        Assert.Equal(ServerRole.Mod, options.MinRole);
        Assert.Equal(LogEventLevel.Information, options.MinLevel);
    }

    [Fact]
    public void Options_BoundFromConfiguration_ParsesEnums()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServerLogs:Enabled"] = "false",
                ["ServerLogs:RoomName"] = "audit",
                ["ServerLogs:MinRole"] = "Admin",
                ["ServerLogs:MinLevel"] = "Warning",
                ["ServerLogs:BacklogLines"] = "42",
            })
            .Build();

        var options = config.GetSection("ServerLogs").Get<ServerLogsOptions>()!;

        Assert.False(options.Enabled);
        Assert.Equal("audit", options.RoomName);
        Assert.Equal(ServerRole.Admin, options.MinRole);
        Assert.Equal(LogEventLevel.Warning, options.MinLevel);
        Assert.Equal(42, options.BacklogLines);
    }

    // ── ServerLogsService: identity + role gate ───────────────────────

    [Fact]
    public void IsLogsChannel_MatchesConfiguredNameCaseInsensitively()
    {
        var service = new ServerLogsService(new ServerLogsOptions { RoomName = "Server-Logs" });

        Assert.True(service.IsLogsChannel("server-logs"));
        Assert.True(service.IsLogsChannel("SERVER-LOGS"));
        Assert.False(service.IsLogsChannel("general"));
    }

    [Fact]
    public void IsLogsChannel_WhenDisabled_AlwaysFalse()
    {
        var service = new ServerLogsService(new ServerLogsOptions { Enabled = false });

        Assert.False(service.IsLogsChannel("server-logs"));
    }

    [Theory]
    [InlineData(ServerRole.Member, false)]
    [InlineData(ServerRole.Mod, true)]
    [InlineData(ServerRole.Admin, true)]
    [InlineData(ServerRole.Owner, true)]
    public void CanView_RespectsMinRole(ServerRole role, bool expected)
    {
        var service = new ServerLogsService(new ServerLogsOptions { MinRole = ServerRole.Mod });

        Assert.Equal(expected, service.CanView(role));
    }

    [Fact]
    public void CanView_WhenDisabled_AlwaysFalse()
    {
        var service = new ServerLogsService(new ServerLogsOptions { Enabled = false, MinRole = ServerRole.Member });

        Assert.False(service.CanView(ServerRole.Owner));
    }

    // ── Backlog grouping ──────────────────────────────────────────────

    [Fact]
    public void GroupIntoEntries_AttachesContinuationLinesToPrecedingEntry()
    {
        string[] lines =
        [
            "2026-07-17 10:00:00.000 [INF] first line",
            "2026-07-17 10:00:01.000 [ERR] boom",
            "System.Exception: boom",
            "   at Foo.Bar()",
        ];

        var entries = ServerLogsService.GroupIntoEntries(lines, skipLeadingContinuations: false, maxEntries: 100);

        Assert.Equal(2, entries.Count);
        Assert.Equal("[INF] first line", entries[0].Content);
        Assert.Equal("[ERR] boom\nSystem.Exception: boom\n   at Foo.Bar()", entries[1].Content);
    }

    [Fact]
    public void GroupIntoEntries_SkipLeadingContinuations_DropsPartialFirstEntry()
    {
        // Simulates seeking into the middle of a file: the leading stack-trace fragment has no
        // owning timestamped line and must be discarded rather than shown as its own entry.
        string[] lines =
        [
            "   at Orphaned.Frame()",
            "2026-07-17 10:00:00.000 [INF] real entry",
        ];

        var entries = ServerLogsService.GroupIntoEntries(lines, skipLeadingContinuations: true, maxEntries: 100);

        Assert.Single(entries);
        Assert.Equal("[INF] real entry", entries[0].Content);
    }

    [Fact]
    public void GroupIntoEntries_CapsToMaxEntries_KeepingNewest()
    {
        var lines = Enumerable.Range(0, 10)
            .Select(i => $"2026-07-17 10:00:0{i}.000 [INF] entry {i}")
            .ToArray();

        var entries = ServerLogsService.GroupIntoEntries(lines, skipLeadingContinuations: false, maxEntries: 3);

        Assert.Equal(3, entries.Count);
        Assert.Equal("[INF] entry 7", entries[0].Content);
        Assert.Equal("[INF] entry 9", entries[2].Content);
    }

    [Fact]
    public void ReadBacklog_ReadsNewestFile_MostRecentEntries()
    {
        var dir = Path.Combine(Path.GetTempPath(), "echohub-logtest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "echohub-server-20260717.log"),
                "2026-07-17 10:00:00.000 [INF] alpha\n2026-07-17 10:00:01.000 [WRN] beta\n");

            var service = new ServerLogsService(new ServerLogsOptions
            {
                LogDirectory = dir,
                LogFilePattern = "echohub-server-*.log",
                BacklogLines = 100,
            });

            var entries = service.ReadBacklog();

            Assert.Equal(2, entries.Count);
            Assert.Equal("[INF] alpha", entries[0].Content);
            Assert.Equal("[WRN] beta", entries[1].Content);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReadBacklog_MissingDirectory_ReturnsEmpty()
    {
        var service = new ServerLogsService(new ServerLogsOptions
        {
            LogDirectory = Path.Combine(Path.GetTempPath(), "echohub-does-not-exist-" + Guid.NewGuid().ToString("N")),
        });

        Assert.Empty(service.ReadBacklog());
    }

    // ── Formatting ────────────────────────────────────────────────────

    [Fact]
    public void Format_RendersLevelAndMessage()
    {
        var formatted = ServerLogsStreamService.Format(MakeEvent(LogEventLevel.Warning, "disk almost full"));

        Assert.Equal("[WRN] disk almost full", formatted);
    }

    [Fact]
    public void Format_AppendsException()
    {
        var formatted = ServerLogsStreamService.Format(
            MakeEvent(LogEventLevel.Error, "failed", exception: new InvalidOperationException("nope")));

        Assert.StartsWith("[ERR] failed\n", formatted);
        Assert.Contains("nope", formatted);
    }

    [Fact]
    public void Format_TruncatesToMaxMessageLength()
    {
        var formatted = ServerLogsStreamService.Format(
            MakeEvent(LogEventLevel.Information, new string('x', HubConstants.MaxMessageLength * 2)));

        Assert.True(formatted.Length <= HubConstants.MaxMessageLength);
        Assert.EndsWith("…", formatted);
    }

    // ── Sink: level + source filtering, no feedback loop ──────────────

    [Fact]
    public void Sink_DropsEventsBelowMinLevel()
    {
        var sink = new ServerLogsSink(new ServerLogsOptions { MinLevel = LogEventLevel.Warning });

        sink.Emit(MakeEvent(LogEventLevel.Information, "quiet"));

        Assert.False(sink.Reader.TryRead(out _));
    }

    [Fact]
    public void Sink_EnqueuesEventsAtOrAboveMinLevel()
    {
        var sink = new ServerLogsSink(new ServerLogsOptions { MinLevel = LogEventLevel.Information });

        sink.Emit(MakeEvent(LogEventLevel.Warning, "heads up"));

        Assert.True(sink.Reader.TryRead(out var e));
        Assert.Equal(LogEventLevel.Warning, e!.Level);
    }

    [Theory]
    [InlineData("EchoHub.Server.Services.ServerLogs.ServerLogsStreamService")]
    [InlineData("Microsoft.AspNetCore.SignalR.HubConnectionContext")]
    [InlineData("Microsoft.AspNetCore.Http.Connections.Internal.HttpConnectionManager")]
    public void Sink_DropsEventsFromPipelineSources_PreventingFeedbackLoop(string source)
    {
        var sink = new ServerLogsSink(new ServerLogsOptions { MinLevel = LogEventLevel.Information });

        sink.Emit(MakeEvent(LogEventLevel.Error, "would loop", sourceContext: source));

        Assert.False(sink.Reader.TryRead(out _));
    }

    [Fact]
    public void Sink_KeepsEventsFromOtherSources()
    {
        var sink = new ServerLogsSink(new ServerLogsOptions { MinLevel = LogEventLevel.Information });

        sink.Emit(MakeEvent(LogEventLevel.Information, "kept", sourceContext: "EchoHub.Server.Services.ChatService"));

        Assert.True(sink.Reader.TryRead(out _));
    }
}
