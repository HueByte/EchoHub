using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI;

/// <summary>
/// A colored text segment within a chat line.
/// </summary>
public record ChatSegment(string Text, Attribute? Color);

/// <summary>
/// A single line in the chat, composed of colored segments.
/// </summary>
public partial class ChatLine
{
    public List<ChatSegment> Segments { get; }
    public int TextLength { get; }
    public Guid? MessageId { get; set; }
    public bool IsMention { get; set; }

    public ChatLine(string plainText)
    {
        Segments = [new ChatSegment(plainText, null)];
        TextLength = plainText.Length;
    }

    public ChatLine(List<ChatSegment> segments)
    {
        Segments = segments;
        TextLength = segments.Sum(s => s.Text.Length);
    }

    public override string ToString() => string.Concat(Segments.Select(s => s.Text));

    /// <summary>
    /// Wrap this line into multiple lines that fit within the given width.
    /// Continuation lines are indented with the specified number of spaces.
    /// </summary>
    public List<ChatLine> Wrap(int width, int continuationIndent = 0)
    {
        if (width <= 0 || TextLength <= width)
            return [this];

        var results = new List<ChatLine>();
        var currentSegments = new List<ChatSegment>();
        int col = 0;

        foreach (var segment in Segments)
        {
            int segPos = 0;
            while (segPos < segment.Text.Length)
            {
                int remaining = width - col;
                if (remaining <= 0)
                {
                    // Emit current line and start a new one
                    results.Add(new ChatLine(currentSegments));
                    currentSegments = [];

                    // Add indent for continuation
                    if (continuationIndent > 0)
                    {
                        currentSegments.Add(new ChatSegment(new string(' ', continuationIndent), null));
                        col = continuationIndent;
                    }
                    else
                    {
                        col = 0;
                    }

                    remaining = width - col;
                }

                int take = Math.Min(segment.Text.Length - segPos, remaining);
                currentSegments.Add(new ChatSegment(segment.Text.Substring(segPos, take), segment.Color));
                col += take;
                segPos += take;
            }
        }

        if (currentSegments.Count > 0)
            results.Add(new ChatLine(currentSegments));

        return results;
    }

    /// <summary>
    /// Returns true if a line contains printable color tags.
    /// </summary>
    public static bool HasColorTags(string text) =>
        text.Contains("{F:") || text.Contains("{B:") || text.Contains("{X}");

    /// <summary>
    /// Parse a string containing printable color tags into colored segments.
    /// Format: {F:RRGGBB} (foreground), {B:RRGGBB} (background), {X} (reset).
    /// </summary>
    public static ChatLine FromColoredText(string text, Attribute? defaultAttr = null)
    {
        var segments = new List<ChatSegment>();
        int lastIndex = 0;
        Color? currentFg = null;
        Color? currentBg = null;
        var defaultFg = defaultAttr?.Foreground;
        var defaultBg = defaultAttr?.Background ?? Color.Black;

        Attribute? BuildAttr()
        {
            if (currentFg is null && currentBg is null) return defaultAttr;
            var fg = currentFg ?? defaultFg ?? Color.White;
            var bg = currentBg ?? defaultBg;
            return new Attribute(fg, bg);
        }

        foreach (Match match in ColorTagRegex().Matches(text))
        {
            if (match.Index > lastIndex)
            {
                var t = text[lastIndex..match.Index];
                if (t.Length > 0)
                    segments.Add(new ChatSegment(t, BuildAttr()));
            }

            if (match.Groups[1].Success)
            {
                // Reset {X}
                currentFg = null;
                currentBg = null;
            }
            else if (match.Groups[2].Success)
            {
                var hex = match.Groups[3].Value;
                var r = Convert.ToInt32(hex[..2], 16);
                var g = Convert.ToInt32(hex[2..4], 16);
                var b = Convert.ToInt32(hex[4..6], 16);
                if (match.Groups[2].Value == "F")
                    currentFg = new Color(r, g, b);
                else
                    currentBg = new Color(r, g, b);
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            var t = text[lastIndex..];
            if (t.Length > 0)
                segments.Add(new ChatSegment(t, BuildAttr()));
        }

        return segments.Count > 0 ? new ChatLine(segments) : new ChatLine("");
    }

    // {X} (reset), {F:RRGGBB} (foreground), {B:RRGGBB} (background)
    [GeneratedRegex(@"\{(?:(X)|(?:(F|B):([0-9A-Fa-f]{6})))\}")]
    private static partial Regex ColorTagRegex();
}

/// <summary>
/// Custom list data source for chat messages with per-character coloring.
/// </summary>
public class ChatListSource : IListDataSource
{
    private readonly List<ChatLine> _lines = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _lines.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    public void Add(ChatLine line)
    {
        _lines.Add(line);
        UpdateMaxLength(line);
        RaiseCollectionChanged();
    }

    public void AddRange(IEnumerable<ChatLine> lines)
    {
        foreach (var line in lines)
        {
            _lines.Add(line);
            UpdateMaxLength(line);
        }
        RaiseCollectionChanged();
    }

    public void InsertRange(int index, IEnumerable<ChatLine> lines)
    {
        var items = lines.ToList();
        _lines.InsertRange(index, items);
        foreach (var line in items)
            UpdateMaxLength(line);
        RaiseCollectionChanged();
    }

    public void Clear()
    {
        _lines.Clear();
        MaxItemLength = 0;
        RaiseCollectionChanged();
    }

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _lines.Select(l => l.ToString()).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var chatLine = _lines[item];
        // Always use Normal — chat messages should not show focus/selection highlight
        var normalAttr = listView.GetAttributeForRole(VisualRole.Normal);
        var mentionBg = chatLine.IsMention ? ChatColors.MentionHighlightAttr.Background : (Color?)null;

        int charPos = 0;
        int drawnChars = 0;

        foreach (var segment in chatLine.Segments)
        {
            var attr = segment.Color ?? normalAttr;
            // Override background for mention-highlighted lines
            if (mentionBg.HasValue)
                attr = new Attribute(attr.Foreground, mentionBg.Value);
            listView.SetAttribute(attr);

            foreach (var ch in segment.Text)
            {
                if (charPos >= viewportX && drawnChars < width)
                {
                    listView.AddRune(new Rune(ch));
                    drawnChars++;
                }
                charPos++;
            }
        }

        // Fill remaining width with spaces
        var fillAttr = mentionBg.HasValue ? new Attribute(normalAttr.Foreground, mentionBg.Value) : normalAttr;
        listView.SetAttribute(fillAttr);
        while (drawnChars < width)
        {
            listView.AddRune(new Rune(' '));
            drawnChars++;
        }
    }

    private void UpdateMaxLength(ChatLine line)
    {
        if (line.TextLength > MaxItemLength)
            MaxItemLength = line.TextLength;
    }

    private void RaiseCollectionChanged()
    {
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void Dispose() { }
}

/// <summary>
/// Custom list data source for colored channel list rendering.
/// Active channel gets a > indicator, unread channels are bright with a count badge.
/// </summary>
public class ChannelListSource : IListDataSource
{
    private readonly List<string> _channelNames = [];
    private readonly Dictionary<string, int> _unreadCounts = [];
    private string _activeChannel = string.Empty;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public int Count => _channelNames.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    private static readonly Attribute ActiveAttr = new(Color.White, Color.Black);
    private static readonly Attribute UnreadAttr = new(Color.BrightCyan, Color.Black);
    private static readonly Attribute NormalAttr = new(Color.DarkGray, Color.Black);
    private static readonly Attribute BadgeAttr = new(Color.BrightYellow, Color.Black);

    public void Update(List<string> channels, Dictionary<string, int> unread, string activeChannel)
    {
        _channelNames.Clear();
        _channelNames.AddRange(channels);
        _unreadCounts.Clear();
        foreach (var kv in unread)
            _unreadCounts[kv.Key] = kv.Value;
        _activeChannel = activeChannel;
        MaxItemLength = channels.Count > 0 ? channels.Max(c => c.Length + 6) : 0;
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _channelNames.Select(n => $"#{n}").ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var name = _channelNames[item];
        var isActive = name == _activeChannel;
        _unreadCounts.TryGetValue(name, out var unread);
        var hasUnread = unread > 0;

        var focusAttr = listView.GetAttributeForRole(VisualRole.Focus);
        var prefix = isActive ? "> " : "  ";
        var channelText = $"#{name}";
        var badge = hasUnread ? $" ({unread})" : "";

        int drawnChars = 0;

        // Use focus attr if this row is selected
        if (selected)
        {
            listView.SetAttribute(focusAttr);
            foreach (var ch in (prefix + channelText + badge))
            {
                if (drawnChars < width) { listView.AddRune(new Rune(ch)); drawnChars++; }
            }
        }
        else
        {
            // Prefix
            var prefixAttr = isActive ? ActiveAttr : NormalAttr;
            listView.SetAttribute(prefixAttr);
            foreach (var ch in prefix)
            {
                if (drawnChars < width) { listView.AddRune(new Rune(ch)); drawnChars++; }
            }

            // Channel name
            var nameAttr = isActive ? ActiveAttr : hasUnread ? UnreadAttr : NormalAttr;
            listView.SetAttribute(nameAttr);
            foreach (var ch in channelText)
            {
                if (drawnChars < width) { listView.AddRune(new Rune(ch)); drawnChars++; }
            }

            // Unread badge
            if (hasUnread)
            {
                listView.SetAttribute(BadgeAttr);
                foreach (var ch in badge)
                {
                    if (drawnChars < width) { listView.AddRune(new Rune(ch)); drawnChars++; }
                }
            }
        }

        // Fill rest
        var fillAttr = selected ? focusAttr : listView.GetAttributeForRole(VisualRole.Normal);
        listView.SetAttribute(fillAttr);
        while (drawnChars < width)
        {
            listView.AddRune(new Rune(' '));
            drawnChars++;
        }
    }

    public void Dispose() { }
}

/// <summary>
/// Custom list data source for the online users panel with per-user nickname colors.
/// </summary>
public class UserListSource : IListDataSource
{
    private readonly List<(string Text, Attribute? NameColor)> _users = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public int Count => _users.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    public void Update(List<(string Text, Attribute? NameColor)> users)
    {
        _users.Clear();
        _users.AddRange(users);
        MaxItemLength = users.Count > 0 ? users.Max(u => u.Text.Length) : 0;
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _users.Select(u => u.Text).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var (text, nameColor) = _users[item];
        var normalAttr = listView.GetAttributeForRole(selected ? VisualRole.Focus : VisualRole.Normal);

        // Find where the name starts (after status icon + space + optional role badge)
        // Format: "● ★Username" or "● Username"
        int nameStart = 0;
        int i = 0;
        // Skip status icon
        while (i < text.Length && !char.IsLetterOrDigit(text[i]) && text[i] != '_') i++;
        nameStart = i;

        int drawnChars = 0;

        // Draw prefix (status icon + role badge) in normal color
        var prefixAttr = normalAttr;
        for (int c = 0; c < nameStart && c < text.Length; c++)
        {
            if (drawnChars < width)
            {
                listView.SetAttribute(prefixAttr);
                listView.AddRune(new Rune(text[c]));
                drawnChars++;
            }
        }

        // Draw name in nickname color
        var userAttr = nameColor ?? normalAttr;
        if (selected) userAttr = normalAttr; // use focus attr when selected
        for (int c = nameStart; c < text.Length; c++)
        {
            if (drawnChars < width)
            {
                listView.SetAttribute(userAttr);
                listView.AddRune(new Rune(text[c]));
                drawnChars++;
            }
        }

        // Fill rest
        listView.SetAttribute(normalAttr);
        while (drawnChars < width)
        {
            listView.AddRune(new Rune(' '));
            drawnChars++;
        }
    }

    public void Dispose() { }
}

/// <summary>
/// Shared color attributes for chat rendering (timestamps, system messages).
/// </summary>
public static class ChatColors
{
    public static readonly Attribute TimestampAttr = new(Color.DarkGray, Color.Black);
    public static readonly Attribute SystemAttr = new(new Color(0, 180, 180), Color.Black);
    public static readonly Attribute MentionHighlightAttr = new(Color.White, new Color(80, 40, 0));
}

/// <summary>
/// Helper to parse hex colors to Terminal.Gui Attributes.
/// </summary>
public static class ColorHelper
{
    public static Attribute? ParseHexColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            return null;

        try
        {
            var r = Convert.ToInt32(hex[..2], 16);
            var g = Convert.ToInt32(hex[2..4], 16);
            var b = Convert.ToInt32(hex[4..6], 16);
            return new Attribute(new Color(r, g, b), Color.Black);
        }
        catch
        {
            return null;
        }
    }
}
