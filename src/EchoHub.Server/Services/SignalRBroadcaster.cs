using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EchoHub.Server.Services;

public class SignalRBroadcaster(
    IHubContext<ChatHub, IEchoHubClient> hubContext,
    PresenceTracker presenceTracker) : IChatBroadcaster
{
    public Task SendMessageToChannelAsync(string channelName, MessageDto message)
        => hubContext.Clients.Group(channelName).ReceiveMessage(message);

    public Task SendUserJoinedAsync(string channelName, string username, string? excludeConnectionId = null)
    {
        if (excludeConnectionId is not null && !excludeConnectionId.StartsWith("irc-"))
            return hubContext.Clients.GroupExcept(channelName, [excludeConnectionId]).UserJoined(channelName, username);

        return hubContext.Clients.Group(channelName).UserJoined(channelName, username);
    }

    public Task SendUserLeftAsync(string channelName, string username)
        => hubContext.Clients.Group(channelName).UserLeft(channelName, username);

    public Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
    {
        if (channelName is not null)
            return hubContext.Clients.Group(channelName).ChannelUpdated(channel);

        return hubContext.Clients.All.ChannelUpdated(channel);
    }

    public Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence)
    {
        var connections = presenceTracker.GetConnectionsInChannels(channelNames)
            .Where(c => !c.StartsWith("irc-"))
            .ToList();

        if (connections.Count == 0)
            return Task.CompletedTask;

        return hubContext.Clients.Clients(connections).UserStatusChanged(presence);
    }

    public Task SendErrorAsync(string connectionId, string message)
    {
        if (connectionId.StartsWith("irc-"))
            return Task.CompletedTask;

        return hubContext.Clients.Client(connectionId).Error(message);
    }
}
