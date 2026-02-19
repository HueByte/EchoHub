using Microsoft.AspNetCore.SignalR.Client;

namespace EchoHub.Server.Services;

public sealed class ServerDirectoryService(
    IConfiguration configuration,
    PresenceTracker presenceTracker,
    ILogger<ServerDirectoryService> logger) : BackgroundService
{
    private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReconnectBaseDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(30);

    private HubConnection? _connection;
    private int _lastReportedUserCount = -1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host finish starting before we log or connect
        await Task.Yield();

        var isPublic = configuration.GetValue<bool>("Server:PublicServer");
        if (!isPublic)
        {
            logger.LogInformation("PublicServer is disabled — not registering with directory");
            return;
        }

        var host = configuration["Server:PublicHost"];

        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("PublicServer is enabled but Server:PublicHost is not set — skipping directory registration");
            return;
        }

        var serverName = configuration["Server:Name"] ?? "EchoHub Server";
        var description = configuration["Server:Description"];

        logger.LogInformation("PublicServer is enabled — connecting to EchoHubSpace directory as {Name} ({Host})", serverName, host);

        // Outer loop: rebuilds the connection if automatic reconnect permanently fails
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var connection = BuildConnection();
            _connection = connection;

            var connectionPermanentlyClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.Reconnected += async _ =>
            {
                logger.LogInformation("Reconnected to directory — re-registering server");
                _lastReportedUserCount = -1;
                await RegisterAsync(serverName, description, host);
            };

            connection.Closed += ex =>
            {
                if (ex is not null)
                    logger.LogWarning(ex, "Directory connection permanently closed — will rebuild");
                else
                    logger.LogWarning("Directory connection permanently closed — will rebuild");

                connectionPermanentlyClosed.TrySetResult();
                return Task.CompletedTask;
            };

            // Connect with retry
            if (!await ConnectWithRetryAsync(connection, stoppingToken))
                return;

            logger.LogInformation("Successfully connected to EchoHubSpace API at {Url}", DirectoryHubUrl);
            await RegisterAsync(serverName, description, host);

            // Poll user count until the connection is permanently closed or cancellation
            await PollUserCountAsync(connection, connectionPermanentlyClosed.Task, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                return;

            // Connection was permanently closed — wait briefly then rebuild
            _connection = null;
            logger.LogInformation("Rebuilding directory connection...");
            await Task.Delay(ReconnectBaseDelay, stoppingToken);
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
                logger.LogWarning(ex, "Failed to connect to directory — retrying in {Delay}s", delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        return false;
    }

    private async Task PollUserCountAsync(HubConnection connection, Task connectionClosed, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var delayTask = Task.Delay(UpdateInterval, ct);
            var completed = await Task.WhenAny(delayTask, connectionClosed);

            if (completed == connectionClosed)
                return;

            // Observe the delay task (may throw if cancelled)
            try { await delayTask; }
            catch (OperationCanceledException) { return; }

            if (connection.State != HubConnectionState.Connected)
                continue;

            var currentCount = presenceTracker.GetOnlineUserCount();
            if (currentCount == _lastReportedUserCount)
                continue;

            try
            {
                await connection.InvokeAsync("UpdateUserCount", currentCount, ct);
                _lastReportedUserCount = currentCount;
                logger.LogDebug("Updated directory user count to {Count}", currentCount);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update user count on directory");
            }
        }
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, 10)));
        return delay > ReconnectMaxDelay ? ReconnectMaxDelay : delay;
    }

    private async Task RegisterAsync(string name, string? description, string host)
    {
        if (_connection?.State != HubConnectionState.Connected)
            return;

        try
        {
            var userCount = presenceTracker.GetOnlineUserCount();
            var dto = new RegisterServerDto(name, description, host, userCount);
            await _connection.InvokeAsync("RegisterServer", dto);
            _lastReportedUserCount = userCount;
            logger.LogInformation("Registered with directory as {Name} at {Host}", name, host);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to register with directory");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cancel ExecuteAsync first — it disposes the connection via await using
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

internal record RegisterServerDto(string Name, string? Description, string Host, int UserCount);
