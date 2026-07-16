using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Helpers;

/// <summary>
/// Deterministic per-nick colors for users who haven't picked a nickname color.
/// The same nick always maps to the same palette entry (classic IRC client behavior),
/// so a busy channel stays scannable without any configuration.
/// </summary>
public static class NickColorHelper
{
    // Medium-saturation truecolor values chosen to stay readable on both dark and
    // light backgrounds. Order matters: changing it re-colors everyone.
    private static readonly Attribute[] Palette =
    [
        new(new Color(224, 108, 117), Color.None), // soft red
        new(new Color(152, 195, 121), Color.None), // green
        new(new Color(229, 192, 123), Color.None), // sand
        new(new Color(97, 175, 239), Color.None),  // blue
        new(new Color(198, 120, 221), Color.None), // magenta
        new(new Color(86, 182, 194), Color.None),  // teal
        new(new Color(255, 160, 122), Color.None), // salmon
        new(new Color(130, 170, 255), Color.None), // periwinkle
        new(new Color(195, 232, 141), Color.None), // lime
        new(new Color(137, 221, 255), Color.None), // sky
        new(new Color(255, 203, 107), Color.None), // amber
        new(new Color(240, 130, 170), Color.None), // rose
    ];

    /// <summary>
    /// Stable palette index for a nick: case-insensitive FNV-1a over the nick,
    /// reduced modulo <paramref name="paletteSize"/>. Pure function (no Terminal.Gui
    /// types) so it is unit-testable without a display driver.
    /// </summary>
    public static int GetPaletteIndex(string nick, int paletteSize)
    {
        if (paletteSize <= 0)
            return 0;

        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;

        uint hash = fnvOffset;
        foreach (var ch in nick)
        {
            hash ^= char.ToLowerInvariant(ch);
            hash *= fnvPrime;
        }

        return (int)(hash % (uint)paletteSize);
    }

    /// <summary>
    /// The color attribute for a nick. Used as a fallback when the user has no
    /// explicit nickname color set.
    /// </summary>
    public static Attribute GetAttribute(string nick) =>
        Palette[GetPaletteIndex(nick, Palette.Length)];
}
