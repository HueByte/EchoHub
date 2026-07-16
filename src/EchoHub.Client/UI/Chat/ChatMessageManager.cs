using System.Text;
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
    /// <summary>Columns reserved for the right-aligned nick column (WeeChat-style).</summary>
    public const int NickColWidth = 12;

    /// <summary>Columns before message text starts: "HH:mm " + nick column + " │ ".</summary>
    public const int ContentIndentCols = 6 + NickColWidth + 3;

    private readonly Dictionary<string, List<ChatLine>> _channelMessages = [];
    private readonly Dictionary<string, int> _channelUnread = [];
    private readonly Dictionary<string, DateTime> _channelLastDate = [];
    private readonly HashSet<string> _markedChannels = [];
    private readonly Dictionary<string, Guid> _markerAnchor = [];
    private readonly HashSet<string> _mentionChannels = [];
    private readonly Dictionary<string, Guid> _lastRead = [];
    private readonly Dictionary<string, Guid> _channelNewestId = [];

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
        set
        {
            if (_currentChannel == value)
                return;

            // Leaving a channel consumes its "new messages" marker so the next
            // unread burst gets a fresh one (irssi behavior), and everything
            // visible up to now counts as read.
            RemoveUnreadMarker(_currentChannel);
            MarkRead(_currentChannel);
            _currentChannel = value;
        }
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
        _mentionChannels.Remove(channelName);
        MarkRead(channelName);
    }

    /// <summary>
    /// Last message the user has read per channel — persisted by the orchestrator so
    /// unread/mention state can be seeded from history on the next connect.
    /// </summary>
    public IReadOnlyDictionary<string, Guid> LastReadIds => _lastRead;

    private void MarkRead(string channelName)
    {
        if (!string.IsNullOrEmpty(channelName) && _channelNewestId.TryGetValue(channelName, out var newest))
            _lastRead[channelName] = newest;
    }

    internal Dictionary<string, int> GetUnreadCounts() => _channelUnread;

    /// <summary>Channels with an unread @mention of the current user (cleared by <see cref="ClearUnread"/>).</summary>
    public IReadOnlySet<string> MentionChannels => _mentionChannels;

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

        // Day boundary → horizontal date rule
        var msgDate = message.SentAt.ToLocalTime().Date;
        if (!_channelLastDate.TryGetValue(message.ChannelName, out var lastDate) || lastDate != msgDate)
        {
            messages.Add(DateRule(msgDate));
            _channelLastDate[message.ChannelName] = msgDate;
        }

        var isCurrent = message.ChannelName == _currentChannel;
        _channelNewestId[message.ChannelName] = message.Id;
        if (isCurrent)
            _lastRead[message.ChannelName] = message.Id;

        // First unread message in an inactive channel → "new messages" marker,
        // anchored to this message so a history reload can re-place it
        if (!isCurrent && _markedChannels.Add(message.ChannelName))
        {
            messages.Add(UnreadMarkerRule());
            _markerAnchor[message.ChannelName] = message.Id;
        }

        foreach (var line in lines)
            messages.Add(line);

        if (!isCurrent)
        {
            _channelUnread.TryGetValue(message.ChannelName, out var count);
            _channelUnread[message.ChannelName] = count + 1;
            if (lines.Any(l => l.IsMention))
                _mentionChannels.Add(message.ChannelName);
        }

        MessagesChanged?.Invoke(message.ChannelName);
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

        var time = FormatTime(DateTimeOffset.Now);
        var textLines = text.Split('\n');

        var header = SystemHeaderSegments(time);
        header.Add(new(textLines[0].TrimEnd('\r'), ChatColors.SystemAttr));
        messages.Add(new ChatLine(header) { ContinuationPrefixSegments = RailPrefix() });

        for (int i = 1; i < textLines.Length; i++)
        {
            var line = textLines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            var segments = RailPrefix();
            segments.Add(new(line, ChatColors.SystemAttr));
            messages.Add(new ChatLine(segments) { ContinuationPrefixSegments = RailPrefix() });
        }

        if (channelName == _currentChannel)
            MessagesChanged?.Invoke(channelName);
    }

    /// <summary>
    /// Add a status change message to a channel with colored styling.
    /// </summary>
    public void AddStatusMessage(string channelName, string username, string status)
    {
        var time = FormatTime(DateTimeOffset.Now);
        var segments = SystemHeaderSegments(time);
        segments.Add(new($"{username} is now {status}", ChatColors.SystemAttr));

        if (!_channelMessages.TryGetValue(channelName, out var messages))
        {
            messages = [];
            _channelMessages[channelName] = messages;
        }
        messages.Add(new ChatLine(segments) { ContinuationPrefixSegments = RailPrefix() });

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
            _channelLastDate.Remove(channelName);
            _markedChannels.Remove(channelName);
            _markerAnchor.Remove(channelName);
            if (channelName == _currentChannel)
                MessagesChanged?.Invoke(channelName);
        }
    }

    /// <summary>
    /// Load historical messages into a channel, replacing any existing messages.
    /// When <paramref name="lastReadId"/> is given (persisted from a previous session),
    /// messages after it seed the unread count, @mention highlight, and the
    /// "new messages" marker — so activity that happened while offline still lights up.
    /// </summary>
    public void LoadHistory(string channelName, List<MessageDto> messages, Guid? lastReadId = null)
    {
        var formatted = FormatWithDateRules(messages, out var lastDate);

        if (messages.Count > 0)
            _channelNewestId[channelName] = messages[^1].Id;

        // Re-place the "new messages" marker at its anchor — channel selection
        // reloads history, which would otherwise wipe the marker right when the
        // user switches in to read the unread backlog.
        if (_markedChannels.Contains(channelName))
        {
            var anchorIdx = _markerAnchor.TryGetValue(channelName, out var anchorId)
                ? formatted.FindIndex(l => l.MessageId == anchorId)
                : -1;
            if (anchorIdx >= 0)
            {
                formatted.Insert(anchorIdx, UnreadMarkerRule());
            }
            else
            {
                // Anchor fell outside the fetched history window — drop the marker
                _markedChannels.Remove(channelName);
                _markerAnchor.Remove(channelName);
            }
        }
        else if (lastReadId is { } lastRead && messages.Count > 0)
        {
            SeedUnreadFromHistory(channelName, messages, formatted, lastRead);
        }

        _channelMessages[channelName] = formatted;

        if (lastDate is { } date)
            _channelLastDate[channelName] = date;
        else
            _channelLastDate.Remove(channelName);

        MessagesChanged?.Invoke(channelName);
    }

    /// <summary>
    /// Reconstructs unread state from a persisted last-read message id: places the
    /// "new messages" marker before the first unread message and, for inactive
    /// channels, seeds the unread count and @mention highlight. A last-read id that
    /// is no longer inside the fetched window treats the whole window as unread.
    /// </summary>
    private void SeedUnreadFromHistory(string channelName, List<MessageDto> messages,
        List<ChatLine> formatted, Guid lastReadId)
    {
        // FindIndex miss (-1 → 0) means the last-read message is older than the fetched
        // window: everything in the window counts as unread.
        var firstUnread = messages.FindIndex(m => m.Id == lastReadId) + 1;
        if (firstUnread >= messages.Count)
            return; // everything read

        var anchor = messages[firstUnread];
        var lineIdx = formatted.FindIndex(l => l.MessageId == anchor.Id);
        if (lineIdx < 0)
            return;

        formatted.Insert(lineIdx, UnreadMarkerRule());
        _markedChannels.Add(channelName);
        _markerAnchor[channelName] = anchor.Id;

        // The active channel shows the marker but is being read right now —
        // badges and mention highlights are only for background channels.
        if (channelName == _currentChannel)
            return;

        _channelUnread[channelName] = messages.Count - firstUnread;

        if (!string.IsNullOrEmpty(_currentUser))
        {
            var pattern = $@"@{Regex.Escape(_currentUser)}\b";
            if (messages.Skip(firstUnread).Any(m => Regex.IsMatch(m.Content, pattern, RegexOptions.IgnoreCase)))
                _mentionChannels.Add(channelName);
        }
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

        var fresh = olderMessages.Where(m => !existingIds.Contains(m.Id)).ToList();
        var newLines = FormatWithDateRules(fresh, out var lastBatchDate);

        if (newLines.Count == 0)
            return;

        // The buffer's leading date rule is redundant when the prepended batch
        // ends on the same day — the batch already carries that day's rule.
        if (lastBatchDate is { } batchDate
            && existing.Count > 0
            && existing[0].RuleLabel is { } label
            && !existing[0].IsUnreadMarker
            && label == DateRuleLabel(batchDate))
        {
            existing.RemoveAt(0);
        }

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
        _channelLastDate.Clear();
        _markedChannels.Clear();
        _markerAnchor.Clear();
        _mentionChannels.Clear();
        _lastRead.Clear();
        _channelNewestId.Clear();
        _currentChannel = string.Empty;
        _currentUser = string.Empty;
    }

    // ── Formatting ───────────────────────────────────────────────────

    private List<ChatLine> FormatMessage(MessageDto message)
    {
        var time = FormatTime(message.SentAt);
        // Show the display name, but keep color + click identity keyed to the username
        // so they stay consistent with the user list and profile lookups.
        var senderName = message.SenderDisplayName ?? message.SenderUsername;
        var senderColor = HexColorHelper.ParseHexColor(message.SenderNicknameColor)
            ?? NickColorHelper.GetAttribute(message.SenderUsername);

        var lines = new List<ChatLine>();
        var hasContent = !string.IsNullOrWhiteSpace(message.Content);
        var attachments = message.Attachments ?? [];

        // Header line: caption text, or a summary when the message is attachments-only
        if (hasContent)
        {
            var displayContent = EmojiHelper.ReplaceEmoji(message.Content);
            var contentLines = displayContent.Split('\n');

            var header = HeaderSegments(time, senderName, senderColor);
            header.AddRange(ChatColors.SplitMentions(contentLines[0].TrimEnd('\r')));
            lines.Add(new ChatLine(header));

            for (int i = 1; i < contentLines.Length; i++)
            {
                var segments = RailPrefix();
                segments.AddRange(ChatColors.SplitMentions(contentLines[i].TrimEnd('\r')));
                lines.Add(new ChatLine(segments));
            }
        }
        else
        {
            var summary = attachments.Count switch
            {
                0 => " ",
                1 => $"[{attachments[0].Kind.ToString().ToLowerInvariant()}]",
                _ => $"[{attachments.Count} attachments]",
            };
            var header = HeaderSegments(time, senderName, senderColor);
            header.Add(new(summary, null));
            lines.Add(new ChatLine(header));
        }

        foreach (var l in lines)
            l.ContinuationPrefixSegments = RailPrefix();

        // One block per attachment — every block hangs off the nick-column rail
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
                            var segments = RailPrefix();
                            if (ChatLine.HasColorTags(trimmed))
                                segments.AddRange(ChatLine.FromColoredText(trimmed).Segments);
                            else
                                segments.Add(new(trimmed, null));
                            lines.Add(new ChatLine(segments));
                        }
                    }
                    lines.Add(AttachmentActionLine(
                        $"[↓ save original] {attachment.FileName} [{FormatFileSize(attachment.FileSize)}]",
                        ChatColors.FileAttr, attachment));
                    break;

                case Core.Models.AttachmentKind.Audio:
                    lines.Add(AttachmentActionLine(
                        $"♪ [Audio: {attachment.FileName}] [{FormatFileSize(attachment.FileSize)}]",
                        ChatColors.AudioAttr, attachment));
                    break;

                default:
                    lines.Add(AttachmentActionLine(
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
                lines.AddRange(FormatEmbed(embed, chatWidth));
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
    private static ChatLine AttachmentActionLine(string text, Attribute color, AttachmentDto attachment)
    {
        var segments = RailPrefix();
        segments.Add(new(text, color));
        return new ChatLine(segments)
        {
            AttachmentUrl = attachment.Url,
            AttachmentFileName = attachment.FileName,
            AttachmentKind = attachment.Kind,
            ContinuationPrefixSegments = RailPrefix(),
        };
    }

    /// <summary>
    /// Leading segments of a message header line: dim "HH:mm ", the right-aligned
    /// nick column, and the " │ " rail. Message text follows at <see cref="ContentIndentCols"/>.
    /// </summary>
    private static List<ChatSegment> HeaderSegments(string time, string nick, Attribute? nickColor) =>
    [
        new($"{time} ", ChatColors.TimestampAttr),
        new(PadNick(nick), nickColor),
        new(" │ ", ChatColors.RailAttr),
    ];

    /// <summary>Header variant for system/status lines: "--" in the nick column.</summary>
    private static List<ChatSegment> SystemHeaderSegments(string time) =>
    [
        new($"{time} ", ChatColors.TimestampAttr),
        new(PadNick("--"), ChatColors.TimestampAttr),
        new(" │ ", ChatColors.RailAttr),
    ];

    /// <summary>
    /// Indent segments aligning continuation/attachment/embed lines under the message
    /// text, extending the │ rail. Returns a fresh mutable list each call.
    /// </summary>
    private static List<ChatSegment> RailPrefix() =>
    [
        new(new string(' ', 6 + NickColWidth + 1), null),
        new("│ ", ChatColors.RailAttr),
    ];

    /// <summary>
    /// Right-aligns a nick into the fixed nick column, truncating over-long nicks
    /// with an ellipsis. Grapheme/column aware.
    /// </summary>
    internal static string PadNick(string nick)
    {
        var cols = nick.GetColumns();
        if (cols > NickColWidth)
        {
            var sb = new StringBuilder();
            int used = 0;
            foreach (var g in GraphemeHelper.GetGraphemes(nick))
            {
                var gCols = Math.Max(g.GetColumns(), 1);
                if (used + gCols > NickColWidth - 1) break;
                sb.Append(g);
                used += gCols;
            }
            sb.Append('…');
            nick = sb.ToString();
            cols = used + 1;
        }
        return new string(' ', NickColWidth - cols) + nick;
    }

    internal static string DateRuleLabel(DateTime date) => date.ToString("ddd, MMM d yyyy");

    private static ChatLine DateRule(DateTime date)
    {
        var label = DateRuleLabel(date);
        return new ChatLine([new($"── {label} ──", ChatColors.DateRuleAttr)])
        {
            RuleLabel = label,
            RuleAttr = ChatColors.DateRuleAttr,
        };
    }

    private static ChatLine UnreadMarkerRule() =>
        new([new("── new messages ──", ChatColors.UnreadMarkerAttr)])
        {
            RuleLabel = "new messages",
            RuleAttr = ChatColors.UnreadMarkerAttr,
            IsUnreadMarker = true,
        };

    private void RemoveUnreadMarker(string channel)
    {
        if (string.IsNullOrEmpty(channel) || !_markedChannels.Remove(channel))
            return;

        _markerAnchor.Remove(channel);
        if (_channelMessages.TryGetValue(channel, out var messages))
            messages.RemoveAll(l => l.IsUnreadMarker);
    }

    /// <summary>
    /// Formats a chronological batch of messages, inserting a date rule before the
    /// first message and at every day boundary. Outputs the batch's last local date.
    /// </summary>
    private List<ChatLine> FormatWithDateRules(List<MessageDto> messages, out DateTime? lastDate)
    {
        var lines = new List<ChatLine>();
        lastDate = null;

        foreach (var message in messages)
        {
            var date = message.SentAt.ToLocalTime().Date;
            if (lastDate != date)
            {
                lines.Add(DateRule(date));
                lastDate = date;
            }
            lines.AddRange(FormatMessage(message));
        }

        return lines;
    }

    private static List<ChatLine> FormatEmbed(EmbedDto embed, int chatWidth)
    {
        var lines = new List<ChatLine>();
        const string border = "\u258f "; // ▏ + space
        const int borderCols = 2;
        int textWidth = chatWidth - ContentIndentCols - borderCols;
        if (textWidth < 20) textWidth = 20;

        var borderAttr = HexColorHelper.ParseHexColor(embed.ThemeColor) ?? ChatColors.EmbedBorderAttr;

        void AddTextLine(string text, Attribute? color)
        {
            var segments = RailPrefix();
            segments.Add(new ChatSegment(border, borderAttr));
            segments.Add(new ChatSegment(text, color));
            lines.Add(new ChatLine(segments));
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

    // Timestamps are compact HH:mm — the calendar day is carried by date rules,
    // inserted at every local-day boundary. Convert to local first so a message
    // near midnight lands under the right date rule.
    private static string FormatTime(DateTimeOffset timestamp) =>
        timestamp.ToLocalTime().ToString("HH:mm");

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
