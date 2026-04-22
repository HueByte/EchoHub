using System.Reflection;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;

namespace EchoHub.Server.Services;

public sealed class ServerDirectoryService : BackgroundService
{
    private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers";
    private static readonly TimeSpan ReconnectBaseDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UserCountMinInterval = TimeSpan.FromSeconds(1);

    private readonly IConfiguration _configuration;
    private readonly PresenceTracker _presenceTracker;
    private readonly ILogger<ServerDirectoryService> _logger;

    // Single-slot, latest-wins channel coalesces bursts of presence changes into one update.
    private readonly Channel<int> _userCountUpdates = Channel.CreateBounded<int>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

    private HubConnection? _connection;
    private int _lastReportedUserCount = -1;

    public ServerDirectoryService(
        IConfiguration configuration,
        PresenceTracker presenceTracker,
        ILogger<ServerDirectoryService> logger)
    {
        _configuration = configuration;
        _presenceTracker = presenceTracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host finish starting before we log or connect
        await Task.Yield();

        var isPublic = _configuration.GetValue<bool>("Server:PublicServer");
        if (!isPublic)
        {
            _logger.LogInformation("PublicServer is disabled — not registering with directory");
            return;
        }

        var hosts = _configuration.GetSection("Server:PublicHosts").Get<string[]>()
            ?.Where(h => !string.IsNullOrWhiteSpace(h))
            .ToArray() ?? Array.Empty<string>();

        if (hosts.Length == 0)
        {
            _logger.LogWarning("PublicServer is enabled but Server:PublicHosts is empty — skipping directory registration");
            return;
        }

        var serverName = _configuration["Server:Name"] ?? "EchoHub Server";
        var description = _configuration["Server:Description"];
        var tags = _configuration.GetSection("Server:Tags").Get<string[]>()
            ?.Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray() ?? Array.Empty<string>();
        var version = ResolveVersion();

        _logger.LogInformation("PublicServer is enabled — connecting to EchoHubSpace directory as {Name} ({Hosts})", serverName, string.Join(", ", hosts));

        _presenceTracker.UserCountChanged += OnUserCountChanged;
        try
        {
            await RunConnectionLoopAsync(serverName, description, hosts, version, tags, stoppingToken);
        }
        finally
        {
            _presenceTracker.UserCountChanged -= OnUserCountChanged;
        }
    }

    private async Task RunConnectionLoopAsync(
        string serverName,
        string? description,
        string[] hosts,
        string version,
        string[] tags,
        CancellationToken stoppingToken)
    {
        // Outer loop: rebuilds the connection if automatic reconnect permanently fails
        while (!stoppingToken.IsCancellationRequested)
        {
            var connection = BuildConnection();
            _connection = connection;

            try
            {
                var connectionPermanentlyClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                connection.On("Ping", async () =>
                {
                    _logger.LogDebug("Received alive check from directory — sending heartbeat");
                    try
                    {
                        await connection.InvokeAsync("Heartbeat");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send heartbeat response");
                    }
                });

                connection.Reconnected += async _ =>
                {
                    _logger.LogInformation("Reconnected to directory — re-registering server");
                    _lastReportedUserCount = -1;
                    await RegisterAsync(serverName, description, hosts, version, tags);
                };

                connection.Closed += ex =>
                {
                    if (ex is not null)
                        _logger.LogWarning(ex, "Directory connection permanently closed — will rebuild");
                    else
                        _logger.LogWarning("Directory connection permanently closed — will rebuild");

                    connectionPermanentlyClosed.TrySetResult();
                    return Task.CompletedTask;
                };

                // Connect with retry
                if (!await ConnectWithRetryAsync(connection, stoppingToken))
                    return;

                _logger.LogInformation("Successfully connected to EchoHubSpace API at {Url}", DirectoryHubUrl);
                await RegisterAsync(serverName, description, hosts, version, tags);

                // Push user-count updates as PresenceTracker raises events, until the connection closes or cancellation
                await ProcessUserCountUpdatesAsync(connection, connectionPermanentlyClosed.Task, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    return;

                // Connection was permanently closed — wait briefly then rebuild
                _logger.LogInformation("Rebuilding directory connection...");
                await Task.Delay(ReconnectBaseDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            finally
            {
                _connection = null;
                await DisposeConnectionAsync(connection);
            }
        }
    }

    private HubConnection BuildConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(DirectoryHubUrl)
            .WithAutomaticReconnect(new InfiniteRetryPolicy())
            .Build();
    }

    private async Task<bool> ConnectWithRetryAsync(HubConnection connection, CancellationToken ct)
    {
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await connection.StartAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                attempt++;
                var delay = GetBackoffDelay(attempt);
                _logger.LogWarning(ex, "Failed to connect to directory — retrying in {Delay}s", delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        return false;
    }

    private void OnUserCountChanged(int newCount)
    {
        // Single-slot channel: latest write wins, so a burst of presence changes coalesces.
        _userCountUpdates.Writer.TryWrite(newCount);
    }

    private async Task ProcessUserCountUpdatesAsync(HubConnection connection, Task connectionClosed, CancellationToken ct)
    {
        var lastSentAt = DateTimeOffset.MinValue;

        while (!ct.IsCancellationRequested)
        {
            var waitTask = _userCountUpdates.Reader.WaitToReadAsync(ct).AsTask();
            var completed = await Task.WhenAny(waitTask, connectionClosed);

            if (completed == connectionClosed)
                return;

            bool hasUpdate;
            try { hasUpdate = await waitTask; }
            catch (OperationCanceledException) { return; }

            if (!hasUpdate)
                return;

            if (!_userCountUpdates.Reader.TryRead(out var count))
                continue;

            // Throttle: enforce a minimum interval between sends. While we wait, drain newer
            // values so the eventual send carries the latest count, not a stale snapshot.
            var elapsed = DateTimeOffset.UtcNow - lastSentAt;
            if (elapsed < UserCountMinInterval)
            {
                try { await Task.Delay(UserCountMinInterval - elapsed, ct); }
                catch (OperationCanceledException) { return; }

                while (_userCountUpdates.Reader.TryRead(out var newer))
                    count = newer;
            }

            if (count == _lastReportedUserCount)
                continue;

            if (connection.State != HubConnectionState.Connected)
                continue;

            try
            {
                await connection.InvokeAsync("UpdateUserCount", count, ct);
                _lastReportedUserCount = count;
                lastSentAt = DateTimeOffset.UtcNow;
                _logger.LogDebug("Updated directory user count to {Count}", count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update user count on directory");
            }
        }
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, 10)));
        return delay > ReconnectMaxDelay ? ReconnectMaxDelay : delay;
    }

    private async Task RegisterAsync(string name, string? description, string[] hosts, string version, string[] tags)
    {
        if (_connection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var userCount = _presenceTracker.GetOnlineUserCount();
            var dto = new RegisterServerDto(name, description, hosts, userCount, version, tags);
            await _connection.InvokeAsync("RegisterServer", dto);
            _lastReportedUserCount = userCount;
            _logger.LogInformation("Registered with directory as {Name} at {Hosts}", name, string.Join(", ", hosts));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register with directory");
        }
    }

    private static string ResolveVersion()
    {
        var assembly = typeof(ServerDirectoryService).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip git SHA suffix that SourceLink appends (e.g. "0.2.10+abc123")
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static async Task DisposeConnectionAsync(HubConnection connection)
    {
        try
        {
            await connection.DisposeAsync()
                .AsTask().WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch
        {
            // Don't let a slow dispose block shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _connection = null;
    }

    /// <summary>
    /// Retries indefinitely with exponential backoff capped at 30 seconds.
    /// </summary>
    private sealed class InfiniteRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(retryContext.PreviousRetryCount, 10)));
            return delay > ReconnectMaxDelay ? ReconnectMaxDelay : delay;
        }
    }
}

internal record RegisterServerDto(
    string Name,
    string? Description,
    string[] Hosts,
    int UserCount,
    string Version,
    string[] Tags);
