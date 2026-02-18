using System.Text.Json;
using Terminal.Gui.Drawing;
using Terminal.Gui.Configuration;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.Themes;

public static class ThemeManager
{
    private static readonly string ThemeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".echohub", "themes");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Theme DefaultTheme = new()
    {
        Name = "Default",
        Base = new ThemeColors
        {
            Foreground = "Gray",
            Background = "Black",
            FocusForeground = "White",
            FocusBackground = "DarkGray"
        },
        Menu = new ThemeColors
        {
            Foreground = "Gray",
            Background = "Black",
            FocusForeground = "White",
            FocusBackground = "DarkGray"
        },
        Dialog = new ThemeColors
        {
            Foreground = "White",
            Background = "DarkGray",
            FocusForeground = "Black",
            FocusBackground = "Gray"
        },
        Status = new ThemeColors
        {
            Foreground = "White",
            Background = "DarkGray",
            FocusForeground = "White",
            FocusBackground = "DarkGray"
        }
    };

    private static readonly Theme ClassicTheme = new()
    {
        Name = "Classic",
        Base = new ThemeColors
        {
            Foreground = "White",
            Background = "Blue",
            FocusForeground = "Black",
            FocusBackground = "Cyan"
        },
        Menu = new ThemeColors
        {
            Foreground = "White",
            Background = "DarkGray",
            FocusForeground = "White",
            FocusBackground = "Black"
        },
        Dialog = new ThemeColors
        {
            Foreground = "White",
            Background = "DarkGray",
            FocusForeground = "Black",
            FocusBackground = "Cyan"
        },
        Status = new ThemeColors
        {
            Foreground = "White",
            Background = "DarkGray",
            FocusForeground = "White",
            FocusBackground = "DarkGray"
        }
    };

    private static readonly Theme LightTheme = new()
    {
        Name = "Light",
        Base = new ThemeColors
        {
            Foreground = "Black",
            Background = "White",
            FocusForeground = "White",
            FocusBackground = "Blue"
        },
        Menu = new ThemeColors
        {
            Foreground = "White",
            Background = "Blue",
            FocusForeground = "BrightYellow",
            FocusBackground = "Blue"
        },
        Dialog = new ThemeColors
        {
            Foreground = "Black",
            Background = "White",
            FocusForeground = "White",
            FocusBackground = "Blue"
        },
        Status = new ThemeColors
        {
            Foreground = "White",
            Background = "Blue",
            FocusForeground = "White",
            FocusBackground = "Blue"
        }
    };

    private static readonly Theme HackerTheme = new()
    {
        Name = "Hacker",
        Base = new ThemeColors
        {
            Foreground = "BrightGreen",
            Background = "Black",
            FocusForeground = "Black",
            FocusBackground = "Green"
        },
        Menu = new ThemeColors
        {
            Foreground = "BrightGreen",
            Background = "Black",
            FocusForeground = "Black",
            FocusBackground = "BrightGreen"
        },
        Dialog = new ThemeColors
        {
            Foreground = "BrightGreen",
            Background = "Black",
            FocusForeground = "Black",
            FocusBackground = "Green"
        },
        Status = new ThemeColors
        {
            Foreground = "BrightGreen",
            Background = "Black",
            FocusForeground = "BrightGreen",
            FocusBackground = "Black"
        }
    };

    private static readonly Theme SolarizedTheme = new()
    {
        Name = "Solarized",
        Base = new ThemeColors
        {
            Foreground = "Cyan",
            Background = "Black",
            FocusForeground = "BrightYellow",
            FocusBackground = "DarkGray"
        },
        Menu = new ThemeColors
        {
            Foreground = "BrightCyan",
            Background = "DarkGray",
            FocusForeground = "BrightYellow",
            FocusBackground = "Black"
        },
        Dialog = new ThemeColors
        {
            Foreground = "Cyan",
            Background = "DarkGray",
            FocusForeground = "BrightYellow",
            FocusBackground = "Black"
        },
        Status = new ThemeColors
        {
            Foreground = "BrightCyan",
            Background = "DarkGray",
            FocusForeground = "BrightCyan",
            FocusBackground = "DarkGray"
        }
    };

    private static readonly Theme TransparentTheme = new()
    {
        Name = "Transparent",
        Base = new ThemeColors
        {
            Foreground = "White",
            Background = "Black",
            FocusForeground = "BrightCyan",
            FocusBackground = "Black"
        },
        Menu = new ThemeColors
        {
            Foreground = "White",
            Background = "Black",
            FocusForeground = "BrightCyan",
            FocusBackground = "Black"
        },
        Dialog = new ThemeColors
        {
            Foreground = "White",
            Background = "Black",
            FocusForeground = "BrightCyan",
            FocusBackground = "Black"
        },
        Status = new ThemeColors
        {
            Foreground = "Gray",
            Background = "Black",
            FocusForeground = "Gray",
            FocusBackground = "Black"
        }
    };

    private static readonly List<Theme> BuiltInThemes =
    [
        DefaultTheme,
        TransparentTheme,
        ClassicTheme,
        LightTheme,
        HackerTheme,
        SolarizedTheme
    ];

    public static List<Theme> GetAvailableThemes()
    {
        var themes = new List<Theme>(BuiltInThemes);

        try
        {
            if (Directory.Exists(ThemeDir))
            {
                foreach (var file in Directory.GetFiles(ThemeDir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var theme = JsonSerializer.Deserialize<Theme>(json, JsonOptions);
                        if (theme is not null && !string.IsNullOrWhiteSpace(theme.Name))
                        {
                            // Skip if a built-in theme already has this name
                            if (!themes.Exists(t => string.Equals(t.Name, theme.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                themes.Add(theme);
                            }
                        }
                    }
                    catch
                    {
                        // Skip malformed theme files
                    }
                }
            }
        }
        catch
        {
            // If we can't read the theme directory, just return built-ins
        }

        return themes;
    }

    public static Theme GetTheme(string name)
    {
        var themes = GetAvailableThemes();
        return themes.Find(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? DefaultTheme;
    }

    public static void ApplyTheme(Theme theme)
    {
        SchemeManager.AddScheme("Base", BuildColorScheme(theme.Base));
        SchemeManager.AddScheme("Menu", BuildColorScheme(theme.Menu));
        SchemeManager.AddScheme("Dialog", BuildColorScheme(theme.Dialog));
    }

    public static void SaveTheme(Theme theme)
    {
        try
        {
            Directory.CreateDirectory(ThemeDir);
            var fileName = $"{theme.Name}.json";
            var filePath = Path.Combine(ThemeDir, fileName);
            var json = JsonSerializer.Serialize(theme, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Silently fail — theme save is best-effort
        }
    }

    private static Scheme BuildColorScheme(ThemeColors colors)
    {
        var normal = new Attribute(ParseColor(colors.Foreground), ParseColor(colors.Background));
        var focus = new Attribute(ParseColor(colors.FocusForeground), ParseColor(colors.FocusBackground));

        return new Scheme
        {
            Normal = normal,
            Focus = focus,
            HotNormal = normal,
            HotFocus = focus,
            Disabled = normal
        };
    }

    private static Color ParseColor(string colorName)
    {
        if (Color.TryParse(colorName, out var color))
            return color ?? Color.White;

        return Color.White;
    }
}
