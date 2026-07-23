# ThemeManager.cs

> **Source:** `src/EchoHub.Client/Themes/ThemeManager.cs`

## Contents

- [ThemeManager](#thememanager)
  - [ApplyTheme](#applytheme)
  - [BuildColorScheme](#buildcolorscheme)
  - [GetAvailableThemes](#getavailablethemes)
  - [GetTheme](#gettheme)
  - [ParseColor](#parsecolor)
  - [SaveTheme](#savetheme)
  - [BuiltInThemes](#builtinthemes)
  - [ClassicTheme](#classictheme)
  - [DefaultTheme](#defaulttheme)
  - [DraculaTheme](#draculatheme)
  - [GruvboxTheme](#gruvboxtheme)
  - [HackerTheme](#hackertheme)
  - [HighContrastTheme](#highcontrasttheme)
  - [JsonOptions](#jsonoptions)
  - [LightTheme](#lighttheme)
  - [MonokaiTheme](#monokaitheme)
  - [NordTheme](#nordtheme)
  - [OceanTheme](#oceantheme)
  - [RosePineTheme](#rosepinetheme)
  - [SolarizedTheme](#solarizedtheme)
  - [ThemeDir](#themedir)
  - [TransparentLightTheme](#transparentlighttheme)
  - [TransparentTheme](#transparenttheme)

---

## ThemeManager
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** class

```csharp
public static class ThemeManager
```


ThemeManager is a static helper that centralizes theming for the client UI. It defines built-in themes, reads user-defined themes from the user's theme directory, and exposes methods to enumerate available themes, fetch a theme by name, apply a theme at runtime, and persist theme definitions to disk. Developers reach for it when they need to present theme choices to users, switch the active look, or save a customized theme for future sessions.

## Remarks

Theme definitions live as [`Theme`](Theme.cs.md) instances inside the manager, with a fixed set of built-ins (e.g. `DefaultTheme`, `TransparentTheme`, `TransparentLightTheme`, `ClassicTheme`, `LightTheme`, `HackerTheme`, `SolarizedTheme`, `DraculaTheme`, `MonokaiTheme`, `NordTheme`, `GruvboxTheme`, `OceanTheme`, `HighContrastTheme`, `RosePineTheme`) and a mechanism to discover additional user themes from the directory located at `ThemeDir`. `GetAvailableThemes()` merges these sources while skipping duplicates by name and ignoring malformed theme files; if the theme directory cannot be read, it gracefully falls back to the built-ins. The color wiring happens in `BuildColorScheme(ThemeColors colors)` to ensure the editor surfaces—such as `TextView` and `TextField`—are pinned to the theme’s colors so transparency is preserved (e.g. transparent themes do not render an opaque input background). `ApplyTheme(Theme theme)` applies the chosen look to UI chrome like frame borders and titles, while `SaveTheme(Theme theme)` persists changes to disk as a best-effort operation.

## Notes

- Reading themes from disk is guarded with a fallback to built-ins; IO failures result in a safe degradation rather than a crash.
- Saving themes is a best-effort operation and may fail silently to avoid impacting startup or runtime stability.
- Color parsing relies on `ParseColor(string colorName)`; ensure color names in themes map to known colors to avoid rendering surprises.


---

### ApplyTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
public static void ApplyTheme(Theme theme)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `theme` | [`Theme`](Theme.cs.md) | — |

**Returns:** `void`


ApplyTheme translates a [`Theme`](Theme.cs.md) into runtime color schemes and registers them with the central scheme registry (`SchemeManager`). For each area (`Base`, `Menu`, `Dialog`) it calls `BuildColorScheme` and registers the result via `SchemeManager.AddScheme`. The `Border` area is populated as well, using `theme.Border` when provided or falling back to `theme.Base` when it is not, ensuring frame decorations always have a defined appearance.

## Remarks

By encapsulating the mapping from a [`Theme`](Theme.cs.md) to per-area color schemes, `ApplyTheme` centralizes theming logic and reduces boilerplate across the UI. It also encodes the intended fallback for borders: if a `Border` scheme isn't specified, the `Base` scheme is reused so borders and title bars stay consistent with the rest of the theme.

## Notes

- If `theme.Base` is null and no explicit `theme.Border` is provided, `BuildColorScheme` will receive null, which could lead to an exception at runtime. Ensure `theme.Base` is non-null when a border theme isn't supplied.

---

### BuildColorScheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
private static Scheme BuildColorScheme(ThemeColors colors)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `colors` | [`ThemeColors`](Theme.cs.md) | — |

**Returns:** `Scheme`


BuildColorScheme is an internal helper that converts a [`ThemeColors`](Theme.cs.md) instance into a complete `Scheme` by translating the theme's foreground/background for normal and focused states into two `Attribute`s and applying them across the scheme's state properties (`Normal`, `Focus`, `HotNormal`, `HotFocus`, `Disabled`, `Editable`, `ReadOnly`). It ensures the editable areas reflect the same colors as the surrounding background, which matters for transparent themes.

## Remarks
Conceptually, this centralizes the translation from [`ThemeColors`](Theme.cs.md) to a `Scheme`, guaranteeing consistent color usage across `Normal`/`Focus` and their hot variants. By reusing the same color attributes for `Normal`, `Disabled`, and the editable states, it reduces drift when themes change and keeps UI elements visually cohesive. The inline comment explains the rationale: binding `Editable` and `ReadOnly` to the theme's `Normal` colors ensures the input areas don't render an opaque box behind transparent themes.

## Notes
- Disabled uses the same color as `Normal`; if you need a distinct disabled appearance, this method would need to be extended.
- Editable and ReadOnly are pinned to `Normal` to preserve background transparency; changing this could cause mismatches with the theme's background in transparent themes.

---

### GetAvailableThemes
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
public static List<Theme> GetAvailableThemes()
```

**Returns:** `List<Theme>`


GetAvailableThemes collects the available themes by starting with the built-in set (`BuiltInThemes`), then augmenting it with user-provided themes discovered as JSON files in `ThemeDir`. It reads each `*.json` file, deserializes the content into a [`Theme`](Theme.cs.md) using `JsonSerializer` with `JsonOptions`, and, if the resulting theme has a non-empty `Name` and isn't already present (checked by name using `StringComparison.OrdinalIgnoreCase`), adds it to the list. If the theme directory can't be read or a file is malformed, those items are skipped and the method returns the built-in themes as a fallback. The result is a `List<Theme>` that callers can present to the user.

## Remarks
The `GetAvailableThemes` abstraction centralizes theme discovery, ensuring that built-in themes serve as a baseline while allowing runtime customization through JSON files in `ThemeDir`. It performs simple de-duplication by `Theme.Name` in a case-insensitive manner, so user-provided themes do not create duplicates of built-ins. The design favors resilience: IO or deserialization failures are swallowed so startup remains stable, and valid themes are still returned. This function depends on the shape of the [`Theme`](Theme.cs.md) model (e.g., `Name`, `Base`/`Menu`/`Dialog` color sets) to render themes in the UI.

## Example
```csharp
var themes = ThemeManager.GetAvailableThemes();
foreach (var t in themes)
{
    Console.WriteLine(t.Name);
}
```

## Notes
- IO or JSON parsing errors for individual files are ignored; only valid themes are included in the result.
- If `ThemeDir` does not exist or cannot be read, the method falls back to returning only the built-in themes.
- A runtime-provided theme with a name equal (ignoring case) to an existing built-in theme will be skipped to avoid duplicates.

---

### GetTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
public static Theme GetTheme(string name)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `name` | `string` | — |

**Returns:** [`Theme`](Theme.cs.md)


Returns the [`Theme`](Theme.cs.md) whose `Name` matches the provided `name` using a case-insensitive comparison (`StringComparison.OrdinalIgnoreCase`), sourcing candidates from `GetAvailableThemes()`. If no match is found, it returns `DefaultTheme` as a safe fallback. This encapsulates the pattern of resolving a theme by name and protects callers from handling nulls or missing themes themselves.

---

### ParseColor
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
private static Color ParseColor(string colorName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `colorName` | `string` | — |

**Returns:** `Color`


Parses a color name into a `Color` value by delegating to `Color.TryParse`. If the parse succeeds, it returns the resulting color (or `Color.White` if the parsed value is null). If parsing fails, it falls back to `Color.White`. This provides a safe, centralized way to convert string-based color specifications (for example, theme or config values) into a concrete `Color` without forcing callers to handle parsing errors themselves.

## Remarks
This method encapsulates the color-name resolution logic so the rest of the theming code does not need to repeat `TryParse` calls or null checks. It guarantees a non-null `Color` return value by defaulting to `Color.White`, thereby defining a system-wide fallback policy for theme colors. Being a private helper, it represents an internal implementation detail of the theme system rather than a public API, which keeps the surface area clean for consumers.

## Notes
- Invalid or unrecognized color names map to `Color.White`, which can mask configuration errors; consider validating color names if distinguishing between an explicit white and a default fallback is important.
- If `colorName` is null or empty, the method still returns `Color.White` via the parse/fallback path, ensuring callers always receive a concrete `Color` without exceptions.

---

### SaveTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
public static void SaveTheme(Theme theme)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `theme` | [`Theme`](Theme.cs.md) | — |

**Returns:** `void`


Saves a [`Theme`](Theme.cs.md) to disk as a JSON file under `ThemeDir`. It ensures `ThemeDir` exists, constructs the file path using the theme's name (the value of `theme.Name`) with a `.json` extension, serializes the [`Theme`](Theme.cs.md) with `JsonSerializer` using `JsonOptions`, and writes the resulting JSON to disk. Any exceptions are swallowed, making this a best-effort persistence rather than a guaranteed save.

## Remarks
SaveTheme encapsulates the simple, best-effort persistence strategy for user-defined themes and deliberately avoids propagating IO errors to callers. It is safe to call during normal operation without risking user-facing crashes, but callers should not rely on this method to succeed every time. Because the file name is derived from `theme.Name`, unmapped or invalid characters in names can cause a write to fail silently.

## Notes
- The empty catch means failures won't surface to the caller; consider validating `theme.Name` to ensure a valid file name before invoking this method.
- Writes are synchronous and will overwrite an existing file named after the theme.

---

### BuiltInThemes
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly List<Theme> BuiltInThemes =
    [
        DefaultTheme,
        TransparentTheme,
        TransparentLightTheme,
        ClassicTheme,
        LightTheme,
        HackerTheme,
        SolarizedTheme,
        DraculaTheme,
        MonokaiTheme,
        NordTheme,
        GruvboxTheme,
        OceanTheme,
        HighContrastTheme,
        RosePineTheme
    ]
```


BuiltInThemes is a private static readonly collection of [`Theme`](Theme.cs.md) instances that enumerates the built-in themes shipped with the client. It is initialized with a predefined sequence of themes: `DefaultTheme`, `TransparentTheme`, `TransparentLightTheme`, `ClassicTheme`, `LightTheme`, `HackerTheme`, `SolarizedTheme`, `DraculaTheme`, `MonokaiTheme`, `NordTheme`, `GruvboxTheme`, `OceanTheme`, `HighContrastTheme`, and `RosePineTheme`, and is used internally by the theming subsystem to provide a centralized source of available themes without constructing them at runtime.

## Remarks
This private, static collection centralizes the built-in theme catalog used by the theming system. The `readonly` modifier prevents reassigning the field, but the underlying `List<Theme>` can still be mutated by internal code, which means changes to the set of built-ins could affect any UI that relies on them. If true immutability is required, consider exposing a read-only wrapper or a dedicated API surface.

## Notes
- The `List<Theme>` is mutable even though the field is `readonly`; external code cannot access it, but internal code can modify its contents. If you need to guarantee immutability, replace with a read-only wrapper such as `ReadOnlyCollection<Theme>` and expose a safe accessor.

---

### ClassicTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme ClassicTheme = new()
```


ClassicTheme is a privately scoped, statically initialized [`Theme`](Theme.cs.md) instance that serves as the built-in look-and-feel blueprint used by the UI. It defines color mappings for the `Base`, `Menu`, `Dialog`, and `Status` surfaces, establishing a cohesive appearance across the application. Because it is declared as `private static readonly`, the instance is created once during type initialization and is shared for the lifetime of the process, acting as a default theme reference for the `ThemeManager`.

## Remarks
By centralizing the palette in a single, private field, the `ThemeManager` can apply a consistent Classic style across all major surfaces without requiring external configuration. The private visibility keeps the default theme encapsulated within the theming code, making it straightforward to introduce additional themes or swap them by adding alternative static fields or exposing a configuration mechanism in the future.

---

### DefaultTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme DefaultTheme = new()
```


Represents the canonical default color theme used by the theming subsystem. As a private static readonly [`Theme`](Theme.cs.md) named `Default`, it seeds the color configuration for core surfaces (`Base`, `Menu`, `Dialog`, `Status`) so the UI maintains a consistent palette when no user-provided theme is supplied.

## Remarks
This value acts as the internal seed for all theming operations within the `ThemeManager`. Centralizing the default colors in a single `DefaultTheme` instance ensures consistent visuals across surfaces and avoids duplicating color choices. Note that while the field is `readonly`, its nested [`ThemeColors`](Theme.cs.md) objects may still be mutable at runtime, depending on their mutability; consuming code should not rely on deep immutability unless enforced by the type definitions. The arrangement guarantees uniform behavior for the `Base`, `Menu`, `Dialog`, and `Status` color states (foreground, background, and focus states).

## Notes
- Although the field is `readonly` at the top level, the nested [`ThemeColors`](Theme.cs.md) instances may be mutated; treat this as a potential mutation point.


---

### DraculaTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme DraculaTheme = new()
```


DraculaTheme is a preconfigured [`Theme`](Theme.cs.md) instance that encodes the Dracula color palette for the UI. Declared as a private static readonly field named `DraculaTheme`, it defines a single, shared palette used by the application to color the core surfaces — `Base`, `Menu`, `Dialog`, and `Status` — with per-surface mappings such as foregrounds, backgrounds, and focus colors that collectively establish a cohesive, dark interface with magenta accents on focus. With `Name` set to Dracula, this theme provides a consistent Dracula aesthetic across the application.

## Remarks
Centralizes the Dracula color choices in one place to ensure visual consistency across surfaces and to simplify theme swapping by the `ThemeManager` without recalculating colors at render time. The per-surface [`ThemeColors`](Theme.cs.md) definitions govern how content appears on the main areas (`Base`), the navigation (`Menu`), popups (`Dialog`), and status indicators (`Status`).

## Notes
- The nested [`ThemeColors`](Theme.cs.md) objects may be mutable; treat DraculaTheme as effectively immutable only if those types are immutable, or clone before modification if variations are needed.
- Because the field is private, external code cannot reference it directly; expose an accessor or copy if you need to reuse this theme outside its containing class.

---

### GruvboxTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme GruvboxTheme = new()
```


GruvboxTheme is a private static readonly [`Theme`](Theme.cs.md) that defines the Gruvbox color palette used by the UI. It initializes `Name` to "Gruvbox" and provides color configurations for the core UI regions via `Base`, `Menu`, `Dialog`, and `Status`, each specifying `Foreground`, `Background`, `FocusForeground`, and `FocusBackground` values.

## Remarks
GruvboxTheme serves as a single source of truth for the Gruvbox palette, making it easy to apply the same colors across `Base`, `Menu`, `Dialog`, and `Status` without duplicating literals elsewhere. Because the field is `static` and `readonly`, the palette is established once during type initialization and cannot be mutated at runtime, ensuring a consistent theme until a deliberate change is made in code. External code relies on the public theming surface to apply the Gruvbox palette; GruvboxTheme itself remains a private, immutable foundation for that surface.

## Notes
- Private field scope means external code cannot reference `GruvboxTheme` directly; use the public theming API (e.g., `ThemeManager`) to switch or retrieve themes.

---

### HackerTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme HackerTheme = new()
```


HackerTheme is a private static readonly instance of [`Theme`](Theme.cs.md) that defines the 'Hacker' color scheme used by the UI. It centralizes the color configuration for the core regions—`Base`, `Menu`, `Dialog`, and `Status`—by specifying `Foreground`, `Background`, `FocusForeground`, and `FocusBackground` to deliver a cohesive hacker aesthetic across the interface, and is reused internally rather than rebuilt for each component.

## Remarks
By housing the entire color palette in a single static field, the code ensures visual consistency across all UI surfaces that adopt this theme. The `HackerTheme` instance is created once at class initialization and referenced wherever a [`Theme`](Theme.cs.md) is needed within the theme system, promoting reuse and reducing the risk of divergent color values. Keeping this configuration private reinforces encapsulation: external code cannot mutate the theme inadvertently, preserving the intended appearance.

---

### HighContrastTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme HighContrastTheme = new()
```


Defines a preconfigured [`Theme`](Theme.cs.md) instance named `HighContrast` that drives a high-contrast UI palette. It is exposed internally as a private static readonly field `HighContrastTheme` and initializes the `Base`, `Menu`, `Dialog`, and `Status` surfaces with a dark background (`Black`) and bright foreground (`BrightYellow`), while tuning region-specific focus colors to preserve legibility. Because it is static and readonly, the theme is constructed once and reused by the UI theming system rather than rebuilt at runtime.

## Remarks
This field acts as a canonical, immutable high-contrast palette for the theming subsystem. By centralizing the color choices for `Base`, `Menu`, `Dialog`, and `Status`, it ensures consistent accessibility-friendly visuals across the application and prevents drift between components. Its private visibility indicates it is an internal implementation detail of the theme infrastructure, intended to be consumed by the theme-management logic rather than by consumer code directly.

## Notes
- The `HighContrastTheme` is immutable after initialization due to `readonly`; runtime theme switching would require a separate mechanism to swap themes. 


---

### JsonOptions
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
```


Defines a shared `JsonSerializerOptions` instance named `JsonOptions` used by the `ThemeManager` to serialize theme data with consistent formatting. It configures pretty-printed JSON by setting `WriteIndented` to true and enforces camelCase property names by using `PropertyNamingPolicy` via `JsonNamingPolicy.CamelCase`.

## Remarks
By making the field `static` and `readonly`, the class ensures a single, immutable source of serialization configuration for all calls within the ThemeManager, reducing duplication and the risk of inconsistent formatting. This centralization also minimizes drift if multiple serialization sites exist in the class.

## Notes
- Do not mutate `JsonOptions` after initialization; although `JsonSerializerOptions` properties are mutable, the field is intended to be consumed as a fixed configuration.
- If a one-off operation requires a different formatting (e.g., a different naming policy or indentation), create and use a separate `JsonSerializerOptions` instance instead of modifying this field.

---

### LightTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme LightTheme = new()
```


The `LightTheme` field provides a concrete, immutable light color scheme used by the theming system. It centralizes color definitions for the main UI surfaces: `Base`, the `Menu`, `Dialog`, and `Status` areas, ensuring consistent foreground/background combinations across the application and predictable focus states.

With `Name` set to `Light` and color pairs like `Foreground`/`Background` and `FocusForeground`/`FocusBackground` defined per surface, it enables the ThemeManager to apply the light theme quickly without reconstructing the palette each time.

## Remarks

By keeping the field `private static readonly`, the code guarantees a single, shared instance of the light theme that cannot be modified at runtime, avoiding drift between components. This centralization also clarifies the intended visual identity for the light mode and reduces duplication whenever a light theme is needed.

---

### MonokaiTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme MonokaiTheme = new()
```


MonokaiTheme is a private static readonly field that encapsulates the internal Monokai color palette used by the UI. It defines a single [`Theme`](Theme.cs.md) named `Monokai` with dedicated [`ThemeColors`](Theme.cs.md) for `Base`, `Menu`, `Dialog`, and `Status`, specifying `Foreground`, `Background`, `FocusForeground`, and `FocusBackground` to ensure the interface presents a cohesive look.

## Remarks
MonokaiTheme centralizes the Monokai palette for the UI, providing a single source of truth for the [`Theme`](Theme.cs.md) the `ThemeManager` applies across components. Its private static readonly scope ensures a stable, class-wide instance isn't exposed or replaced by external code, preserving the intended appearance. If internal code mutates the nested [`ThemeColors`](Theme.cs.md) objects, the look could drift, so treat the instance as effectively immutable after initialization.

## Notes
- `readonly` prevents reassigning the field, but nested color objects may still be mutated; ensure internal code avoids mutating the theme after initialization or consider making the color data immutable.

---

### NordTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme NordTheme = new()
```


A private static readonly [`Theme`](Theme.cs.md) named `NordTheme` encodes the Nord color palette for the UI. It initializes `Base`, `Menu`, `Dialog`, and `Status` color schemes with explicit foreground and background values, serving as an immutable, centralized Nord appearance that the theme system can apply when Nord is active.

## Remarks

NordTheme acts as a self-contained Nord theme preset, isolating color mappings for core UI regions. Because it is `static` and `readonly`, the palette is stabilized at startup, ensuring consistent visuals across the app when Nord is selected. Each region (`Base`, `Menu`, `Dialog`, `Status`) groups foreground/background pairs, making future tweaks localized to this single field.

## Notes

- Since `NordTheme` is `private`, external code cannot reference it directly; if runtime theme switching is needed, introduce a public API or factory to expose a Nord palette.

---

### OceanTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme OceanTheme = new()
```


The private static readonly field `OceanTheme` is a [`Theme`](Theme.cs.md) instance configured with a named palette Ocean and dedicated [`ThemeColors`](Theme.cs.md) for its `Base`, `Menu`, `Dialog`, and `Status` sections. It is initialized inline with specific color tokens such as `BrightCyan`, `DarkBlue`, and `DarkCyan` to ensure a cohesive, visually distinct look across the UI. Being `static readonly` means this instance is created once at type initialization and cannot be reassigned, serving as an internal, consistent theme blueprint for the `ThemeManager`.

## Remarks

This field encapsulates a concrete theme configuration that `ThemeManager` uses internally, without exposing mutable defaults to consumers. Centralizing color mappings for `Base`, `Menu`, `Dialog`, and `Status` in a single private field reduces duplication and promotes visual consistency across the UI. Because the field is private, external code cannot reference or alter it directly; changes must go through the public theming API, preserving encapsulation.

---

### RosePineTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme RosePineTheme = new()
```


RosePineTheme is a private static readonly [`Theme`](Theme.cs.md) instance named `RosePine` that encodes a RosePine color palette for the UI. It defines color roles for `Base`, `Menu`, `Dialog`, and `Status` via nested [`ThemeColors`](Theme.cs.md) objects, specifying `Foreground`, `Background`, `FocusForeground`, and `FocusBackground` values. This single, prebuilt object lets the rest of the UI apply a cohesive RosePine appearance without reconstructing a [`Theme`](Theme.cs.md) from scratch.

## Remarks
Centralizes the RosePine aesthetic in one place, ensuring consistent color usage across the core chrome (`Base`, `Menu`, `Dialog`, `Status`). As a private static field, it is intended for internal composition by the theme system, reducing boilerplate when constructing themes at runtime. If you need to expose it externally, you would typically wrap or copy it behind a public API.

## Notes
- Although the field is `readonly`, the nested [`ThemeColors`](Theme.cs.md) instances may still be mutable if their properties have setters. Treat the object as immutable; avoid mutating to preserve a consistent RosePine theme.
- The field is private, so external consumers cannot reference `RosePineTheme` directly; changes to the theme would require a public accessor or method in `ThemeManager`.

---

### SolarizedTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme SolarizedTheme = new()
```


This field defines the pre-defined Solarized color theme as a private, static, readonly [`Theme`](Theme.cs.md) instance named `SolarizedTheme`. It bundles color roles for the base chrome, menus, dialogs, and status areas, providing a centralized Solarized palette that the theming subsystem can apply to the UI. The `private static readonly` designation ensures a single, immutable instance is created at startup, guaranteeing consistent visuals across the application.

## Remarks
Having a single [`Theme`](Theme.cs.md) instance for Solarized encapsulates the palette in one place, reducing duplication of color literals across UI surfaces. By separating the colors into `Base`, `Menu`, `Dialog`, and `Status` groups, the theme clearly communicates how each UI surface should appear and simplifies future tweaks. This private field serves as an internal canonical source for the Solarized look within the codebase and is consumed by the theming pipeline without exposing implementation details publicly.

---

### ThemeDir
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly string ThemeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".echohub", "themes")
```


ThemeDir stores the path to the per-user themes directory for the EchoHub client. It is initialized once at type initialization by combining the user's home directory (obtained via `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)`) with the relative path `".echohub/themes"`, yielding a stable, user-scoped base for reading or enumerating theme assets.

## Remarks
- By centralizing the path construction, this private static readonly field reduces duplication and ensures all theme IO uses the same base directory.
- It encodes the assumption that themes are stored under the user's profile, which keeps user-specific customization isolated from system-wide resources.
- The static readonly nature means the value is fixed after initialization, simplifying reasoning about its value and caching theme metadata.

## Notes
- If the environment lacks a user profile directory, `Environment.GetFolderPath` may return an empty string, which would yield an invalid `ThemeDir`. Calling code should validate the path before attempting IO.
- It is a private field; external code cannot rely on this path and must use public APIs provided by the class for theme access.

---

### TransparentLightTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme TransparentLightTheme = new()
```


Defines a concrete [`Theme`](Theme.cs.md) named `TransparentLight` with per-surface color rules for `Base`, `Menu`, `Dialog`, `Status`, and `Border` via [`ThemeColors`](Theme.cs.md). Each surface is configured with `Foreground`, `Background`, and `FocusForeground`/`FocusBackground` values to yield a light, nearly transparent appearance on the host UI: most surfaces use `Background = "None"`, while `Dialog` uses a light gray background and blue focus accents. This field is `private static readonly`, initialized once and used internally by the theming system to provide the `TransparentLight` theme.

## Remarks
By centralizing the color definitions for a light, semi-transparent appearance, this field enables consistent theming across the UI without scattering color literals throughout the code. Because it is `private`, external code cannot directly reference it; the surrounding theme infrastructure can expose higher-level theme switching that pulls from this internal variant. The immutable reference helps ensure the theme is not accidentally replaced at runtime, though the nested [`ThemeColors`](Theme.cs.md) instances may still be mutated if their properties are writable.

## Notes
- The `readonly` modifier prevents reassignment of the field, but the nested [`ThemeColors`](Theme.cs.md) objects could still be mutated if their properties have setters; avoid mutating them at runtime to preserve theme consistency.


---

### TransparentTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme TransparentTheme = new()
```


TransparentTheme is a private, static readonly instance of [`Theme`](Theme.cs.md) that encodes the glassy, transparent UI aesthetic named 'Transparent' and is intended for internal use by the theming system rather than as a public theme. It defines color settings for `Base`, `Menu`, `Dialog`, `Status`, and `Border` to deliver a cohesive appearance, with muted `Border` colors to preserve the translucent look.

## Remarks
TransparentTheme centralizes the palette for the glassy style in a single immutable object, reducing duplication across components. As a private field, it serves as an internal predefined palette that the theming system can apply without exposing a public API. This encapsulation makes it easy to tweak the look in one place while keeping the public surface stable.

---