using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using EchoHub.Core.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EchoHub.Server.Irc;

public sealed class IrcGatewayService : BackgroundService
{
    private readonly IrcOptions _options;
    private readonly IChatService _chatService;
    private readonly ILogger<IrcGatewayService> _logger;
    private readonly ConcurrentDictionary<string, IrcClientConnection> _connections = new();

    public IrcOptions Options => _options;
    public IReadOnlyDictionary<string, IrcClientConnection> Connections => _connections;

    public IrcGatewayService(
        IOptions<IrcOptions> options,
        IChatService chatService,
        ILogger<IrcGatewayService> logger)
    {
        _options = options.Value;
        _chatService = chatService;
        _logger = logger;
    }

    public IEnumerable<IrcClientConnection> GetConnectionsInChannel(string channelName)
    {
        return _connections.Values
            .Where(c => c.IsAuthenticated && c.JoinedChannels.Contains(channelName));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.Error.WriteLine("[DIAG] IrcGatewayService.ExecuteAsync entered.");
        await Task.Yield();
        Console.Error.WriteLine($"[DIAG] IrcGatewayService.ExecuteAsync resumed after Task.Yield(). Enabled={_options.Enabled}");

        if (!_options.Enabled)
        {
            _logger.LogInformation("IRC gateway is disabled");
            return;
        }

        var listeners = new List<Task>();

        listeners.Add(RunListenerAsync(_options.Port, useTls: false, stoppingToken));

        if (_options.TlsEnabled && !string.IsNullOrWhiteSpace(_options.TlsCertPath))
        {
            listeners.Add(RunListenerAsync(_options.TlsPort, useTls: true, stoppingToken));
        }

        await Task.WhenAll(listeners);
    }

    private async Task RunListenerAsync(int port, bool useTls, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _logger.LogInformation("IRC gateway listening on port {Port} ({Mode})",
            port, useTls ? "TLS" : "plain");

        ct.Register(() => listener.Stop());

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(tcpClient, useTls, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, bool useTls, CancellationToken ct)
    {
        Stream stream = tcpClient.GetStream();

        if (useTls)
        {
            try
            {
                var cert = X509CertificateLoader.LoadPkcs12FromFile(_options.TlsCertPath!, _options.TlsCertPassword);
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                await sslStream.AuthenticateAsServerAsync(cert);
                stream = sslStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TLS handshake failed");
                tcpClient.Close();
                return;
            }
        }

        var connection = new IrcClientConnection(tcpClient, stream);
        _connections[connection.ConnectionId] = connection;

        _logger.LogInformation("IRC client connected: {Id}", connection.ConnectionId);

        try
        {
            var handler = new IrcCommandHandler(
                connection, _options, _chatService, _logger);

            await handler.RunAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IRC client {Id} error", connection.ConnectionId);
        }
        finally
        {
            if (connection.IsAuthenticated)
            {
                foreach (var ch in connection.JoinedChannels.ToList())
                {
                    await _chatService.LeaveChannelAsync(
                        connection.ConnectionId, connection.Nickname!, ch);
                }
                await _chatService.UserDisconnectedAsync(connection.ConnectionId);
            }

            _connections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync();
            _logger.LogInformation("IRC client {Id} ({Nick}) disconnected",
                connection.ConnectionId, connection.Nickname ?? "unregistered");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("[DIAG] IrcGatewayService.StopAsync entered.");
        // Cancel ExecuteAsync first so listeners stop accepting
        await base.StopAsync(cancellationToken);
        Console.Error.WriteLine("[DIAG] IrcGatewayService.StopAsync: base.StopAsync returned.");

        // Force-close any remaining client connections
        foreach (var (_, conn) in _connections)
        {
            try
            {
                await conn.SendAsync("ERROR :Server shutting down")
                    .WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch { }
            finally
            {
                try { await conn.DisposeAsync(); }
                catch { }
            }
        }
        _connections.Clear();
    }
}
