using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

/// <summary>
/// The MOTD-style splash rendered into the chat pane when no channel is selected —
/// a gold-gradient ASCII logo with version and key hints, in the spirit of classic
/// IRC client greetings.
/// </summary>
internal static class WelcomeBanner
{
    // "ECHOHUB" in FIGlet ANSI-Shadow (58 columns)
    private static readonly string[] BigLogo =
    [
        "███████╗ ██████╗██╗  ██╗ ██████╗ ██╗  ██╗██╗   ██╗██████╗ ",
        "██╔════╝██╔════╝██║  ██║██╔═══██╗██║  ██║██║   ██║██╔══██╗",
        "█████╗  ██║     ███████║██║   ██║███████║██║   ██║██████╔╝",
        "██╔══╝  ██║     ██╔══██║██║   ██║██╔══██║██║   ██║██╔══██╗",
        "███████╗╚██████╗██║  ██║╚██████╔╝██║  ██║╚██████╔╝██████╔╝",
        "╚══════╝ ╚═════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ",
    ];

    // Compact box-drawing fallback for narrow panes (21 columns)
    private static readonly string[] SmallLogo =
    [
        "┌─┐┌─┐┬ ┬┌─┐┬ ┬┬ ┬┌┐ ",
        "├┤ │  ├─┤│ │├─┤│ │├┴┐",
        "└─┘└─┘┴ ┴└─┘┴ ┴└─┘└─┘",
    ];

    // Vertical gold gradient, bright at the top fading to bronze — matches the
    // EchoHub brand color used in the status bar.
    private static readonly Color[] Gradient =
    [
        new(255, 215, 105),
        new(245, 199, 89),
        new(232, 183, 74),
        new(216, 165, 60),
        new(198, 146, 48),
        new(178, 128, 38),
    ];

    private static readonly Attribute HintKeyAttr = new(new Color(140, 170, 200), Color.None);
    private static readonly Attribute HintTextAttr = new(new Color(120, 120, 120), Color.None);
    private static readonly Attribute TaglineAttr = new(new Color(160, 160, 160), Color.None);

    private static readonly (string Key, string Hint)[] Hints =
    [
        ("Server → Connect", "join a server"),
        ("Ctrl+K          ", "search channels & users"),
        ("F2              ", "toggle the users panel"),
        ("/help           ", "all commands"),
    ];

    /// <summary>
    /// Builds banner lines centered for the given viewport width.
    /// </summary>
    public static List<ChatLine> Build(int width, string version)
    {
        var logo = width >= BigLogo[0].GetColumns() + 2 ? BigLogo : SmallLogo;
        int logoWidth = logo[0].GetColumns();
        var pad = new string(' ', Math.Max((width - logoWidth) / 2, 0));

        var lines = new List<ChatLine> { new(""), new("") };

        for (int i = 0; i < logo.Length; i++)
        {
            // Scale the gradient across however many rows the chosen logo has
            var color = Gradient[Math.Min(i * Gradient.Length / logo.Length, Gradient.Length - 1)];
            lines.Add(new ChatLine([
                new ChatSegment(pad, null),
                new ChatSegment(logo[i], new Attribute(color, Color.None)),
            ]));
        }

        lines.Add(new ChatLine(""));

        var tagline = $"v{version} — terminal chat with that old IRC soul";
        lines.Add(Centered(tagline, width, TaglineAttr));
        lines.Add(new ChatLine(""));

        int hintWidth = Hints.Max(h => h.Key.Length + 2 + h.Hint.Length);
        var hintPad = new string(' ', Math.Max((width - hintWidth) / 2, 0));
        foreach (var (key, hint) in Hints)
        {
            lines.Add(new ChatLine([
                new ChatSegment(hintPad, null),
                new ChatSegment(key, HintKeyAttr),
                new ChatSegment("  " + hint, HintTextAttr),
            ]));
        }

        return lines;
    }

    private static ChatLine Centered(string text, int width, Attribute attr)
    {
        var pad = new string(' ', Math.Max((width - text.GetColumns()) / 2, 0));
        return new ChatLine([new ChatSegment(pad, null), new ChatSegment(text, attr)]);
    }
}
