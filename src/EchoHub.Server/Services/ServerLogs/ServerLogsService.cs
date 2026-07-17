using System.Globalization;
using EchoHub.Core.Models;
using EchoHub.Server.Config;

namespace EchoHub.Server.Services.ServerLogs;

/// <summary>
/// A backlog entry read from the log file: one timestamped log line plus any continuation
/// lines (exception stack traces) that followed it.
/// </summary>
public record LogBacklogEntry(DateTimeOffset Timestamp, string Content);

/// <summary>
/// Shared logic for the live log room: room identity, the role gate, and reading the backlog
/// tail from the current rolling log file. The file stays the only persistence — log lines
/// are never stored as messages.
/// </summary>
public sealed class ServerLogsService
{
    /// <summary>Username shown as the sender of streamed log messages.</summary>
    public const string SenderName = "server";

    public const string RoomTopic = "Live server logs — read-only";

    /// <summary>Timestamp prefix of the file sink's output template.</summary>
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>How far back into the log file the backlog read reaches, at most.</summary>
    private const int TailReadBytes = 256 * 1024;

    private readonly ServerLogsOptions _options;

    public ServerLogsService(ServerLogsOptions options) => _options = options;

    public ServerLogsOptions Options => _options;

    /// <summary>Whether the given channel is the (enabled) live log room.</summary>
    public bool IsLogsChannel(string channelName) =>
        _options.Enabled
        && string.Equals(channelName.Trim(), _options.NormalizedRoomName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a user with this role may see and join the log room.</summary>
    public bool CanView(ServerRole role) => _options.Enabled && role >= _options.MinRole;

    /// <summary>
    /// Reads the last <see cref="ServerLogsOptions.BacklogLines"/> entries from the newest
    /// log file. A line starting with a timestamp begins a new entry; continuation lines
    /// (stack traces) stay attached to the entry above them. Best-effort: any I/O problem
    /// yields an empty backlog rather than failing the join.
    /// </summary>
    public IReadOnlyList<LogBacklogEntry> ReadBacklog()
    {
        try
        {
            var directory = Path.GetFullPath(_options.LogDirectory);
            if (!Directory.Exists(directory))
                return [];

            var newest = new DirectoryInfo(directory)
                .GetFiles(_options.LogFilePattern)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            if (newest is null)
                return [];

            // Shared read: Serilog keeps the file open for writing (and rolls it daily).
            using var stream = new FileStream(newest.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var seeked = stream.Length > TailReadBytes;
            if (seeked)
                stream.Seek(-TailReadBytes, SeekOrigin.End);
            using var reader = new StreamReader(stream);
            var lines = reader.ReadToEnd().Split('\n');

            return GroupIntoEntries(lines, skipLeadingContinuations: seeked, _options.BacklogLines);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Groups raw file lines into entries by their timestamp prefix. Public for tests.
    /// </summary>
    public static IReadOnlyList<LogBacklogEntry> GroupIntoEntries(
        IReadOnlyList<string> lines, bool skipLeadingContinuations, int maxEntries)
    {
        var entries = new List<LogBacklogEntry>();
        LogBacklogEntry? current = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
                continue;

            if (TryParseTimestamp(line, out var timestamp, out var rest))
            {
                if (current is not null)
                    entries.Add(current);
                current = new LogBacklogEntry(timestamp, rest);
            }
            else if (current is not null)
            {
                current = current with { Content = current.Content + "\n" + line };
            }
            else if (!skipLeadingContinuations)
            {
                // File starts mid-entry only when we seeked into it; otherwise keep the line.
                current = new LogBacklogEntry(DateTimeOffset.UtcNow, line);
            }
        }

        if (current is not null)
            entries.Add(current);

        if (entries.Count > maxEntries)
            entries.RemoveRange(0, entries.Count - maxEntries);

        return entries;
    }

    private static bool TryParseTimestamp(string line, out DateTimeOffset timestamp, out string rest)
    {
        timestamp = default;
        rest = string.Empty;

        if (line.Length <= TimestampFormat.Length
            || !DateTime.TryParseExact(line[..TimestampFormat.Length], TimestampFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return false;

        timestamp = new DateTimeOffset(parsed);
        rest = line[TimestampFormat.Length..].TrimStart();
        return true;
    }
}
