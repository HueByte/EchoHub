using Microsoft.AspNetCore.SignalR.Client;

namespace EchoHub.Server.Services;

public sealed class ServerDirectoryService(
    IConfiguration configuration,
    PresenceTracker presenceTracker,
    ILogger<ServerDirectoryService> logger) : BackgroundService
{
    private const string DirectoryHubUrl = "https://echohub.voidcube.cloud/hubs/servers";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(30);

    private HubConnection? _connection;
    private int _lastReportedUserCount = -1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        _connection = new HubConnectionBuilder()
            .WithUrl(DirectoryHubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Reconnected += async _ =>
        {
            logger.LogInformation("Reconnected to directory — re-registering server");
            await RegisterAsync(serverName, description, host);
        };

        _connection.Closed += ex =>
        {
            if (ex is not null)
                logger.LogWarning(ex, "Directory connection closed with error");
            return Task.CompletedTask;
        };

        // Initial connection with retry
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _connection.StartAsync(stoppingToken);
                logger.LogInformation("Successfully connected to EchoHubSpace API at {Url}", DirectoryHubUrl);
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to directory — retrying in 30s");
                await Task.Delay(UpdateInterval, stoppingToken);
            }
        }

        if (stoppingToken.IsCancellationRequested)
            return;

        // Register on first connect
        await RegisterAsync(serverName, description, host);

        // Poll user count and send updates
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(UpdateInterval, stoppingToken);

            if (_connection.State != HubConnectionState.Connected)
                continue;

            var currentCount = presenceTracker.GetOnlineUserCount();
            if (currentCount == _lastReportedUserCount)
                continue;

            try
            {
                await _connection.InvokeAsync("UpdateUserCount", currentCount, stoppingToken);
                _lastReportedUserCount = currentCount;
                logger.LogDebug("Updated directory user count to {Count}", currentCount);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update user count on directory");
            }
        }
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
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        await base.StopAsync(cancellationToken);
    }
}

internal record RegisterServerDto(string Name, string? Description, string Host, int UserCount);
