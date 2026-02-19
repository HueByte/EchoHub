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
    /// Parse a string containing ANSI 24-bit color escape codes into colored segments.
    /// Format: \x1b[38;2;R;G;Bm (foreground color), \x1b[0m (reset)
    /// </summary>
    public static ChatLine FromAnsi(string ansiText, Attribute? defaultAttr = null)
    {
        var segments = new List<ChatSegment>();
        var regex = AnsiColorRegex();
        int lastIndex = 0;
        Attribute? currentColor = defaultAttr;

        foreach (Match match in regex.Matches(ansiText))
        {
            // Add any text before this escape sequence
            if (match.Index > lastIndex)
            {
                var text = ansiText[lastIndex..match.Index];
                if (text.Length > 0)
                    segments.Add(new ChatSegment(text, currentColor));
            }

            // Parse the escape sequence
            if (match.Groups[1].Value == "0")
            {
                // Reset
                currentColor = defaultAttr;
            }
            else if (match.Groups[2].Success)
            {
                // 38;2;R;G;B — 24-bit foreground color
                var r = int.Parse(match.Groups[3].Value);
                var g = int.Parse(match.Groups[4].Value);
                var b = int.Parse(match.Groups[5].Value);
                currentColor = new Attribute(new Color(r, g, b), Color.Black);
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < ansiText.Length)
        {
            var text = ansiText[lastIndex..];
            if (text.Length > 0)
                segments.Add(new ChatSegment(text, currentColor));
        }

        return segments.Count > 0 ? new ChatLine(segments) : new ChatLine("");
    }

    // Matches: \x1b[0m (reset) or \x1b[38;2;R;G;Bm (24-bit foreground)
    [GeneratedRegex(@"\x1b\[(?:(0)|(?:(38;2);(\d{1,3});(\d{1,3});(\d{1,3})))m")]
    private static partial Regex AnsiColorRegex();
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
        var normalAttr = listView.GetAttributeForRole(selected ? VisualRole.Focus : VisualRole.Normal);

        int charPos = 0;
        int drawnChars = 0;

        foreach (var segment in chatLine.Segments)
        {
            var attr = segment.Color ?? normalAttr;
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

        // Fill remaining width with spaces using default colors
        listView.SetAttribute(normalAttr);
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
