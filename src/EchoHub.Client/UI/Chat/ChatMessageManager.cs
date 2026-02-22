using System.Text.RegularExpressions;
using EchoHub.Client.UI.Helpers;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

/// <summary>
/// Owns chat message storage, formatting, and mutation.
/// Fires <see cref="MessagesChanged"/> when a channel's message list is modified
/// so the UI layer can refresh.
/// </summary>
public sealed class ChatMessageManager
{
    private readonly Dictionary<string, List<ChatLine>> _channelMessages = [];
    private readonly Dictionary<string, int> _channelUnread = [];

    private string _currentUser = string.Empty;
    private string _currentChannel = string.Empty;
    private int _chatWidth;

    /// <summary>
    /// Fired after any mutation to a channel's messages. Parameter is the channel name.
    /// </summary>
    public event Action<string>? MessagesChanged;

    /// <summary>
    /// The currently active channel (used for unread tracking and @mention detection).
    /// </summary>
    public string CurrentChannel
    {
        get => _currentChannel;
        set => _currentChannel = value;
    }

    public string CurrentUser => _currentUser;

    public void SetCurrentUser(string username) => _currentUser = username;

    public void SetChatWidth(int width) => _chatWidth = width;

    // ── Queries ──────────────────────────────────────────────────────

    public List<ChatLine>? GetMessages(string channelName)
    {
        return _channelMessages.TryGetValue(channelName, out var messages) ? messages : null;
    }

    public int GetUnreadCount(string channelName)
    {
        return _channelUnread.TryGetValue(channelName, out var count) ? count : 0;
    }

    public void ClearUnread(string channelName)
    {
        _channelUnread[channelName] = 0;
    }

    internal Dictionary<string, int> GetUnreadCounts() => _channelUnread;

    // ── Mutations ────────────────────────────────────────────────────

    /// <summary>
    /// Format and store a received message. Increments unread count if not the active channel.
    /// </summary>
    public void AddMessage(MessageDto message)
    {
        var lines = FormatMessage(message);
        if (!_channelMessages.TryGetValue(message.ChannelName, out var messages))
        {
            messages = [];
            _channelMessages[message.ChannelName] = messages;
        }

        foreach (var line in lines)
            messages.Add(line);

        if (message.ChannelName == _currentChannel)
        {
            MessagesChanged?.Invoke(message.ChannelName);
        }
        else
        {
            _channelUnread.TryGetValue(message.ChannelName, out var count);
            _channelUnread[message.ChannelName] = count + 1;
            MessagesChanged?.Invoke(message.ChannelName);
        }
    }

    /// <summary>
    /// Add a system/informational message to a channel with colored styling.
    /// </summary>
    public void AddSystemMessage(string channelName, string text)
    {
        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }

        var time = DateTimeOffset.Now.ToString("HH:mm");
        var textLines = text.Split('\n');

        messages.Add(new ChatLine(
        [
            new($"[{time}] ", ChatColors.TimestampAttr),
            new($"** {textLines[0].TrimEnd('\r')}", ChatColors.SystemAttr)
        ]));

        var indent = new string(' ', $"[{time}] ** ".Length);
        for (int i = 1; i < textLines.Length; i++)
        {
            var line = textLines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            messages.Add(new ChatLine(
            [
                new($"{indent}{line}", ChatColors.SystemAttr)
            ]));
        }

        if (channelName == _currentChannel)
            MessagesChanged?.Invoke(channelName);
    }

    /// <summary>
    /// Add a status change message to a channel with colored styling.
    /// </summary>
    public void AddStatusMessage(string channelName, string username, string status)
    {
        var time = DateTimeOffset.Now.ToString("HH:mm");
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new($"** {username} is now {status}", ChatColors.SystemAttr)
        };

        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }
        messages.Add(new ChatLine(segments));

        if (channelName == _currentChannel)
            MessagesChanged?.Invoke(channelName);
    }

    /// <summary>
    /// Remove all lines associated with a specific message ID.
    /// </summary>
    public void RemoveMessage(string channelName, Guid messageId)
    {
        if (_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages.RemoveAll(l => l.MessageId == messageId);
            if (channelName == _currentChannel)
                MessagesChanged?.Invoke(channelName);
        }
    }

    /// <summary>
    /// Clear all messages from a specific channel.
    /// </summary>
    public void ClearChannelMessages(string channelName)
    {
        if (_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages.Clear();
            if (channelName == _currentChannel)
                MessagesChanged?.Invoke(channelName);
        }
    }

    /// <summary>
    /// Load historical messages into a channel, replacing any existing messages.
    /// </summary>
    public void LoadHistory(string channelName, List<MessageDto> messages)
    {
        var formatted = messages.SelectMany(FormatMessage).ToList();
        _channelMessages[channelName] = formatted;

        if (channelName == _currentChannel)
            MessagesChanged?.Invoke(channelName);
    }

    /// <summary>
    /// Reset all message state (used on disconnect).
    /// </summary>
    public void ClearAll()
    {
        _channelMessages.Clear();
        _channelUnread.Clear();
        _currentChannel = string.Empty;
        _currentUser = string.Empty;
    }

    // ── Formatting ───────────────────────────────────────────────────

    private List<ChatLine> FormatMessage(MessageDto message)
    {
        var time = message.SentAt.ToLocalTime().ToString("HH:mm");
        var senderName = message.SenderUsername + ":";
        var senderColor = HexColorHelper.ParseHexColor(message.SenderNicknameColor);

        var lines = new List<ChatLine>();

        switch (message.Type)
        {
            case MessageType.Image:
                lines.Add(BuildChatLine(time, senderName, senderColor, " [Image]"));
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    foreach (var artLine in message.Content.Split('\n'))
                    {
                        var trimmed = artLine.TrimEnd('\r');
                        if (ChatLine.HasColorTags(trimmed))
                            lines.Add(ChatLine.FromColoredText("       " + trimmed));
                        else
                            lines.Add(new ChatLine($"       {trimmed}"));
                    }
                }
                break;

            case MessageType.Audio:
                var audioName = message.AttachmentFileName ?? "unknown";
                var audioSize = FormatFileSize(message.AttachmentFileSize);
                var audioLine = BuildChatLineColored(time, senderName, senderColor,
                    $" \u266a [Audio: {audioName}] [{audioSize}]", ChatColors.AudioAttr);
                audioLine.AttachmentUrl = message.AttachmentUrl;
                audioLine.AttachmentFileName = audioName;
                audioLine.Type = MessageType.Audio;
                lines.Add(audioLine);
                break;

            case MessageType.File:
                var fileName = message.AttachmentFileName ?? "unknown";
                var fileSize = FormatFileSize(message.AttachmentFileSize);
                var fileLine = BuildChatLineColored(time, senderName, senderColor,
                    $" [File: {fileName}] [{fileSize}]", ChatColors.FileAttr);
                fileLine.AttachmentUrl = message.AttachmentUrl;
                fileLine.AttachmentFileName = fileName;
                fileLine.Type = MessageType.File;
                lines.Add(fileLine);
                break;

            case MessageType.Text:
            default:
                var displayContent = EmojiHelper.ReplaceEmoji(message.Content);
                var contentLines = displayContent.Split('\n');
                var firstLine = contentLines[0].TrimEnd('\r');
                lines.Add(BuildChatLineWithMentions(time, senderName, senderColor, $" {firstLine}"));
                var indent = new string(' ', $"[{time}] {senderName} ".Length);
                for (int i = 1; i < contentLines.Length; i++)
                {
                    var contText = $"{indent}{contentLines[i].TrimEnd('\r')}";
                    lines.Add(new ChatLine(ChatColors.SplitMentions(contText)));
                }

                if (message.Embeds is { Count: > 0 })
                {
                    var chatWidth = _chatWidth > 0 ? _chatWidth : 80;
                    foreach (var embed in message.Embeds)
                        lines.AddRange(FormatEmbed(embed, indent, chatWidth));
                }
                break;
        }

        foreach (var line in lines)
            line.MessageId = message.Id;

        if (!string.IsNullOrEmpty(_currentUser) && message.Type == MessageType.Text)
        {
            var pattern = $@"@{Regex.Escape(_currentUser)}\b";
            if (Regex.IsMatch(message.Content, pattern, RegexOptions.IgnoreCase))
            {
                foreach (var line in lines)
                    line.IsMention = true;
            }
        }

        return lines;
    }

    private static ChatLine BuildChatLine(string time, string senderName, Attribute? senderColor, string suffix)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
            new(suffix, null)
        };
        return new ChatLine(segments);
    }

    private static ChatLine BuildChatLineColored(string time, string senderName, Attribute? senderColor, string suffix, Attribute suffixColor)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
            new(suffix, suffixColor)
        };
        return new ChatLine(segments);
    }

    private static ChatLine BuildChatLineWithMentions(string time, string senderName, Attribute? senderColor, string suffix)
    {
        var segments = new List<ChatSegment>
        {
            new($"[{time}] ", ChatColors.TimestampAttr),
            new(senderName, senderColor),
        };
        segments.AddRange(ChatColors.SplitMentions(suffix));
        return new ChatLine(segments);
    }

    private static List<ChatLine> FormatEmbed(EmbedDto embed, string indent, int chatWidth)
    {
        var lines = new List<ChatLine>();
        const string border = "\u258f "; // ▏ + space
        const int borderCols = 2;
        int indentCols = indent.GetColumns();
        int textWidth = chatWidth - indentCols - borderCols;
        if (textWidth < 20) textWidth = 20;

        void AddTextLine(string text, Attribute? color)
        {
            lines.Add(new ChatLine(
            [
                new ChatSegment(indent, null),
                new ChatSegment(border, ChatColors.EmbedBorderAttr),
                new ChatSegment(text, color)
            ]));
        }

        if (!string.IsNullOrWhiteSpace(embed.SiteName))
            AddTextLine(embed.SiteName, ChatColors.EmbedBorderAttr);

        if (!string.IsNullOrWhiteSpace(embed.Title))
        {
            foreach (var wrapped in WordWrap(embed.Title, textWidth))
                AddTextLine(wrapped, ChatColors.EmbedTitleAttr);
        }

        if (!string.IsNullOrWhiteSpace(embed.Description))
        {
            foreach (var wrapped in WordWrap(embed.Description, textWidth))
                AddTextLine(wrapped, ChatColors.EmbedDescAttr);
        }

        return lines;
    }

    private static List<string> WordWrap(string text, int maxCols)
    {
        if (maxCols <= 0)
            return [text];

        var result = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = "";

        foreach (var word in words)
        {
            var candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
            if (candidate.GetColumns() <= maxCols)
            {
                currentLine = candidate;
            }
            else
            {
                if (currentLine.Length > 0)
                    result.Add(currentLine);
                currentLine = word;
            }
        }

        if (currentLine.Length > 0)
            result.Add(currentLine);

        return result;
    }

    internal static string FormatFileSize(long? bytes)
    {
        if (bytes is null or 0)
            return "?";

        return bytes.Value switch
        {
            < 1024 => $"{bytes.Value} B",
            < 1024 * 1024 => $"{bytes.Value / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes.Value / (1024.0 * 1024.0):F1} MB",
            _ => $"{bytes.Value / (1024.0 * 1024.0 * 1024.0):F1} GB"
        };
    }
}
