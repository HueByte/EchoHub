using System.Text.RegularExpressions;
using EchoHub.Core.Models;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

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

        // Propagate attachment/type metadata to all wrapped lines so they remain clickable
        foreach (var wrapped in results)
        {
            wrapped.AttachmentUrl = AttachmentUrl;
            wrapped.AttachmentFileName = AttachmentFileName;
            wrapped.Type = Type;
            wrapped.MessageId = MessageId;
        }

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
        var defaultBg = defaultAttr?.Background ?? Color.None;

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
