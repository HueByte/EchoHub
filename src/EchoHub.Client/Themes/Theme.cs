namespace EchoHub.Client.Themes;

public class Theme
{
    public required string Name { get; set; }
    public ThemeColors Base { get; set; } = new();
    public ThemeColors Menu { get; set; } = new();
    public ThemeColors Dialog { get; set; } = new();
    public ThemeColors Status { get; set; } = new();
}

public class ThemeColors
{
    public string Foreground { get; set; } = "White";
    public string Background { get; set; } = "Black";
    public string FocusForeground { get; set; } = "White";
    public string FocusBackground { get; set; } = "Blue";
}
