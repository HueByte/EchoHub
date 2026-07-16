namespace EchoHub.Client.Themes;

public class Theme
{
    public required string Name { get; set; }
    public ThemeColors Base { get; set; } = new();
    public ThemeColors Menu { get; set; } = new();
    public ThemeColors Dialog { get; set; } = new();
    public ThemeColors Status { get; set; } = new();

    /// <summary>
    /// Colors for the main-window frame borders (and their titles). Null falls back
    /// to <see cref="Base"/>. Lets themes tone borders down independently of text —
    /// e.g. the transparent themes use a dim gray for a subtler, glassy look.
    /// Supports hex values ("#6E6E6E") as well as named colors.
    /// </summary>
    public ThemeColors? Border { get; set; }
}

public class ThemeColors
{
    public string Foreground { get; set; } = "White";
    public string Background { get; set; } = "Black";
    public string FocusForeground { get; set; } = "White";
    public string FocusBackground { get; set; } = "Blue";
}
