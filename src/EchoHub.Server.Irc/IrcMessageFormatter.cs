using System.Text;
using System.Text.RegularExpressions;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;

namespace EchoHub.Server.Irc;

public static partial class IrcMessageFormatter
{
    private const int MaxIrcLineContentBytes = 400;

    /// <summary>
    /// Format a MessageDto as one or more IRC PRIVMSG lines.
    /// </summary>
    public static List<string> FormatMessage(MessageDto message)
    {
        var lines = new List<string>();
        var ircChannel = $"#{message.ChannelName}";
        var prefix = $":{message.SenderUsername}!{message.SenderUsername}@echohub";

        switch (message.Type)
        {
            case MessageType.Text:
                foreach (var chunk in SplitMessage(message.Content, MaxIrcLineContentBytes))
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :{chunk}");

                // Append embed previews if present
                if (message.Embeds is { Count: > 0 })
                {
                    foreach (var embed in message.Embeds)
                        lines.AddRange(FormatEmbed(prefix, ircChannel, embed));
                }
                break;

            case MessageType.Image:
                lines.Add($"{prefix} PRIVMSG {ircChannel} :[Image: {message.AttachmentFileName}]");
                if (message.AttachmentUrl is not null)
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :Download: {message.AttachmentUrl}");

                foreach (var line in message.Content.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (trimmed.Length > 0)
                        lines.Add($"{prefix} PRIVMSG {ircChannel} :{ColorTagsToAnsi(trimmed)}");
                }
                break;

            case MessageType.File:
                lines.Add($"{prefix} PRIVMSG {ircChannel} :[File: {message.AttachmentFileName}] {message.AttachmentUrl}");
                break;

            case MessageType.Audio:
                lines.Add($"{prefix} PRIVMSG {ircChannel} :\u266a [Audio: {message.AttachmentFileName}] {message.AttachmentUrl}");
                break;
        }

        return lines;
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
    /// Convert printable color tags ({F:RRGGBB}, {B:RRGGBB}, {X}) to ANSI escape codes for IRC clients.
    /// Also passes through content that already uses ANSI codes unchanged.
    /// </summary>
    public static string ColorTagsToAnsi(string text)
    {
        if (!text.Contains('{'))
            return text;

        return ColorTagRegex().Replace(text, match =>
        {
            if (match.Groups[1].Success) // {X} reset
                return "\x1b[0m";
            if (match.Groups[2].Success) // {F:RRGGBB} or {B:RRGGBB}
            {
                var hex = match.Groups[3].Value;
                var r = Convert.ToInt32(hex[..2], 16);
                var g = Convert.ToInt32(hex[2..4], 16);
                var b = Convert.ToInt32(hex[4..6], 16);
                var code = match.Groups[2].Value == "F" ? "38" : "48";
                return $"\x1b[{code};2;{r};{g};{b}m";
            }
            return match.Value;
        });
    }

    [GeneratedRegex(@"\{(?:(X)|(?:(F|B):([0-9A-Fa-f]{6})))\}")]
    private static partial Regex ColorTagRegex();

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
