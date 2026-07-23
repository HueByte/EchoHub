# Theme.cs

> **Source:** `src/EchoHub.Client/Themes/Theme.cs`

## Contents

- [Theme](#theme)
- [ThemeColors](#themecolors)

---

## Theme
> **File:** `src/EchoHub.Client/Themes/Theme.cs`  
> **Kind:** class

```csharp
public class Theme
```


The `Theme` class encapsulates the color palette used by the UI. It groups per-surface color sets for the main surfaces (`Base`, `Menu`, `Dialog`, `Status`) and exposes an optional `Border` color that can override the window frame independently of text. By providing a name and a complete set of colors, a developer can switch or define visual styles at runtime and apply them to the UI. If you do not need a separate border color, leave `Border` as null to fall back to `Base`.

## Remarks
The `Theme` object acts as a central theme descriptor that isolates surface-specific colors from the core palette, making it easy to create variants (e.g., light, dark, or glassy appearances) without scattering color values through the code. The optional `Border` enables stylistic nuances for window chrome without altering text or control coloring, helping to achieve subtler, themed aesthetics while preserving readability.

## Example
```csharp
var theme = new Theme
{
    Name = "Glass",
    Base = new ThemeColors(), // default color family for surfaces
    Border = null // explicit fallback to Base colors for borders
};
```


---

## ThemeColors
> **File:** `src/EchoHub.Client/Themes/Theme.cs`  
> **Kind:** class

```csharp
public class ThemeColors
```


ThemeColors is a small data container that groups the color tokens used by the UI: `Foreground`, `Background`, `FocusForeground`, and `FocusBackground`. Create and pass a single `ThemeColors` instance to ensure consistent theming across components rather than scattering color literals throughout the code.

## Remarks
By centralizing color choices in `ThemeColors`, the UI can swap themes or provide variations without touching individual controls. The default initializers encode a high-contrast dark theme (white text on black, focus highlight in blue), but you can override any property to tailor a theme for a particular context.

## Notes
- Mutability: the properties have public setters, so the color values can be changed after construction; if a `ThemeColors` instance is shared, mutations will affect all dependents.
- Defaults are defined via property initializers; override them on construction if you want a different baseline.

---