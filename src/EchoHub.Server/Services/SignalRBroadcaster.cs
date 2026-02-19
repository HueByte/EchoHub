using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EchoHub.Server.Services;

public class SignalRBroadcaster : IChatBroadcaster
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PresenceTracker _presenceTracker;
    private IHubContext<ChatHub, IEchoHubClient>? _hubContext;

    private IHubContext<ChatHub, IEchoHubClient> HubContext
        => _hubContext ??= _serviceProvider.GetRequiredService<IHubContext<ChatHub, IEchoHubClient>>();

    public SignalRBroadcaster(IServiceProvider serviceProvider, PresenceTracker presenceTracker)
    {
        _serviceProvider = serviceProvider;
        _presenceTracker = presenceTracker;
    }

    public Task SendMessageToChannelAsync(string channelName, MessageDto message)
        => HubContext.Clients.Group(channelName).ReceiveMessage(message);

    public Task SendUserJoinedAsync(string channelName, string username, string? excludeConnectionId = null)
    {
        if (excludeConnectionId is not null && !excludeConnectionId.StartsWith("irc-"))
            return HubContext.Clients.GroupExcept(channelName, [excludeConnectionId]).UserJoined(channelName, username);

        return HubContext.Clients.Group(channelName).UserJoined(channelName, username);
    }

    public Task SendUserLeftAsync(string channelName, string username)
        => HubContext.Clients.Group(channelName).UserLeft(channelName, username);

    public Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
    {
        if (channelName is not null)
            return HubContext.Clients.Group(channelName).ChannelUpdated(channel);

        return HubContext.Clients.All.ChannelUpdated(channel);
    }

    public Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence)
    {
        var connections = _presenceTracker.GetConnectionsInChannels(channelNames)
            .Where(c => !c.StartsWith("irc-"))
            .ToList();

        if (connections.Count == 0)
            return Task.CompletedTask;

        return HubContext.Clients.Clients(connections).UserStatusChanged(presence);
    }

    public Task SendUserKickedAsync(string channelName, string username, string? reason)
        => HubContext.Clients.Group(channelName).UserKicked(channelName, username, reason);

    public Task SendUserBannedAsync(string username, string? reason)
        => HubContext.Clients.All.UserBanned(username, reason);

    public Task SendMessageDeletedAsync(string channelName, Guid messageId)
        => HubContext.Clients.Group(channelName).MessageDeleted(channelName, messageId);

    public Task SendChannelNukedAsync(string channelName)
        => HubContext.Clients.Group(channelName).ChannelNuked(channelName);

    public Task SendErrorAsync(string connectionId, string message)
    {
        if (connectionId.StartsWith("irc-"))
            return Task.CompletedTask;

        return HubContext.Clients.Client(connectionId).Error(message);
    }

    public Task ForceDisconnectUserAsync(List<string> connectionIds, string reason)
    {
        var signalRIds = connectionIds.Where(c => !c.StartsWith("irc-")).ToList();
        if (signalRIds.Count == 0)
            return Task.CompletedTask;

        return HubContext.Clients.Clients(signalRIds).ForceDisconnect(reason);
    }
}
