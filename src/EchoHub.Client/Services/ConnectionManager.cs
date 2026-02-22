using EchoHub.Client.Config;
using EchoHub.Client.UI.Dialogs;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Result of a successful connection, returned to AppOrchestrator for UI updates.
/// </summary>
internal record ConnectResult(
    LoginResponse Login,
    List<ChannelDto> Channels,
    List<MessageDto> DefaultHistory);

/// <summary>
/// Owns connection lifecycle, authentication, SignalR event wiring, and channel tracking.
/// Fires events so AppOrchestrator can update the UI without managing connection internals.
/// </summary>
internal sealed class ConnectionManager : IAsyncDisposable
{
    private EchoHubConnection? _connection;
    private ApiClient? _apiClient;
    private readonly ClientEncryptionService _encryption = new();
    private readonly HashSet<string> _joinedChannels = [];

    // ── Properties ────────────────────────────────────────────────────────

    public bool IsConnected => _connection?.IsConnected == true;
    public bool IsAuthenticated => _apiClient is not null;
    public ApiClient? Api => _apiClient;

    // ── Events (forwarded from SignalR) ───────────────────────────────────

    public event Action<MessageDto>? MessageReceived;
    public event Action<string, string>? UserJoined;
    public event Action<string, string>? UserLeft;
    public event Action<UserPresenceDto>? UserStatusChanged;
    public event Action<string, string, string?>? UserKicked;
    public event Action<string, string?>? UserBanned;
    public event Action<string>? ForceDisconnected;
    public event Action<string, Guid>? MessageDeleted;
    public event Action<string>? ChannelNuked;
    public event Action<ChannelDto>? ChannelUpdated;
    public event Action<string>? Error;
    public event Action<string>? ConnectionStatusChanged;
    public event Action? Reconnected;

    // ── Connect ───────────────────────────────────────────────────────────

    /// <summary>
    /// Full connection flow: authenticate → encryption → SignalR → join default channel.
    /// Calls <paramref name="onStatus"/> with progress messages for UI updates.
    /// Throws on auth failure (caller handles saved-session expiry, etc.).
    /// </summary>
    public async Task<ConnectResult> ConnectAsync(ConnectDialogResult info, Action<string> onStatus)
    {
        _apiClient?.Dispose();
        _apiClient = new ApiClient(info.ServerUrl);

        onStatus("Authenticating...");

        LoginResponse loginResponse;

        if (info.SavedRefreshToken is not null)
        {
            try
            {
                loginResponse = await _apiClient.LoginWithRefreshTokenAsync(info.SavedRefreshToken);
                Log.Information("Authenticated via saved session for {User}", loginResponse.Username);
            }
            catch
            {
                _apiClient.Dispose();
                _apiClient = null;
                throw; // Caller handles saved-session expiry
            }
        }
        else if (info.IsRegister)
        {
            loginResponse = await _apiClient.RegisterAsync(info.Username, info.Password);
        }
        else
        {
            loginResponse = await _apiClient.LoginAsync(info.Username, info.Password);
        }

        // Auto-persist rotated refresh tokens for Remember Me
        _apiClient.OnTokensRefreshed += HandleTokensRefreshed;

        // E2E encryption key
        onStatus("Fetching encryption key...");
        try
        {
            var encryptionKey = await _apiClient.GetEncryptionKeyAsync();
            _encryption.SetKey(encryptionKey);
            Log.Information("E2E encryption key established");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch encryption key — messages will not be encrypted");
        }

        onStatus("Authenticated, connecting...");

        if (_connection is not null)
            await _connection.DisposeAsync();

        _connection = new EchoHubConnection(info.ServerUrl, _apiClient, _encryption);
        WireConnectionEvents(_connection);
        await _connection.ConnectAsync();

        var channels = await _apiClient.GetChannelsAsync();
        onStatus("Connected");

        // Join default channel + fetch history
        _joinedChannels.Clear();
        _joinedChannels.Add(HubConstants.DefaultChannel);
        await _connection.JoinChannelAsync(HubConstants.DefaultChannel);

        List<MessageDto> history = [];
        try
        {
            history = await _connection.GetHistoryAsync(HubConstants.DefaultChannel);
        }
        catch
        {
            // History might not be available
        }

        return new ConnectResult(loginResponse, channels, history);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    /// <summary>
    /// Disconnect and dispose connection + API client, clear channel tracking.
    /// </summary>
    public async Task CleanupAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisconnectAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _apiClient?.Dispose();
        _apiClient = null;
        _joinedChannels.Clear();
    }

    /// <summary>
    /// Revoke refresh token on the server. Call <see cref="CleanupAsync"/> afterwards.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_apiClient is not null)
            await _apiClient.LogoutAsync();
    }

    // ── Channel Operations ────────────────────────────────────────────────

    public async Task<List<MessageDto>> JoinChannelAsync(string channelName)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected");
        _joinedChannels.Add(channelName);
        return await _connection.JoinChannelAsync(channelName);
    }

    public async Task LeaveChannelAsync(string channelName)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected");
        await _connection.LeaveChannelAsync(channelName);
        _joinedChannels.Remove(channelName);
    }

    /// <summary>
    /// Track a channel as joined (returns true if newly added).
    /// </summary>
    public bool TrackChannel(string channelName) => _joinedChannels.Add(channelName);

    public void UntrackChannel(string channelName) => _joinedChannels.Remove(channelName);

    // ── Delegate Operations ───────────────────────────────────────────────

    public Task SendMessageAsync(string channel, string content) =>
        _connection?.SendMessageAsync(channel, content)
        ?? throw new InvalidOperationException("Not connected");

    public Task<List<MessageDto>> GetHistoryAsync(string channel) =>
        _connection?.GetHistoryAsync(channel)
        ?? throw new InvalidOperationException("Not connected");

    public Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channel) =>
        _connection?.GetOnlineUsersAsync(channel)
        ?? throw new InvalidOperationException("Not connected");

    public Task UpdateStatusAsync(UserStatus status, string? message) =>
        _connection?.UpdateStatusAsync(status, message)
        ?? throw new InvalidOperationException("Not connected");

    // ── Reconnect ─────────────────────────────────────────────────────────

    /// <summary>
    /// Rejoin all previously tracked channels after a reconnect.
    /// </summary>
    public async Task RejoinChannelsAsync()
    {
        var channels = _joinedChannels.ToList();
        if (channels.Count == 0 || _connection is null) return;

        _joinedChannels.Clear();

        foreach (var channel in channels)
        {
            _joinedChannels.Add(channel);
            await _connection.JoinChannelAsync(channel);
        }

        Log.Information("Rejoined {Count} channel(s) after reconnect", channels.Count);
    }

    // ── SignalR Event Wiring ──────────────────────────────────────────────

    private void WireConnectionEvents(EchoHubConnection connection)
    {
        connection.OnMessageReceived += msg => MessageReceived?.Invoke(msg);
        connection.OnUserJoined += (ch, user) => UserJoined?.Invoke(ch, user);
        connection.OnUserLeft += (ch, user) => UserLeft?.Invoke(ch, user);
        connection.OnUserStatusChanged += p => UserStatusChanged?.Invoke(p);
        connection.OnUserKicked += (ch, user, reason) => UserKicked?.Invoke(ch, user, reason);
        connection.OnUserBanned += (user, reason) => UserBanned?.Invoke(user, reason);
        connection.OnForceDisconnect += reason => ForceDisconnected?.Invoke(reason);
        connection.OnMessageDeleted += (ch, id) => MessageDeleted?.Invoke(ch, id);
        connection.OnChannelNuked += ch => ChannelNuked?.Invoke(ch);
        connection.OnChannelUpdated += ch => ChannelUpdated?.Invoke(ch);
        connection.OnError += msg => Error?.Invoke(msg);
        connection.OnConnectionStateChanged += status => ConnectionStatusChanged?.Invoke(status);
        connection.OnReconnected += () => Reconnected?.Invoke();
    }

    // ── Token Persistence ─────────────────────────────────────────────────

    private void HandleTokensRefreshed()
    {
        if (_apiClient?.RefreshToken is null) return;
        var config = ConfigManager.Load();
        var server = config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, _apiClient.BaseUrl, StringComparison.OrdinalIgnoreCase));
        if (server is not null && server.RememberMe)
        {
            server.RefreshToken = _apiClient.RefreshToken;
            ConfigManager.Save(config);
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _apiClient?.Dispose();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
