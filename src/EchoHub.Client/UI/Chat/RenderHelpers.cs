using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.Views;

namespace EchoHub.Client.UI.Chat;

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
