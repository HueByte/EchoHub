using System.Text;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;

namespace EchoHub.Server.Irc;

public static class IrcMessageFormatter
{
    private const int MaxIrcLineContentBytes = 400;

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

        // Caption text first (may be empty when the message is attachments-only)
        if (!string.IsNullOrEmpty(message.Content))
        {
            foreach (var chunk in SplitMessage(message.Content, MaxIrcLineContentBytes))
                lines.Add($"{prefix} PRIVMSG {ircChannel} :{chunk}");
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
