using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Helpers;

/// <summary>
/// Helper to parse hex colors to Terminal.Gui Attributes.
/// </summary>
public static class HexColorHelper
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
            return new Attribute(new Color(r, g, b), Color.None);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse a hex color string (with or without #) to a Terminal.Gui Color.
    /// Returns <paramref name="fallback"/> if parsing fails.
    /// </summary>
    public static Color ParseHexToColor(string? hex, Color fallback = default)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        var trimmed = hex.TrimStart('#');
        if (trimmed.Length != 6)
            return fallback;

        try
        {
            var r = Convert.ToInt32(trimmed[..2], 16);
            var g = Convert.ToInt32(trimmed[2..4], 16);
            var b = Convert.ToInt32(trimmed[4..6], 16);
            return new Color(r, g, b);
        }
        catch
        {
            return fallback;
        }
    }
}
