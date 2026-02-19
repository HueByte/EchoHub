using System.Text;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;

namespace EchoHub.Server.Irc;

public static class IrcMessageFormatter
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
                break;

            case MessageType.Image:
                lines.Add($"{prefix} PRIVMSG {ircChannel} :[Image: {message.AttachmentFileName}]");
                if (message.AttachmentUrl is not null)
                    lines.Add($"{prefix} PRIVMSG {ircChannel} :Download: {message.AttachmentUrl}");

                foreach (var line in message.Content.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (trimmed.Length > 0)
                        lines.Add($"{prefix} PRIVMSG {ircChannel} :{trimmed}");
                }
                break;

            case MessageType.File:
                lines.Add($"{prefix} PRIVMSG {ircChannel} :[File: {message.AttachmentFileName}] {message.AttachmentUrl}");
                break;
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
