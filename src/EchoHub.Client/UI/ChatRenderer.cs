using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using EchoHub.Core.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
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
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public MessageType? Type { get; set; }

    public ChatLine(string plainText)
    {
        Segments = [new ChatSegment(plainText, null)];
        TextLength = plainText.GetColumns();
    }

    public ChatLine(List<ChatSegment> segments)
    {
        Segments = segments;
        TextLength = segments.Sum(s => s.Text.GetColumns());
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
            var text = segment.Text;
            int chunkStart = 0;
            int charPos = 0;

            foreach (var grapheme in GraphemeHelper.GetGraphemes(text))
            {
                var graphemeCols = Math.Max(grapheme.GetColumns(), 1);

                if (col + graphemeCols > width)
                {
                    if (charPos > chunkStart)
                        currentSegments.Add(new ChatSegment(text[chunkStart..charPos], segment.Color));

                    results.Add(new ChatLine(currentSegments));
                    currentSegments = [];

                    if (continuationIndent > 0)
                    {
                        currentSegments.Add(new ChatSegment(new string(' ', continuationIndent), null));
                        col = continuationIndent;
                    }
                    else
                    {
                        col = 0;
                    }

                    chunkStart = charPos;
                }

                col += graphemeCols;
                charPos += grapheme.Length;
            }

            if (chunkStart < text.Length)
                currentSegments.Add(new ChatSegment(text[chunkStart..], segment.Color));
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
    /// Remove all color tags from text, returning only the visible characters.
    /// </summary>
    public static string StripColorTags(string text) =>
        ColorTagRegex().Replace(text, "");

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
        var defaultBg = defaultAttr?.Background ?? Color.Transparent;

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

    [GeneratedRegex(@"\{(?:(X)|(?:(F|B):([0-9A-Fa-f]{6})))\}")]
    private static partial Regex ColorTagRegex();
}

/// <summary>
/// Custom list data source for chat messages with per-segment coloring.
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

    public ChatLine? GetLine(int index) => index >= 0 && index < _lines.Count ? _lines[index] : null;

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _lines.Select(l => l.ToString()).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var chatLine = _lines[item];
        var normalAttr = listView.GetAttributeForRole(VisualRole.Normal);
        var mentionBg = chatLine.IsMention ? ChatColors.MentionHighlightAttr.Background : (Color?)null;

        int charPos = 0;
        int drawnChars = 0;

        foreach (var segment in chatLine.Segments)
        {
            var attr = segment.Color ?? normalAttr;
            if (attr.Background == Color.Transparent)
                attr = attr with { Background = normalAttr.Background };
            if (mentionBg.HasValue)
                attr = attr with { Background = mentionBg.Value };
            listView.SetAttribute(attr);

            foreach (var grapheme in GraphemeHelper.GetGraphemes(segment.Text))
            {
                var cols = Math.Max(grapheme.GetColumns(), 1);
                if (charPos >= viewportX && drawnChars + cols <= width)
                {
                    listView.AddStr(grapheme);
                    drawnChars += cols;
                }
                charPos += cols;
            }
        }

        var fillAttr = mentionBg.HasValue ? new Attribute(normalAttr.Foreground, mentionBg.Value) : normalAttr;
        listView.SetAttribute(fillAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
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

    private static readonly Attribute ActiveAttr = new(Color.White, Color.Transparent);
    private static readonly Attribute UnreadAttr = new(Color.BrightCyan, Color.Transparent);
    private static readonly Attribute NormalAttr = new(Color.DarkGray, Color.Transparent);
    private static readonly Attribute BadgeAttr = new(Color.BrightYellow, Color.Transparent);

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

        var normalAttr = listView.GetAttributeForRole(VisualRole.Normal);
        var focusAttr = listView.GetAttributeForRole(VisualRole.Focus);
        var prefix = isActive ? "> " : "  ";
        var channelText = $"#{name}";
        var badge = hasUnread ? $" ({unread})" : "";

        // Resolve Transparent backgrounds to the view's actual background
        Attribute Resolve(Attribute attr) =>
            attr.Background == Color.Transparent ? attr with { Background = normalAttr.Background } : attr;

        int drawnChars = 0;

        if (selected)
        {
            listView.SetAttribute(focusAttr);
            drawnChars = RenderHelpers.WriteText(listView, prefix + channelText + badge, drawnChars, width);
        }
        else
        {
            listView.SetAttribute(Resolve(isActive ? ActiveAttr : NormalAttr));
            drawnChars = RenderHelpers.WriteText(listView, prefix, drawnChars, width);

            listView.SetAttribute(Resolve(isActive ? ActiveAttr : hasUnread ? UnreadAttr : NormalAttr));
            drawnChars = RenderHelpers.WriteText(listView, channelText, drawnChars, width);

            if (hasUnread)
            {
                listView.SetAttribute(Resolve(BadgeAttr));
                drawnChars = RenderHelpers.WriteText(listView, badge, drawnChars, width);
            }
        }

        var fillAttr = selected ? focusAttr : normalAttr;
        listView.SetAttribute(fillAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
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
        MaxItemLength = users.Count > 0 ? users.Max(u => u.Text.GetColumns()) : 0;
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
        var graphemes = GraphemeHelper.GetGraphemes(text).ToList();
        int nameStart = 0;
        while (nameStart < graphemes.Count)
        {
            var g = graphemes[nameStart];
            if (g.Length > 0 && (char.IsLetterOrDigit(g[0]) || g[0] == '_'))
                break;
            nameStart++;
        }

        int drawnChars = 0;

        // Draw prefix (status icon + role badge) in normal color
        listView.SetAttribute(normalAttr);
        for (int i = 0; i < nameStart; i++)
        {
            var cols = Math.Max(graphemes[i].GetColumns(), 1);
            if (drawnChars + cols > width) break;
            listView.AddStr(graphemes[i]);
            drawnChars += cols;
        }

        // Draw name in nickname color
        var userAttr = selected ? normalAttr : nameColor ?? normalAttr;
        listView.SetAttribute(userAttr);
        for (int i = nameStart; i < graphemes.Count; i++)
        {
            var cols = Math.Max(graphemes[i].GetColumns(), 1);
            if (drawnChars + cols > width) break;
            listView.AddStr(graphemes[i]);
            drawnChars += cols;
        }

        // Fill rest
        listView.SetAttribute(normalAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
    }

    public void Dispose() { }
}

/// <summary>
/// Shared rendering helpers for IListDataSource implementations.
/// </summary>
static class RenderHelpers
{
    /// <summary>
    /// Write text grapheme-by-grapheme to a ListView, respecting a width limit.
    /// Returns the updated drawn-columns count.
    /// </summary>
    public static int WriteText(ListView lv, string text, int drawn, int maxWidth)
    {
        foreach (var grapheme in GraphemeHelper.GetGraphemes(text))
        {
            var cols = Math.Max(grapheme.GetColumns(), 1);
            if (drawn + cols > maxWidth) break;
            lv.AddStr(grapheme);
            drawn += cols;
        }
        return drawn;
    }
}

/// <summary>
/// Shared color attributes for chat rendering (timestamps, system messages).
/// </summary>
public static partial class ChatColors
{
    public static readonly Attribute TimestampAttr = new(Color.DarkGray, Color.Transparent);
    public static readonly Attribute SystemAttr = new(new Color(0, 180, 180), Color.Transparent);
    public static readonly Attribute MentionHighlightAttr = new(Color.White, new Color(80, 40, 0));
    public static readonly Attribute MentionTextAttr = new(new Color(255, 180, 50), Color.Transparent);
    public static readonly Attribute EmbedBorderAttr = new(new Color(91, 155, 213), Color.Transparent);
    public static readonly Attribute EmbedTitleAttr = new(Color.White, Color.Transparent);
    public static readonly Attribute EmbedDescAttr = new(new Color(160, 160, 160), Color.Transparent);
    public static readonly Attribute EmbedUrlAttr = new(new Color(100, 100, 100), Color.Transparent);
    public static readonly Attribute AudioAttr = new(new Color(180, 100, 255), Color.Transparent);
    public static readonly Attribute FileAttr = new(new Color(100, 180, 255), Color.Transparent);

    /// <summary>
    /// Split text around @mentions, giving each @word the MentionTextAttr accent color.
    /// Non-mention text uses the provided default color.
    /// </summary>
    public static List<ChatSegment> SplitMentions(string text, Attribute? defaultColor = null)
    {
        var segments = new List<ChatSegment>();
        int lastIndex = 0;

        foreach (Match match in MentionRegex().Matches(text))
        {
            if (match.Index > lastIndex)
                segments.Add(new ChatSegment(text[lastIndex..match.Index], defaultColor));

            segments.Add(new ChatSegment(match.Value, MentionTextAttr));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            segments.Add(new ChatSegment(text[lastIndex..], defaultColor));

        return segments;
    }

    [GeneratedRegex(@"@[\w-]+")]
    private static partial Regex MentionRegex();
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
            return new Attribute(new Color(r, g, b), Color.Transparent);
        }
        catch
        {
            return null;
        }
    }
}
