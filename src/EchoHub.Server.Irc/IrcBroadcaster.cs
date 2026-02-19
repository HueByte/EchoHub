using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;

namespace EchoHub.Server.Irc;

public class IrcBroadcaster(IrcGatewayService gateway) : IChatBroadcaster
{
    public async Task SendMessageToChannelAsync(string channelName, MessageDto message)
    {
        var lines = IrcMessageFormatter.FormatMessage(message);

        foreach (var conn in gateway.GetConnectionsInChannel(channelName))
        {
            // IRC convention: don't echo sender's own message
            if (conn.Nickname == message.SenderUsername)
                continue;

            foreach (var line in lines)
                await conn.SendAsync(line);
        }
    }

    public async Task SendUserJoinedAsync(string channelName, string username, string? excludeConnectionId = null)
    {
        foreach (var conn in gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.ConnectionId == excludeConnectionId) continue;
            await conn.SendAsync($":{username}!{username}@echohub JOIN #{channelName}");
        }
    }

    public async Task SendUserLeftAsync(string channelName, string username)
    {
        foreach (var conn in gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.Nickname == username) continue;
            await conn.SendAsync($":{username}!{username}@echohub PART #{channelName}");
        }
    }

    public async Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
    {
        var target = channelName ?? channel.Name;
        if (channel.Topic is null) return;

        foreach (var conn in gateway.GetConnectionsInChannel(target))
        {
            await conn.SendAsync($":{gateway.Options.ServerName} TOPIC #{channel.Name} :{channel.Topic}");
        }
    }

    public Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence)
    {
        // IRC has no active status broadcast. Clients discover away via WHOIS/WHO.
        return Task.CompletedTask;
    }

    public async Task SendErrorAsync(string connectionId, string message)
    {
        if (!connectionId.StartsWith("irc-")) return;

        if (gateway.Connections.TryGetValue(connectionId, out var conn))
        {
            await conn.SendAsync($":{gateway.Options.ServerName} NOTICE {conn.Nickname ?? "*"} :{message}");
        }
    }
}
