using System.Reflection;
using System.Text.Json;
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
    private readonly DirectoryClaimStore _claimStore;
    private readonly ILogger<ServerDirectoryService> _logger;

    // Single-slot, latest-wins channel coalesces bursts of presence changes into one update.
    private readonly Channel<int> _userCountUpdates = Channel.CreateBounded<int>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

    private HubConnection? _connection;
    private int _lastReportedUserCount = -1;

    // Set true when a registration error code arrives (HostAlreadyClaimed/InvalidToken/HostConflict).
    // Once set, we stop attempting register on this connection AND on any reconnects, since the
    // hub won't kick us off and we'd otherwise tight-loop. Operator must restart after fixing config.
    private bool _registrationPermanentlyFailed;

    public ServerDirectoryService(
        IConfiguration configuration,
        PresenceTracker presenceTracker,
        DirectoryClaimStore claimStore,
        ILogger<ServerDirectoryService> logger)
    {
        _configuration = configuration;
        _presenceTracker = presenceTracker;
        _claimStore = claimStore;
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
                    if (_registrationPermanentlyFailed)
                    {
                        _logger.LogWarning("Reconnected to directory but previous registration permanently failed — not re-registering. Restart the server after fixing configuration.");
                        return;
                    }

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

            // No point pushing presence to a row we don't own (or never claimed)
            if (_registrationPermanentlyFailed || !_claimStore.Status.IsRegistered)
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

        if (_registrationPermanentlyFailed)
            return;

        try
        {
            var userCount = _presenceTracker.GetOnlineUserCount();
            // ClaimToken is null on first-ever registration; otherwise the token persisted on first claim.
            var dto = new RegisterServerDto(name, description, hosts, userCount, version, tags, _claimStore.ClaimToken);

            var envelope = await _connection.InvokeAsync<Response<RegisterServerResult>>("RegisterServer", dto);
            await HandleRegistrationResponseAsync(envelope, userCount, name, hosts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register with directory");
        }
    }

    private async Task HandleRegistrationResponseAsync(Response<RegisterServerResult>? envelope, int userCount, string name, string[] hosts)
    {
        if (envelope is null)
        {
            _registrationPermanentlyFailed = true;
            _logger.LogError("Directory returned a null envelope for RegisterServer — treating as malformed. Server will not retry until restarted.");
            _claimStore.SetFailure(DirectoryRegistrationErrors.MalformedResponse, null);
            return;
        }

        // Pin protocol version. Spec: fail hard on mismatch — bumps are coordinated.
        if (!string.Equals(envelope.Version, DirectoryProtocol.Version, StringComparison.Ordinal))
        {
            _registrationPermanentlyFailed = true;
            _logger.LogError(
                "Directory protocol version mismatch: client expects {Expected}, hub returned {Actual}. " +
                "Refusing to operate. Coordinate a deploy that aligns both sides.",
                DirectoryProtocol.Version, envelope.Version ?? "(null)");
            _claimStore.SetFailure(DirectoryRegistrationErrors.ProtocolVersionMismatch, null);
            return;
        }

        if (!envelope.IsSuccess)
        {
            await HandleRegistrationErrorAsync(envelope.Errors);
            return;
        }

        if (envelope.Data is null)
        {
            _registrationPermanentlyFailed = true;
            _logger.LogError("Directory returned IsSuccess=true but Data was null — treating as malformed. Server will not retry until restarted.");
            _claimStore.SetFailure(DirectoryRegistrationErrors.MalformedResponse, null);
            return;
        }

        var data = envelope.Data;
        var serverId = data.ServerId;

        // Persist a freshly-issued claim token *before* anything else acks success — durability guarantee for first claim.
        if (!string.IsNullOrEmpty(data.ClaimToken))
        {
            await _claimStore.SaveClaimAsync(data.ClaimToken, serverId);
        }
        else
        {
            // No fresh token (re-register): just keep the persisted ServerId in sync defensively.
            await _claimStore.UpdateServerIdAsync(serverId);
        }

        _claimStore.SetSuccess(serverId);
        _lastReportedUserCount = userCount;
        _logger.LogInformation("Registered with directory as {Name} at {Hosts} (ServerId {ServerId})", name, string.Join(", ", hosts), serverId);
    }

    private Task HandleRegistrationErrorAsync(ErrorDetail[]? errors)
    {
        _registrationPermanentlyFailed = true;

        var firstError = errors is { Length: > 0 } ? errors[0] : null;
        var code = firstError?.Code ?? "UnknownError";
        var conflictingHosts = ExtractConflictingHosts(firstError);
        var conflicts = conflictingHosts is { Length: > 0 }
            ? string.Join(", ", conflictingHosts)
            : "(none reported)";

        switch (code)
        {
            case DirectoryRegistrationErrors.HostAlreadyClaimed:
                _logger.LogError(
                    "Directory rejected registration: host(s) already claimed by another server: {ConflictingHosts}. " +
                    "Change Server:PublicHosts or contact the directory admin to release the claim. Server will not retry until restarted.",
                    conflicts);
                break;

            case DirectoryRegistrationErrors.InvalidToken:
                _logger.LogError(
                    "Directory rejected registration: persisted claim token is invalid (likely deleted by admin or stale). " +
                    "Delete the claim file ({ClaimFile}) to claim fresh, or contact the directory admin. Server will not retry until restarted.",
                    _claimStore.FilePath);
                break;

            case DirectoryRegistrationErrors.HostConflict:
                _logger.LogError(
                    "Directory rejected registration: token is valid but newly-advertised host(s) conflict with another server's row: {ConflictingHosts}. " +
                    "Remove the conflicting entries from Server:PublicHosts. Server will not retry until restarted.",
                    conflicts);
                break;

            case DirectoryRegistrationErrors.InvalidInput:
                _logger.LogError(
                    "Directory rejected registration as InvalidInput ({Message}). Likely a client/hub contract drift — check Server config. Server will not retry until restarted.",
                    firstError?.Message ?? "(no message)");
                break;

            default:
                _logger.LogError(
                    "Directory rejected registration with unknown error code: {Error} ({Message}). Server will not retry until restarted.",
                    code, firstError?.Message ?? "(no message)");
                break;
        }

        _claimStore.SetFailure(code, conflictingHosts);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pulls <c>ConflictingHosts</c> out of an error's loosely-typed <c>Data</c> payload.
    /// Tolerates both PascalCase and camelCase keys since SignalR's wire casing depends on
    /// the hub's serializer config and the field is typed <c>object?</c>.
    /// </summary>
    private static string[]? ExtractConflictingHosts(ErrorDetail? error)
    {
        if (error?.Data is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            return null;

        if (!element.TryGetProperty("ConflictingHosts", out var hostsProp)
            && !element.TryGetProperty("conflictingHosts", out hostsProp))
            return null;

        if (hostsProp.ValueKind != JsonValueKind.Array)
            return null;

        List<string> hosts = [];
        foreach (var item in hostsProp.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
                hosts.Add(s);
        }

        return hosts.Count == 0 ? null : hosts.ToArray();
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
    string[] Tags,
    string? ClaimToken);

internal record RegisterServerResult(Guid ServerId, string? ClaimToken);

/// <summary>
/// Envelope wrapping every directory hub response. Mirrors the EchoHubSpace contract.
/// </summary>
internal record Response<T>(bool IsSuccess, T? Data, ErrorDetail[]? Errors, string? Version);

/// <summary>
/// Error entry inside a <see cref="Response{T}"/>. <c>Data</c> is loosely-typed because the
/// payload shape varies by error code (e.g. <c>{ ConflictingHosts: string[] }</c> for host errors).
/// </summary>
internal record ErrorDetail(string Code, string? Message, JsonElement? Data);

internal static class DirectoryProtocol
{
    /// <summary>
    /// Pinned envelope protocol version. Bumps are coordinated across both repos.
    /// </summary>
    public const string Version = "1.0";
}

internal static class DirectoryRegistrationErrors
{
    public const string InvalidInput = "InvalidInput";
    public const string InvalidToken = "InvalidToken";
    public const string HostAlreadyClaimed = "HostAlreadyClaimed";
    public const string HostConflict = "HostConflict";

    // Client-side synthetic codes (never returned by hub, generated locally for status reporting)
    public const string ProtocolVersionMismatch = "ProtocolVersionMismatch";
    public const string MalformedResponse = "MalformedResponse";
}
