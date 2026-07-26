using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;

namespace EchoHub.Server.Irc;

public class IrcBroadcaster : IChatBroadcaster
{
    private readonly IrcGatewayService _gateway;
    private readonly IMessageEncryptionService _encryption;

    public IrcBroadcaster(IrcGatewayService gateway, IMessageEncryptionService encryption)
    {
        _gateway = gateway;
        _encryption = encryption;
    }

    public async Task SendMessageToChannelAsync(string channelName, MessageDto message, string? excludeConnectionId = null)
    {
        // Decrypt transport-encrypted content for IRC clients (they can't handle
        // app-layer encryption). E2E room ciphertext ($RC1$) passes through untouched.
        var decryptedMessage = message with
        {
            Content = _encryption.Decrypt(message.Content),
            ReplyTo = message.ReplyTo is { } reply ? reply with { Content = _encryption.Decrypt(reply.Content) } : null,
        };

        // Clients that support message-tags receive the +reply tag instead of
        // the text reply prefix, so format without it. The sender's echo-message
        // also needs the raw content to correlate with what they sent.
        var hasReply = message.ReplyTo is not null;
        var modernMessage = hasReply ? decryptedMessage with { ReplyTo = null } : decryptedMessage;
        var legacyLines = IrcMessageFormatter.FormatMessage(decryptedMessage, _gateway.Options.PublicBaseUrl);
        var modernLines = hasReply
            ? IrcMessageFormatter.FormatMessage(modernMessage, _gateway.Options.PublicBaseUrl)
            : legacyLines;

        // Compute shared tag components (same for all connections in this channel)
        var serverTimeTag = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var msgid = message.Id.ToString("D");
        var replyMsgid = message.ReplyTo?.MessageId.ToString("D");

        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            // Don't echo back to traditional IRC clients (they display locally).
            // Echo-message clients need their own messages back for msgid/+reply tracking.
            if (conn.ConnectionId == excludeConnectionId && !conn.HasCap("echo-message"))
                continue;

            // Use modern lines (without reply prefix) for echo-message senders and
            // for any client that gets the +reply tag via message-tags capability.
            var useModern = conn.ConnectionId == excludeConnectionId || conn.HasCap("message-tags");
            var lines = useModern ? modernLines : legacyLines;

            // Build per-connection tags
            var tags = new List<(string Key, string? Value)>();

            if (conn.HasCap("server-time"))
                tags.Add(("time", serverTimeTag));

            if (conn.HasCap("message-tags"))
            {
                tags.Add(("msgid", msgid));
                if (replyMsgid is not null)
                    tags.Add(("+reply", replyMsgid));
            }

            var tagPrefix = tags.Count > 0 ? IrcMessage.BuildTagPrefix([.. tags]) : "";

            if (conn.HasCap("draft/multiline") && conn.HasCap("batch") && lines.Count > 1)
            {
                await SendMultilineBatchAsync(conn, channelName, tagPrefix, lines);
            }
            else
            {
                foreach (var line in lines)
                    await conn.SendAsync(tagPrefix + line);
            }
        }
    }

    private async Task SendMultilineBatchAsync(
        IrcClientConnection conn, string channelName, string tagPrefix, List<string> lines)
    {
        if (lines.Count == 0) return;

        var batchRef = $"ml{Guid.NewGuid().ToString("N")[..8]}";
        var ircChannel = $"#{channelName}";

        // Per draft/multiline spec: msgid and +reply go on the BATCH start line
        // only; per-message tags (time, batch) go on individual lines.
        // Parse the pre-built tagPrefix to split batch-level from line-level tags.
        string batchTags, lineTags;
        if (tagPrefix.Length > 0 && tagPrefix.StartsWith('@'))
        {
            var tagBody = tagPrefix.AsSpan(1).TrimEnd(' ');
            var parts = tagBody.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries);
            var batchParts = new List<string>();
            var lineParts = new List<string>();
            foreach (var part in parts)
            {
                if (part.StartsWith("msgid=") || part.StartsWith("+reply="))
                    batchParts.Add(part);
                else
                    lineParts.Add(part);
            }
            batchTags = batchParts.Count > 0 ? "@" + string.Join(";", batchParts) + " " : "";
            lineParts.Add("batch=" + batchRef);
            lineTags = "@" + string.Join(";", lineParts) + " ";
        }
        else
        {
            batchTags = "";
            lineTags = "@batch=" + batchRef + " ";
        }

        // Extract the sender prefix from the first line
        var firstLine = lines[0];
        var senderPrefix = firstLine.StartsWith(':')
            ? firstLine[1..firstLine.IndexOf(' ')]
            : _gateway.Options.ServerName;

        await conn.SendAsync($"{batchTags}:{senderPrefix} BATCH +{batchRef} draft/multiline {ircChannel}");

        foreach (var line in lines)
            await conn.SendAsync($"{lineTags}{line}");

        await conn.SendAsync($"BATCH -{batchRef}");
    }

    public async Task SendUserJoinedAsync(string channelName, string username, UserPresenceDto? presence, string? excludeConnectionId = null)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.ConnectionId == excludeConnectionId) continue;
            var line = $":{username}!{username}@echohub JOIN #{channelName}";

            var tags = new List<(string Key, string? Value)>();
            if (conn.HasCap("server-time"))
                tags.Add(("time", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

            var tagPrefix = tags.Count > 0 ? IrcMessage.BuildTagPrefix([.. tags]) : "";
            await conn.SendAsync(tagPrefix + line);
        }
    }

    public async Task SendUserLeftAsync(string channelName, string username)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            if (conn.Nickname == username) continue;
            var line = $":{username}!{username}@echohub PART #{channelName}";

            var tags = new List<(string Key, string? Value)>();
            if (conn.HasCap("server-time"))
                tags.Add(("time", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

            var tagPrefix = tags.Count > 0 ? IrcMessage.BuildTagPrefix([.. tags]) : "";
            await conn.SendAsync(tagPrefix + line);
        }
    }

    public async Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
    {
        var target = channelName ?? channel.Name;
        if (channel.Topic is null) return;

        foreach (var conn in _gateway.GetConnectionsInChannel(target))
        {
            var line = $":{_gateway.Options.ServerName} TOPIC #{channel.Name} :{channel.Topic}";

            var tags = new List<(string Key, string? Value)>();
            if (conn.HasCap("server-time"))
                tags.Add(("time", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

            var tagPrefix = tags.Count > 0 ? IrcMessage.BuildTagPrefix([.. tags]) : "";
            await conn.SendAsync(tagPrefix + line);
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

    public async Task SendChannelDeletedAsync(string channelName)
    {
        foreach (var conn in _gateway.GetConnectionsInChannel(channelName))
        {
            await conn.SendAsync($":{_gateway.Options.ServerName} NOTICE {conn.Nickname ?? "*"} :Channel #{channelName} has been deleted");
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

    public async Task ForceDisconnectUserAsync(List<string> connectionIds, string reason)
    {
        foreach (var connId in connectionIds)
        {
            if (!connId.StartsWith("irc-")) continue;

            if (_gateway.Connections.TryGetValue(connId, out var conn))
            {
                try
                {
                    await conn.SendAsync($"ERROR :Closing Link: {reason}");
                    await conn.DisposeAsync();
                }
                catch { /* connection may already be closed */ }
            }
        }
    }
}
