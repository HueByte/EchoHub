using System.Text;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Core.Security;

namespace EchoHub.Server.Irc;

public static class IrcMessageFormatter
{
    private const int MaxIrcLineContentBytes = 400;
    private const int MaxReplySnippetLength = 80;

    /// <summary>
    /// Format a MessageDto as one or more IRC PRIVMSG lines.
    /// Attachments are rendered as single link lines \u2014 the widely-supported IRC convention
    /// (clients auto-preview or open plain http(s) URLs) \u2014 never as terminal color art.
    /// <paramref name="publicBaseUrl"/> makes the links absolute so any IRC client can open them.
    /// </summary>
    public static List<string> FormatMessage(MessageDto message, string? publicBaseUrl = null)
    {
        var lines = new List<string>();
        var ircChannel = $"#{message.ChannelName}";
        var prefix = $":{message.SenderUsername}!{message.SenderUsername}@echohub";

        // Reply reference → the "> nick: snippet" quoting convention IRC users know
        var replyPrefix = FormatReplyPrefix(message.ReplyTo);

        // Caption text first (may be empty when the message is attachments-only)
        if (!string.IsNullOrEmpty(message.Content))
        {
            // /me actions arrive as CTCP ACTION content; each chunk must stay a
            // well-formed CTCP message (\x01ACTION …\x01) or clients render garbage.
            if (MessageConventions.TryParseAction(message.Content, out var actionText))
            {
                if (replyPrefix is not null)
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :{replyPrefix.TrimEnd(' ', '|', ' ')}");

                foreach (var chunk in SplitMessage(actionText, MaxIrcLineContentBytes))
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :{MessageConventions.FormatAction(chunk)}");
            }
            else
            {
                var content = replyPrefix is not null ? replyPrefix + message.Content : message.Content;
                foreach (var chunk in SplitMessage(content, MaxIrcLineContentBytes))
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :{chunk}");
            }
        }
        else if (replyPrefix is not null)
        {
            lines.Add($"{prefix} PRIVMSG {ircChannel} :{replyPrefix.TrimEnd(' ', '|', ' ')}");
        }

        // One link line per attachment
        if (message.Attachments is { Count: > 0 })
        {
            foreach (var attachment in message.Attachments)
            {
                var url = ToAbsoluteUrl(attachment.Url, publicBaseUrl);
                var tag = attachment.Kind switch
                {
                    AttachmentKind.Image => $"[Image: {attachment.FileName}]",
                    AttachmentKind.Audio => $"\u266a [Audio: {attachment.FileName}]",
                    _ => $"[File: {attachment.FileName}]",
                };
                lines.Add($"{prefix} PRIVMSG {ircChannel} :{tag} {url}");
            }
        }

        // Append embed previews if present
        if (message.Embeds is { Count: > 0 })
        {
            foreach (var embed in message.Embeds)
                lines.AddRange(FormatEmbed(prefix, ircChannel, embed));
        }

        return lines;
    }

    /// <summary>
    /// "&gt; nick: snippet | " prefix for replies. Room ciphertext can't be rendered
    /// (IRC can't join encrypted rooms anyway) and is shown as a placeholder.
    /// </summary>
    private static string? FormatReplyPrefix(ReplyRefDto? replyTo)
    {
        if (replyTo is null)
            return null;

        var snippet = RoomCrypto.IsRoomCiphertext(replyTo.Content)
            ? "[encrypted]"
            : replyTo.Content.Replace('\n', ' ').Replace('\r', ' ');

        if (MessageConventions.TryParseAction(snippet, out var actionText))
            snippet = $"* {replyTo.SenderUsername} {actionText}";

        if (snippet.Length > MaxReplySnippetLength)
            snippet = snippet[..MaxReplySnippetLength] + "…";

        return $"> {replyTo.SenderUsername}: {snippet} | ";
    }

    /// <summary>
    /// Joins a relative attachment path onto the configured public base URL.
    /// Already-absolute URLs and unset base URLs pass through unchanged.
    /// </summary>
    public static string ToAbsoluteUrl(string url, string? publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;
        return $"{publicBaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    /// <summary>
    /// Format a link embed as IRC PRIVMSG lines (text-only, no ASCII thumbnail).
    /// </summary>
    private static List<string> FormatEmbed(string prefix, string ircChannel, EmbedDto embed)
    {
        var lines = new List<string>();

        var header = new List<string>();
        if (!string.IsNullOrWhiteSpace(embed.SiteName))
            header.Add(embed.SiteName);
        if (!string.IsNullOrWhiteSpace(embed.Title))
            header.Add(embed.Title);

        if (header.Count > 0)
            lines.Add($"{prefix} PRIVMSG {ircChannel} :\u2502 {string.Join(" \u2014 ", header)}");

        if (!string.IsNullOrWhiteSpace(embed.Description))
        {
            var desc = embed.Description.Length > 200
                ? embed.Description[..197] + "..."
                : embed.Description;
            lines.Add($"{prefix} PRIVMSG {ircChannel} :\u2502 {desc}");
        }

        return lines;
    }

    /// <summary>
    /// Split a message into chunks of approximately maxBytes (UTF-8), at word boundaries.
    /// </summary>
    public static List<string> SplitMessage(string content, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(content) <= maxBytes)
            return [content];

        var chunks = new List<string>();
        var current = new StringBuilder();
        var currentBytes = 0;

        foreach (var word in content.Split(' '))
        {
            var wordBytes = Encoding.UTF8.GetByteCount(word) + 1; // +1 for space

            if (currentBytes + wordBytes > maxBytes && current.Length > 0)
            {
                chunks.Add(current.ToString().TrimEnd());
                current.Clear();
                currentBytes = 0;
            }

            current.Append(word).Append(' ');
            currentBytes += wordBytes;
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().TrimEnd());

        return chunks;
    }
}
