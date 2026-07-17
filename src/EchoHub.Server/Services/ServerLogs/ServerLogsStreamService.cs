using System.Globalization;
using System.Text;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Server.Config;
using EchoHub.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Serilog.Events;

namespace EchoHub.Server.Services.ServerLogs;

/// <summary>
/// Streams queued log events to the live log room as ephemeral messages: SignalR only (the
/// IRC gateway never sees them) and no database rows. Ensures the room exists before
/// streaming, recreating it on the fly if it was deleted.
///
/// This class must never log from its streaming path — its namespace is excluded by
/// <see cref="ServerLogsSink"/> as a second line of defense, but the primary rule is simply
/// not to log per event, otherwise every streamed line would spawn another.
/// </summary>
public sealed class ServerLogsStreamService : BackgroundService
{
    private static readonly TimeSpan EnsureInterval = TimeSpan.FromSeconds(15);

    private readonly ServerLogsSink _sink;
    private readonly ServerLogsOptions _options;
    private readonly IChannelService _channelService;
    private readonly IMessageEncryptionService _encryption;
    private readonly IServiceProvider _serviceProvider;
    private IHubContext<ChatHub, IEchoHubClient>? _hubContext;
    private DateTimeOffset _lastEnsure = DateTimeOffset.MinValue;

    // Resolved lazily: the hub context isn't available while hosted services are constructed.
    private IHubContext<ChatHub, IEchoHubClient> HubContext
        => _hubContext ??= _serviceProvider.GetRequiredService<IHubContext<ChatHub, IEchoHubClient>>();

    public ServerLogsStreamService(
        ServerLogsSink sink,
        ServerLogsOptions options,
        IChannelService channelService,
        IMessageEncryptionService encryption,
        IServiceProvider serviceProvider)
    {
        _sink = sink;
        _options = options;
        _channelService = channelService;
        _encryption = encryption;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Make the room exist from server start, not only once the first event arrives.
        await TryEnsureRoomAsync();

        await foreach (var logEvent in _sink.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await TryEnsureRoomAsync();

                var roomName = _options.NormalizedRoomName;
                var message = new MessageDto(
                    Guid.NewGuid(),
                    _encryption.Encrypt(Format(logEvent)),
                    ServerLogsService.SenderName,
                    null,
                    roomName,
                    logEvent.Timestamp);

                await HubContext.Clients.Group(roomName).ReceiveMessage(message);
            }
            catch when (!stoppingToken.IsCancellationRequested)
            {
                // Swallow: logging here would re-enter the pipeline.
            }
        }
    }

    /// <summary>
    /// Recreates the log room if it disappeared, at most once per <see cref="EnsureInterval"/>.
    /// </summary>
    private async Task TryEnsureRoomAsync()
    {
        if (DateTimeOffset.UtcNow - _lastEnsure < EnsureInterval)
            return;

        _lastEnsure = DateTimeOffset.UtcNow;
        try
        {
            await _channelService.EnsureSystemChannelAsync(_options.NormalizedRoomName, ServerLogsService.RoomTopic);
        }
        catch
        {
            // Retried on the next interval; events streamed meanwhile just go to no group members.
        }
    }

    /// <summary>Formats an event like the file sink's template, minus the timestamp (clients render their own). Public for tests.</summary>
    public static string Format(LogEvent logEvent)
    {
        var builder = new StringBuilder("[").Append(ShortLevel(logEvent.Level)).Append("] ")
            .Append(logEvent.RenderMessage(CultureInfo.InvariantCulture));

        if (logEvent.Exception is not null)
            builder.Append('\n').Append(logEvent.Exception);

        if (builder.Length > HubConstants.MaxMessageLength)
        {
            builder.Length = HubConstants.MaxMessageLength - 1;
            builder.Append('…');
        }

        return builder.ToString();
    }

    private static string ShortLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => level.ToString().ToUpperInvariant(),
    };
}
