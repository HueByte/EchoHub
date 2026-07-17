using EchoHub.Core.Models;
using Serilog.Events;

namespace EchoHub.Server.Config;

/// <summary>
/// Live server-log room settings, bound from the "ServerLogs" config section (env override:
/// <c>ServerLogs__Enabled</c> etc.). When enabled, a read-only system channel is auto-created
/// and log events are streamed to it live — log lines are never stored as messages in the
/// database; the rolling Serilog log files remain the only persistence.
/// </summary>
public sealed class ServerLogsOptions
{
    /// <summary>Master switch for the live log room.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Name of the auto-created system channel. Must satisfy the normal channel-name rules;
    /// the name is reserved — users cannot create a channel with it.
    /// </summary>
    public string RoomName { get; set; } = "server-logs";

    /// <summary>Minimum server role allowed to see and join the log room (Member/Mod/Admin/Owner).</summary>
    public ServerRole MinRole { get; set; } = ServerRole.Mod;

    /// <summary>
    /// Minimum level of log events streamed to the room (Verbose/Debug/Information/Warning/
    /// Error/Fatal). Only affects the room — file and console sinks keep their own levels.
    /// </summary>
    public LogEventLevel MinLevel { get; set; } = LogEventLevel.Information;

    /// <summary>How many recent log entries are replayed from the log file when someone opens the room.</summary>
    public int BacklogLines { get; set; } = 100;

    /// <summary>Directory holding the rolling log files. Must match the Serilog file sink's path.</summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>Filename glob for the rolling log files inside <see cref="LogDirectory"/>.</summary>
    public string LogFilePattern { get; set; } = "echohub-server-*.log";

    /// <summary>Channel names are stored lowercased; compare against this form.</summary>
    public string NormalizedRoomName => RoomName.ToLowerInvariant().Trim();
}
