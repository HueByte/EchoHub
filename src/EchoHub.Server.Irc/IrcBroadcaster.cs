using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;

namespace EchoHub.Server.Irc;

public class IrcBroadcaster : IChatBroadcaster
{
    private readonly IrcGatewayService _gateway;

    public IrcBroadcaster(IrcGatewayService gateway)
    {
        _gateway = gateway;
    }

    public async Task SendMessageToChannelAsync(string channelName, MessageDto message)
    {
        var lines = IrcMessageFormatter.FormatMessage(message);

        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
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
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.ConnectionId == excludeConnectionId) continue;
            await conn.SendAsync($":{username}!{username}@echohub JOIN #{channelName}");
        }
    }

    public async Task SendUserLeftAsync(string channelName, string username)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.Nickname == username) continue;
            await conn.SendAsync($":{username}!{username}@echohub PART #{channelName}");
        }
    }

    public async Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
    {
        var target = channelName ?? channel.Name;
        if (channel.Topic is null) return;

        foreach (var conn in _gateway.GetConnectionsInChannel(target))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} TOPIC #{channel.Name} :{channel.Topic}");
        }
    }

    public Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence)
    {
        // IRC has no active status broadcast. Clients discover away via WHOIS/WHO.
        return Task.CompletedTask;
    }

    public async Task SendUserKickedAsync(string channelName, string username, string? reason)
    {
        var reasonText = reason is not null ? $" :{reason}" : "";
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} KICK #{channelName} {username}{reasonText}");
        }
    }

    public async Task SendUserBannedAsync(string username, string? reason)
    {
        var reasonText = reason ?? "You have been banned.";
        foreach (var conn in _gateway.GetAllConnections())
        {
            if (conn.Nickname == username)
                await conn.SendAsync($":{_gateway.Options.ServerName} NOTICE {username} :You have been banned: {reasonText}");
        }
    }

    public async Task SendMessageDeletedAsync(string channelName, Guid messageId)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} NOTICE {conn.Nickname ?? "*"} :Message {messageId} was deleted in #{channelName}");
        }
    }

    public async Task SendChannelNukedAsync(string channelName)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} NOTICE {conn.Nickname ?? "*"} :All messages in #{channelName} have been cleared");
        }
    }

    public async Task SendErrorAsync(string connectionId, string message)
    {
        if (!connectionId.StartsWith("irc-")) return;

        if (_gateway.Connections.TryGetValue(connectionId, out var conn))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} NOTICE {conn.Nickname ?? "*"} :{message}");
        }
    }
}
