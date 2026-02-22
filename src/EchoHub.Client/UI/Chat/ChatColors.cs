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
    public static readonly Attribute EmbedBorderAttr = new(new Color(91, 155, 213), Color.None);
    public static readonly Attribute EmbedTitleAttr = new(Color.White, Color.None);
    public static readonly Attribute EmbedDescAttr = new(new Color(160, 160, 160), Color.None);
    public static readonly Attribute EmbedUrlAttr = new(new Color(100, 100, 100), Color.None);
    public static readonly Attribute AudioAttr = new(new Color(180, 100, 255), Color.None);
    public static readonly Attribute FileAttr = new(new Color(100, 180, 255), Color.None);

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
