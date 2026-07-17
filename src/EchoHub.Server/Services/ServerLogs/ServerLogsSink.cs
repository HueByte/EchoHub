using System.Threading.Channels;
using EchoHub.Server.Config;
using Serilog.Core;
using Serilog.Events;

namespace EchoHub.Server.Services.ServerLogs;

/// <summary>
/// Serilog sink feeding the live log room. Events are queued in a bounded drop-oldest buffer
/// and consumed by <see cref="ServerLogsStreamService"/>; nothing is written to the database.
/// Events emitted by the streaming pipeline itself (the stream service and SignalR transport
/// internals) are dropped here so a broadcast that logs — e.g. a transport warning on a dead
/// connection — can never enter a log → broadcast → log feedback loop.
/// </summary>
public sealed class ServerLogsSink : ILogEventSink
{
    private const int QueueCapacity = 512;

    private static readonly string[] ExcludedSourcePrefixes =
    [
        "EchoHub.Server.Services.ServerLogs",
        "Microsoft.AspNetCore.SignalR",
        "Microsoft.AspNetCore.Http.Connections",
    ];

    private readonly Channel<LogEvent> _queue = Channel.CreateBounded<LogEvent>(
        new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    private readonly LogEventLevel _minLevel;

    public ServerLogsSink(ServerLogsOptions options) => _minLevel = options.MinLevel;

    public ChannelReader<LogEvent> Reader => _queue.Reader;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _minLevel)
            return;

        if (logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out var sourceProperty)
            && sourceProperty is ScalarValue { Value: string source }
            && ExcludedSourcePrefixes.Any(source.StartsWith))
            return;

        _queue.Writer.TryWrite(logEvent);
    }
}
