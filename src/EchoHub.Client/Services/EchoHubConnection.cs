using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EchoHub.Client.Services;

public sealed class EchoHubConnection : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public event Action<MessageDto>? OnMessageReceived;
    public event Action<string, string>? OnUserJoined;
    public event Action<string, string>? OnUserLeft;
    public event Action<ChannelDto>? OnChannelUpdated;
    public event Action<UserPresenceDto>? OnUserStatusChanged;
    public event Action<string>? OnError;
    public event Action<string>? OnConnectionStateChanged;

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public EchoHubConnection(string serverUrl, string jwtToken)
    {
        var hubUrl = serverUrl.TrimEnd('/') + HubConstants.ChatHubPath;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(jwtToken);
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
            OnMessageReceived?.Invoke(message);
        });

        _connection.On<string, string>(nameof(Core.Contracts.IEchoHubClient.UserJoined), (channelName, username) =>
        {
            OnUserJoined?.Invoke(channelName, username);
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

    public async Task<List<MessageDto>> JoinChannelAsync(string channelName)
    {
        return await _connection.InvokeAsync<List<MessageDto>>("JoinChannel", channelName);
    }

    public async Task LeaveChannelAsync(string channelName)
    {
        await _connection.InvokeAsync("LeaveChannel", channelName);
    }

    public async Task SendMessageAsync(string channelName, string content)
    {
        await _connection.InvokeAsync("SendMessage", channelName, content);
    }

    public async Task<List<MessageDto>> GetHistoryAsync(string channelName, int count = HubConstants.DefaultHistoryCount)
    {
        return await _connection.InvokeAsync<List<MessageDto>>("GetChannelHistory", channelName, count);
    }

    public async Task UpdateStatusAsync(UserStatus status, string? statusMessage = null)
    {
        await _connection.InvokeAsync("UpdateStatus", status, statusMessage);
    }

    public async Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName)
    {
        return await _connection.InvokeAsync<List<UserPresenceDto>>("GetOnlineUsers", channelName);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
