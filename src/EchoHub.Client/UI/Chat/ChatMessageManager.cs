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

        var time = FormatDateTime(DateTimeOffset.Now);
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
        var time = FormatDateTime(DateTimeOffset.Now);
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
    /// Prepend older messages at the front of a channel's buffer, skipping any that are already present.
    /// Fires <see cref="HistoryPrepended"/> when new lines are actually inserted.
    /// </summary>
    public void PrependHistory(string channelName, List<MessageDto> olderMessages)
    {
        if (!_channelMessages.TryGetValue(channelName, out var existing))
            return;

        var existingIds = existing
            .Where(l => l.MessageId.HasValue)
            .Select(l => l.MessageId!.Value)
            .ToHashSet();

        var newLines = olderMessages
            .Where(m => !existingIds.Contains(m.Id))
            .SelectMany(FormatMessage)
            .ToList();

        if (newLines.Count == 0)
            return;

        existing.InsertRange(0, newLines);

        if (channelName == _currentChannel)
            HistoryPrepended?.Invoke(channelName);
    }

    /// <summary>
    /// Fired after older messages are prepended to a channel's buffer. Parameter is the channel name.
    /// </summary>
    public event Action<string>? HistoryPrepended;

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
        var time = FormatDateTime(message.SentAt);
        var senderName = message.SenderUsername + ":";
        var senderColor = HexColorHelper.ParseHexColor(message.SenderNicknameColor);

        var indent = new string(' ', $"[{time}] {senderName} ".Length);
        var pad = new string(' ', 7);

        var lines = new List<ChatLine>();
        var hasContent = !string.IsNullOrWhiteSpace(message.Content);
        var attachments = message.Attachments ?? [];

        // Header line: caption text, or a summary when the message is attachments-only
        if (hasContent)
        {
            var displayContent = EmojiHelper.ReplaceEmoji(message.Content);
            var contentLines = displayContent.Split('\n');
            lines.Add(BuildChatLineWithMentions(time, senderName, senderColor, $" {contentLines[0].TrimEnd('\r')}"));
            for (int i = 1; i < contentLines.Length; i++)
                lines.Add(new ChatLine(ChatColors.SplitMentions($"{indent}{contentLines[i].TrimEnd('\r')}")));
        }
        else
        {
            var summary = attachments.Count switch
            {
                0 => " ",
                1 => $" [{attachments[0].Kind.ToString().ToLowerInvariant()}]",
                _ => $" [{attachments.Count} attachments]",
            };
            lines.Add(BuildChatLine(time, senderName, senderColor, summary));
        }

        foreach (var l in lines)
            l.ContinuationIndent = indent.Length;

        // One block per attachment
        foreach (var attachment in attachments)
        {
            switch (attachment.Kind)
            {
                case Core.Models.AttachmentKind.Image:
                    if (!string.IsNullOrWhiteSpace(attachment.AsciiPreview))
                    {
                        foreach (var artLine in attachment.AsciiPreview.Split('\n'))
                        {
                            var trimmed = artLine.TrimEnd('\r');
                            lines.Add(ChatLine.HasColorTags(trimmed)
                                ? ChatLine.FromColoredText(pad + trimmed)
                                : new ChatLine($"{pad}{trimmed}"));
                        }
                    }
                    lines.Add(AttachmentActionLine(pad,
                        $"[↓ save original] {attachment.FileName} [{FormatFileSize(attachment.FileSize)}]",
                        ChatColors.FileAttr, attachment));
                    break;

                case Core.Models.AttachmentKind.Audio:
                    lines.Add(AttachmentActionLine(pad,
                        $"♪ [Audio: {attachment.FileName}] [{FormatFileSize(attachment.FileSize)}]",
                        ChatColors.AudioAttr, attachment));
                    break;

                default:
                    lines.Add(AttachmentActionLine(pad,
                        $"[File: {attachment.FileName}] [{FormatFileSize(attachment.FileSize)}]",
                        ChatColors.FileAttr, attachment));
                    break;
            }
        }

        // Link embeds (from caption URLs)
        if (message.Embeds is { Count: > 0 })
        {
            var chatWidth = _chatWidth > 0 ? _chatWidth : 80;
            foreach (var embed in message.Embeds)
                lines.AddRange(FormatEmbed(embed, indent, chatWidth));
        }

        foreach (var line in lines)
        {
            line.MessageId = message.Id;
            line.SenderUsername = message.SenderUsername;
        }

        if (hasContent && !string.IsNullOrEmpty(_currentUser))
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

    /// <summary>
    /// Builds a clickable attachment line carrying the metadata the message list uses to
    /// route activation (play audio, download file, save original image).
    /// </summary>
    private static ChatLine AttachmentActionLine(string pad, string text, Attribute color, AttachmentDto attachment)
    {
        var line = new ChatLine(new List<ChatSegment>
        {
            new(pad, null),
            new(text, color),
        });
        line.AttachmentUrl = attachment.Url;
        line.AttachmentFileName = attachment.FileName;
        line.AttachmentKind = attachment.Kind;
        return line;
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

        var borderAttr = HexColorHelper.ParseHexColor(embed.ThemeColor) ?? ChatColors.EmbedBorderAttr;

        void AddTextLine(string text, Attribute? color)
        {
            lines.Add(new ChatLine(
            [
                new ChatSegment(indent, null),
                new ChatSegment(border, borderAttr),
                new ChatSegment(text, color)
            ]));
        }

        if (!string.IsNullOrWhiteSpace(embed.SiteName))
            AddTextLine(embed.SiteName, borderAttr);

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

    private static string FormatDateTime(DateTimeOffset timestamp)
    {
        if (timestamp.Date == DateTimeOffset.Now.Date)
            return timestamp.ToLocalTime().ToString("t");
        else
            return timestamp.ToLocalTime().ToString("g");
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
