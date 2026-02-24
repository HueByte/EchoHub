using System.Text.RegularExpressions;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

/// <summary>
/// Shared color attributes for chat rendering (timestamps, system messages).
/// </summary>
public static partial class ChatColors
{
    public static readonly Attribute TimestampAttr = new(Color.DarkGray, Color.None);
    public static readonly Attribute SystemAttr = new(new Color(0, 180, 180), Color.None);
    public static readonly Attribute MentionHighlightAttr = new(Color.White, new Color(80, 40, 0));
    public static readonly Attribute MentionTextAttr = new(new Color(255, 180, 50), Color.None);
    public static readonly Attribute ChannelRefAttr = new(new Color(100, 200, 255), Color.None);
    public static readonly Attribute EmbedBorderAttr = new(new Color(91, 155, 213), Color.None);
    public static readonly Attribute EmbedTitleAttr = new(Color.White, Color.None);
    public static readonly Attribute EmbedDescAttr = new(new Color(160, 160, 160), Color.None);
    public static readonly Attribute EmbedUrlAttr = new(new Color(100, 100, 100), Color.None);
    public static readonly Attribute AudioAttr = new(new Color(180, 100, 255), Color.None);
    public static readonly Attribute FileAttr = new(new Color(100, 180, 255), Color.None);

    /// <summary>
    /// Split text around @mentions and #channels, giving each the appropriate accent color.
    /// Non-special text uses the provided default color.
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

        // Add remaining text after the last mention (or all text if no mentions found)
        if (lastIndex < text.Length)
            segments.Add(new ChatSegment(text[lastIndex..], defaultColor));

        // Second pass: highlight #channels in non-mention segments
        var mentionSegments = segments;
        segments = [];
        foreach (var seg in mentionSegments)
        {
            if (seg.Color != null && seg.Color != defaultColor)
            {
                // Already colored (mention) — keep as-is
                segments.Add(seg);
                continue;
            }

            int segLast = 0;
            foreach (Match match in ChannelRefRegex().Matches(seg.Text))
            {
                if (match.Index > segLast)
                    segments.Add(new ChatSegment(seg.Text[segLast..match.Index], defaultColor));

                segments.Add(new ChatSegment(match.Value, ChannelRefAttr));
                segLast = match.Index + match.Length;
            }

            if (segLast < seg.Text.Length)
                segments.Add(new ChatSegment(seg.Text[segLast..], defaultColor));
        }

        return segments;
    }

    // @mention — not preceded by a word char (avoids emails)
    [GeneratedRegex(@"(?<!\w)@[\w-]+")]
    private static partial Regex MentionRegex();

    // #channel — not preceded by a word char, must contain at least one letter (avoids hex colors / issue numbers)
    [GeneratedRegex(@"(?<!\w)#(?=.*[a-zA-Z])[\w-]+")]
    private static partial Regex ChannelRefRegex();
}
