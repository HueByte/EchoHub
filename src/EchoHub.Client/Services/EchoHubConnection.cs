using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Core.Security;
using Microsoft.AspNetCore.SignalR.Client;

namespace EchoHub.Client.Services;

/// <summary>
/// Result of joining a channel: decrypted history plus, for end-to-end encrypted
/// channels, the key envelope needed to unlock the room content key.
/// </summary>
public sealed record JoinOutcome(List<MessageDto> History, string? EncryptionSalt, string? WrappedRoomKey);

/// <summary>
/// Thrown when joining a channel fails because a password is required or incorrect.
/// The UI catches this to prompt the user and retry.
/// </summary>
public sealed class ChannelPasswordRequiredException : Exception
{
    public string ChannelName { get; }

    public ChannelPasswordRequiredException(string channelName, string message) : base(message)
    {
        ChannelName = channelName;
    }
}

/// <summary>
/// Thrown when sending into an end-to-end encrypted channel whose room key isn't cached:
/// without the key the message would leave the client as plaintext, which must never happen.
/// </summary>
public sealed class RoomLockedException : Exception
{
    public string ChannelName { get; }

    public RoomLockedException(string channelName)
        : base($"#{channelName} is end-to-end encrypted and locked — enter its passphrase to unlock it before sending.")
    {
        ChannelName = channelName;
    }
}

public sealed class EchoHubConnection : IAsyncDisposable
{
    public const string LockedMessagePlaceholder =
        "[encrypted — rejoin this channel with its passphrase to unlock]";

    private readonly HubConnection _connection;
    private readonly ClientEncryptionService _encryption;
    private readonly RoomKeyStore _roomKeys;

    public event Action<MessageDto>? OnMessageReceived;
    public event Action<string, string, UserPresenceDto?>? OnUserJoined;
    public event Action<string, string>? OnUserLeft;
    public event Action<ChannelDto>? OnChannelUpdated;
    public event Action<UserPresenceDto>? OnUserStatusChanged;
    public event Action<string, string, string?>? OnUserKicked;
    public event Action<string, string?>? OnUserBanned;
    public event Action<string, Guid>? OnMessageDeleted;
    public event Action<string>? OnChannelDeleted;
    public event Action<string>? OnChannelNuked;
    public event Action<string>? OnForceDisconnect;
    public event Action<string>? OnError;
    public event Action<string>? OnConnectionStateChanged;
    public event Action? OnReconnected;

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public EchoHubConnection(string serverUrl, ApiClient apiClient, ClientEncryptionService encryption, RoomKeyStore roomKeys)
    {
        _encryption = encryption;
        _roomKeys = roomKeys;
        var hubUrl = serverUrl.TrimEnd('/') + HubConstants.ChatHubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => apiClient.GetValidTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        _connection.Reconnecting += _ =>
        {
            OnConnectionStateChanged?.Invoke("Reconnecting...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            OnConnectionStateChanged?.Invoke("Connected");
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            OnConnectionStateChanged?.Invoke("Disconnected");
            return Task.CompletedTask;
        };
    }

    private void RegisterHandlers()
    {
        _connection.On<MessageDto>(nameof(Core.Contracts.IEchoHubClient.ReceiveMessage), message =>
        {
            OnMessageReceived?.Invoke(DecryptMessage(message));
        });

        _connection.On<string, string, UserPresenceDto?>(nameof(Core.Contracts.IEchoHubClient.UserJoined), (channelName, username, presence) =>
        {
            OnUserJoined?.Invoke(channelName, username, presence);
        });

        _connection.On<string, string>(nameof(Core.Contracts.IEchoHubClient.UserLeft), (channelName, username) =>
        {
            OnUserLeft?.Invoke(channelName, username);
        });

        _connection.On<ChannelDto>(nameof(Core.Contracts.IEchoHubClient.ChannelUpdated), channel =>
        {
            OnChannelUpdated?.Invoke(channel);
        });

        _connection.On<UserPresenceDto>(nameof(Core.Contracts.IEchoHubClient.UserStatusChanged), presence =>
        {
            OnUserStatusChanged?.Invoke(presence);
        });

        _connection.On<string, string, string?>(nameof(Core.Contracts.IEchoHubClient.UserKicked), (channelName, username, reason) =>
        {
            OnUserKicked?.Invoke(channelName, username, reason);
        });

        _connection.On<string, string?>(nameof(Core.Contracts.IEchoHubClient.UserBanned), (username, reason) =>
        {
            OnUserBanned?.Invoke(username, reason);
        });

        _connection.On<string, Guid>(nameof(Core.Contracts.IEchoHubClient.MessageDeleted), (channelName, messageId) =>
        {
            OnMessageDeleted?.Invoke(channelName, messageId);
        });

        _connection.On<string>(nameof(Core.Contracts.IEchoHubClient.ChannelDeleted), channelName =>
        {
            OnChannelDeleted?.Invoke(channelName);
        });

        _connection.On<string>(nameof(Core.Contracts.IEchoHubClient.ChannelNuked), channelName =>
        {
            OnChannelNuked?.Invoke(channelName);
        });

        _connection.On<string>(nameof(Core.Contracts.IEchoHubClient.ForceDisconnect), reason =>
        {
            OnForceDisconnect?.Invoke(reason);
        });

        _connection.On<string>(nameof(Core.Contracts.IEchoHubClient.Error), message =>
        {
            OnError?.Invoke(message);
        });
    }

    public async Task ConnectAsync()
    {
        OnConnectionStateChanged?.Invoke("Connecting...");
        await _connection.StartAsync();
        OnConnectionStateChanged?.Invoke("Connected");
    }

    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
        OnConnectionStateChanged?.Invoke("Disconnected");
    }

    public async Task<JoinOutcome> JoinChannelAsync(string channelName, string? password = null)
    {
        var result = await _connection.InvokeAsync<JoinChannelResult>("JoinChannel", channelName, password);
        if (!result.Success)
        {
            if (result.PasswordRequired)
                throw new ChannelPasswordRequiredException(channelName, result.Error ?? "Channel is password protected.");
            throw new InvalidOperationException(result.Error ?? "Failed to join channel.");
        }
        if (result.WrappedRoomKey is not null)
            _roomKeys.MarkChannelEncrypted(channelName, true);
        return new JoinOutcome(DecryptMessages(result.History), result.EncryptionSalt, result.WrappedRoomKey);
    }

    public async Task LeaveChannelAsync(string channelName)
    {
        await _connection.InvokeAsync("LeaveChannel", channelName);
    }

    public async Task SendMessageAsync(string channelName, string content)
    {
        // Room layer first (end-to-end, server can't read), then transport encryption
        if (_roomKeys.TryGetKey(channelName, out var roomKey))
            content = RoomCrypto.EncryptText(content, roomKey);
        else if (_roomKeys.IsChannelEncrypted(channelName))
            throw new RoomLockedException(channelName); // never fall through to plaintext

        var encrypted = _encryption.Encrypt(content);
        await _connection.InvokeAsync("SendMessage", channelName, encrypted);
    }

    public async Task<List<MessageDto>> GetHistoryAsync(string channelName, int count = HubConstants.DefaultHistoryCount, int offset = 0)
    {
        var messages = await _connection.InvokeAsync<List<MessageDto>>("GetChannelHistory", channelName, count, offset);
        return DecryptMessages(messages);
    }

    public async Task UpdateStatusAsync(UserStatus status, string? statusMessage = null)
    {
        await _connection.InvokeAsync("UpdateStatus", status, statusMessage);
    }

    public async Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName)
    {
        return await _connection.InvokeAsync<List<UserPresenceDto>>("GetOnlineUsers", channelName);
    }

    private List<MessageDto> DecryptMessages(List<MessageDto> messages)
    {
        return messages.Select(DecryptMessage).ToList();
    }

    /// <summary>
    /// Strips the transport encryption, then the room layer for E2E channels, from the
    /// message content and every attachment preview. Without the room key the content is
    /// replaced by a locked placeholder — re-fetch history after unlocking to render it.
    /// </summary>
    private MessageDto DecryptMessage(MessageDto message)
    {
        _roomKeys.TryGetKey(message.ChannelName, out var roomKey);

        var content = DecryptField(message.Content, roomKey) ?? LockedMessagePlaceholder;

        List<AttachmentDto>? attachments = null;
        if (message.Attachments is { Count: > 0 })
        {
            attachments = message.Attachments
                .Select(a => a with { AsciiPreview = a.AsciiPreview is null ? null : DecryptField(a.AsciiPreview, roomKey) })
                .ToList();
        }

        return message with { Content = content, Attachments = attachments };
    }

    /// <summary>
    /// Decrypts one field: strips transport encryption, then the room layer if it is room
    /// ciphertext. Returns null when it is room ciphertext but the room key is missing/wrong.
    /// </summary>
    private string? DecryptField(string value, byte[]? roomKey)
    {
        var plain = _encryption.Decrypt(value);
        if (!RoomCrypto.IsRoomCiphertext(plain))
            return plain;

        if (roomKey is not null && RoomCrypto.TryDecryptText(plain, roomKey, out var decrypted))
            return decrypted;

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
