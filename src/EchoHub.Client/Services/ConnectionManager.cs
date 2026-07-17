using EchoHub.Client.Config;
using EchoHub.Client.UI.Dialogs;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Result of a successful connection, returned to AppOrchestrator for UI updates.
/// <paramref name="Histories"/> holds the initial history of every auto-joined channel
/// (keyed by channel name, always including the default channel).
/// </summary>
internal record ConnectResult(
    LoginResponse Login,
    List<ChannelDto> Channels,
    Dictionary<string, List<MessageDto>> Histories);

/// <summary>
/// Owns connection lifecycle, authentication, SignalR event wiring, and channel tracking.
/// Fires events so AppOrchestrator can update the UI without managing connection internals.
/// </summary>
internal sealed class ConnectionManager : IAsyncDisposable
{
    private EchoHubConnection? _connection;
    private ApiClient? _apiClient;
    private readonly ClientEncryptionService _encryption = new();
    private readonly RoomKeyStore _roomKeys = new();
    private readonly HashSet<string> _joinedChannels = [];

    // ── Properties ────────────────────────────────────────────────────────

    public bool IsConnected => _connection?.IsConnected == true;
    public bool IsAuthenticated => _apiClient is not null;
    public ApiClient? Api => _apiClient;
    public RoomKeyStore RoomKeys => _roomKeys;

    // ── Events (forwarded from SignalR) ───────────────────────────────────

    public event Action<MessageDto>? MessageReceived;
    public event Action<string, string, UserPresenceDto?>? UserJoined;
    public event Action<string, string>? UserLeft;
    public event Action<UserPresenceDto>? UserStatusChanged;
    public event Action<string, string, string?>? UserKicked;
    public event Action<string, string?>? UserBanned;
    public event Action<string>? ForceDisconnected;
    public event Action<string, Guid>? MessageDeleted;
    public event Action<string>? ChannelDeleted;
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

        try
        {
            onStatus("Authenticating...");

            LoginResponse loginResponse;

            if (info.SavedRefreshToken is not null)
            {
                loginResponse = await _apiClient.LoginWithRefreshTokenAsync(info.SavedRefreshToken);
                Log.Information("Authenticated via saved session for {User}", loginResponse.Username);
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

            _roomKeys.LoadForServer(info.ServerUrl);
            _connection = new EchoHubConnection(info.ServerUrl, _apiClient, _encryption, _roomKeys);
            WireConnectionEvents(_connection);
            await _connection.ConnectAsync();

            var channels = await _apiClient.GetChannelsAsync();

            // Known E2E channels — senders consult this so a client without the room
            // key never emits plaintext into an encrypted room
            foreach (var channel in channels)
                _roomKeys.MarkChannelEncrypted(channel.Name, channel.IsEncrypted);

            // Join default channel + fetch history
            onStatus("Joining channels...");
            _joinedChannels.Clear();
            _joinedChannels.Add(HubConstants.DefaultChannel);
            await _connection.JoinChannelAsync(HubConstants.DefaultChannel);

            var histories = new Dictionary<string, List<MessageDto>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                histories[HubConstants.DefaultChannel] = await _connection.GetHistoryAsync(HubConstants.DefaultChannel);
            }
            catch
            {
                // History might not be available
            }

            // Auto-join every other channel the server lists for this user (public +
            // prior memberships) so message events — unread counts, @mentions — flow for
            // all of them, not just channels opened this session. Channels the user left
            // with /leave stay out until rejoined; protected channels we can't enter
            // silently (no cached membership) are skipped, never prompted for.
            var leftChannels = FindServer(ConfigManager.Load(), info.ServerUrl)?.LeftChannels ?? [];
            foreach (var channel in channels)
            {
                if (channel.Name.Equals(HubConstants.DefaultChannel, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (leftChannels.Contains(channel.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var outcome = await _connection.JoinChannelAsync(channel.Name);
                    _joinedChannels.Add(channel.Name);
                    histories[channel.Name] = outcome.History;
                }
                catch (ChannelPasswordRequiredException)
                {
                    // First-time protected channel — joining stays a manual, prompted action
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Auto-join failed for #{Channel}", channel.Name);
                }
            }

            onStatus("Connected");
            return new ConnectResult(loginResponse, channels, histories);
        }
        catch
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _apiClient.Dispose();
            _apiClient = null;
            throw;
        }
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
        _roomKeys.Clear();
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

    public async Task<JoinOutcome> JoinChannelAsync(string channelName, string? password = null)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected");
        try
        {
            var outcome = await _connection.JoinChannelAsync(channelName, password);
            _joinedChannels.Add(channelName);
            return outcome;
        }
        catch (ChannelPasswordRequiredException)
        {
            // Not actually joined — don't track, or reconnects would retry a doomed join
            _joinedChannels.Remove(channelName);
            throw;
        }
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

    public Task<List<MessageDto>> GetHistoryAsync(string channel, int count = HubConstants.DefaultHistoryCount, int offset = 0) =>
        _connection?.GetHistoryAsync(channel, count, offset)
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
            // One channel gone bad (deleted, membership revoked) must not stop the rest
            try
            {
                await _connection.JoinChannelAsync(channel);
                _joinedChannels.Add(channel);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Rejoin failed for #{Channel}", channel);
            }
        }

        Log.Information("Rejoined {Count} channel(s) after reconnect", _joinedChannels.Count);
    }

    // ── SignalR Event Wiring ──────────────────────────────────────────────

    private void WireConnectionEvents(EchoHubConnection connection)
    {
        connection.OnMessageReceived += msg => MessageReceived?.Invoke(msg);
        connection.OnUserJoined += (ch, user, presence) => UserJoined?.Invoke(ch, user, presence);
        connection.OnUserLeft += (ch, user) => UserLeft?.Invoke(ch, user);
        connection.OnUserStatusChanged += p => UserStatusChanged?.Invoke(p);
        connection.OnUserKicked += (ch, user, reason) => UserKicked?.Invoke(ch, user, reason);
        connection.OnUserBanned += (user, reason) => UserBanned?.Invoke(user, reason);
        connection.OnForceDisconnect += reason => ForceDisconnected?.Invoke(reason);
        connection.OnMessageDeleted += (ch, id) => MessageDeleted?.Invoke(ch, id);
        connection.OnChannelDeleted += ch => ChannelDeleted?.Invoke(ch);
        connection.OnChannelNuked += ch => ChannelNuked?.Invoke(ch);
        connection.OnChannelUpdated += ch =>
        {
            _roomKeys.MarkChannelEncrypted(ch.Name, ch.IsEncrypted);
            ChannelUpdated?.Invoke(ch);
        };
        connection.OnError += msg => Error?.Invoke(msg);
        connection.OnConnectionStateChanged += status => ConnectionStatusChanged?.Invoke(status);
        connection.OnReconnected += () => Reconnected?.Invoke();
    }

    // ── Token Persistence ─────────────────────────────────────────────────

    private void HandleTokensRefreshed()
    {
        if (_apiClient?.RefreshToken is null) return;
        var config = ConfigManager.Load();
        var server = FindServer(config, _apiClient.BaseUrl);
        if (server is not null && server.RememberMe)
        {
            server.RefreshToken = _apiClient.RefreshToken;
            ConfigManager.Save(config);
        }
    }

    private static SavedServer? FindServer(ClientConfig config, string url) =>
        config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));

    // ── Dispose ───────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _apiClient?.Dispose();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
