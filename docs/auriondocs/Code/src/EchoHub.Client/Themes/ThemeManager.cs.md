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
  - [ClassicTheme](#classictheme)
  - [DefaultTheme](#defaulttheme)
  - [DraculaTheme](#draculatheme)
  - [HackerTheme](#hackertheme)
  - [HighContrastTheme](#highcontrasttheme)
  - [JsonOptions](#jsonoptions)
  - [LightTheme](#lighttheme)
  - [MonokaiTheme](#monokaitheme)
  - [OceanTheme](#oceantheme)
  - [SolarizedTheme](#solarizedtheme)
  - [ThemeDir](#themedir)
  - [TransparentLightTheme](#transparentlighttheme)
  - [TransparentTheme](#transparenttheme)
- [BuiltInThemes](#builtinthemes)
- [GruvboxTheme](#gruvboxtheme)
- [NordTheme](#nordtheme)
- [RosePineTheme](#rosepinetheme)

---

## ThemeManager
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** class

```csharp
public static class ThemeManager
```


ThemeManager_overview provides a centralized, static API for discovering, loading, applying, and persisting themes used by the EchoHub client UI. Call GetAvailableThemes to enumerate built-in and user-defined themes, GetTheme to fetch a theme by name, and ApplyTheme to switch the UI to a chosen theme.

## Remarks
Conceptually, ThemeManager acts as the bridge between Theme data (the Theme class) and the runtime UI. It maintains a curated list of built-in themes and exposes logic to load additional themes from a user directory, surfacing them for selection without requiring changes to the runtime code. In addition, BuildColorScheme ensures color assignments for text areas align with the active theme, pinning Editable/ReadOnly roles so that transparent themes render correctly and inputs stay legible. This centralizes theming concerns and keeps theme-related behavior in one place, simplifying maintenance and experimentation with new themes.

## Example
```csharp
var available = ThemeManager.GetAvailableThemes();
var theme = ThemeManager.GetTheme("Default");
ThemeManager.ApplyTheme(theme);
```

## Notes
- SaveTheme is best-effort and silently swallows failures; verify persistence if you rely on saved themes.
- GetAvailableThemes falls back to built-in themes when the theme directory cannot be read.
- ParseColor expects valid color identifiers defined by the theming system; supply colors that exist in the library or your Theme colors.

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


Applies a Theme by registering color schemes for the core UI areas with SchemeManager. This single call maps the Theme's Base, Menu, Dialog, and optional Border sections to named schemes so the rest of the UI can render consistently according to the active theme.

## Remarks
This method acts as a bridge between the Theme model and SchemeManager's scheme registry. It delegates color construction to BuildColorScheme for each region, ensuring Base, Menu, and Dialog colors stay in sync. The Border scheme uses theme.Border when provided, otherwise it falls back to the Base palette to preserve a coherent frame. By applying all four schemes in one place, ApplyTheme reduces the risk of components diverging toward inconsistent styling.

## Notes
- Repeatedly calling ApplyTheme overwrites the previously registered schemes, so batch theme updates if you want to avoid intermediate flashes.
- The Border palette falls back to Base when Border is not provided; ensure the Base colors reflect the desired frame in that case.

## Dependencies
- SchemeManager

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


BuildColorScheme converts ThemeColors into a Terminal.Gui Scheme by deriving two Attributes—Normal from Foreground and Background and Focus from FocusForeground and FocusBackground—then applying them to the Scheme's state properties (Normal, Focus, HotNormal, HotFocus, Disabled). It also pins Editable and ReadOnly to Normal to ensure input controls render against the theme background, avoiding opaque boxes in transparent themes.

## Remarks
This method centralizes the theme-to-scheme translation, decoupling ThemeColors from the Scheme used by the UI. By deriving Normal and Focus once and reusing them for all relevant roles, and by tying Editable/ReadOnly to Normal, it guarantees consistent visual behavior for standard controls and editable regions across themes. The method being private static signals that it's an internal detail of the theming pipeline used by ThemeManager to assemble the active color scheme.

## Notes
- If ThemeColors contain invalid color strings, ParseColor may throw; ensure colors are validated before calling BuildColorScheme.
- The returned Scheme is a new object each time; repeated calls may impact allocations.
- Editable and ReadOnly are deliberately mapped to Normal; if you need distinct input backgrounds, adjust the mapping accordingly.

---

### GetAvailableThemes
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** method

```csharp
public static List<Theme> GetAvailableThemes()
```

**Returns:** `List<Theme>`


The GetAvailableThemes method returns a list of Theme objects by starting with the built-in themes and augmenting that set with user-defined themes discovered in the ThemeDir directory. It iterates over all *.json files, deserializes each one into a Theme using JsonSerializer with the configured JsonOptions, and, if the resulting theme has a non-empty Name and does not duplicate an existing theme (case-insensitive comparison on Name), appends it to the collection. If ThemeDir does not exist or any IO or JSON parsing error occurs, the method gracefully falls back to returning only the built-in themes.

## Remarks
This function encapsulates the theme-loading strategy: built-in themes establish the default baseline, while external JSON themes extend the collection without mutating the originals. It operates defensively, skipping malformed files and continuing execution in the face of read errors, which yields a predictable return value even under partial failure. De-duplication is driven by Theme.Name using a case-insensitive comparison to prevent accidental duplicates when names differ only by case.

## Notes
- It swallows IO and JSON parsing exceptions, so failures to read or parse individual files do not propagate to the caller.
- Built-in themes take precedence: a user-defined theme with a Name that matches an existing built-in theme is ignored, ensuring stable baseline behavior.

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


Resolves a Theme by name by searching the collection returned by GetAvailableThemes and returning the first match found when the theme name equals the provided name, ignoring case. It is the right choice when you need to map a user-provided theme name (from UI, config, or input) to a Theme object, with a fallback to DefaultTheme if no match exists.

## Remarks
By centralizing theme resolution in this single method, callers can map a string (for example, user input) to a Theme object without duplicating comparison logic or null checks. The use of ordinal string comparison ensures consistent, culture-invariant matching across locales. The method relies on GetAvailableThemes providing a valid collection and on DefaultTheme representing a concrete theme.

## Notes
- If GetAvailableThemes returns null, the call to Find will throw a NullReferenceException.
- The search is linear in the size of the themes collection; for large catalogs consider caching or indexing to improve lookup performance.
- Name comparison uses OrdinalIgnoreCase; if you need culture-aware matching, replace with a culture-aware comparison or normalize names elsewhere.

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


Parses a color name into a Color using Color.TryParse. If parsing succeeds, it returns the resulting Color (or White if the parsed color is null). If parsing fails, it returns Color.White. Use this helper when theme code needs to translate a color name string into a Color value, ensuring a valid color is always returned instead of propagating nulls.

## Remarks

Centralizes color-name parsing, reducing duplication and guarding ThemeManager's rendering paths against invalid color inputs. The fallback to White makes the UI predictable but at the risk of hiding misconfigurations; consider logging when a fallback occurs to aid debugging.

## Notes

- Invalid or unknown color names yield Color.White without throwing.
- No exception is thrown; a deterministic Color is always returned.

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


Persists a Theme by serializing it to JSON and writing it to a file named after the theme under ThemeDir. Use SaveTheme to persist a user-selected theme so it can be reloaded on startup; it's a best-effort operation that silently swallows failures, so callers shouldn't rely on it for critical persistence.

## Remarks
This abstraction encapsulates the simple idea of theme persistence: ensure the target directory exists, determine a file path from the Theme.Name, serialize to JSON using JsonOptions, and write the content. It uses Theme.Name as the file name, so two themes with the same name will overwrite each other; an enhanced naming strategy or unique IDs could help. Failures are swallowed, so any persistence failure is invisible to the caller; consider adding logging or a higher-level retry if persistence must be durable. The method depends on JsonOptions for serialization behavior and relies on the standard IO primitives (Directory, Path, JsonSerializer, File).

## Notes
- The catch-all block hides errors; callers cannot detect save failures.
- Using Theme.Name directly as a file name may introduce invalid characters or path traversal risks if Name isn't sanitized.
- Existing theme JSON will be overwritten without backup or versioning.

---

### ClassicTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme ClassicTheme = new()
```


ClassicTheme is a private static readonly Theme that encapsulates the classic visual styling used by the UI. It defines the 'Classic' theme name and assigns color palettes for four UI zones — Base, Menu, Dialog, and Status — so the theming system can render consistent foregrounds, backgrounds, and focus states across the application.

## Remarks
Why this abstraction exists: centralizes the classic color palette in one place, avoiding repetitive literals across components. It also stabilizes the look by exposing a single instance that the ThemeManager can switch to internally to apply the classic aesthetic. In short, ClassicTheme acts as the canonical, versioned styling bundle for the traditional UI appearance.

## Notes
- Potential mutability: If Theme or ThemeColors expose public setters, the colors may be mutated after initialization. Consumers should rely on a stable palette or the code should enforce immutability.
- Accessibility considerations: The palette uses high-contrast combinations (e.g., White foreground on DarkGray/Blue). If your accessibility requirements change, adjust this Theme instance or provide alternative themes.

---

### DefaultTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme DefaultTheme = new()
```


Defines the canonical default theme used by the UI components within the ThemeManager. This private static readonly field initializes a single Theme instance named 'Default' with color settings for each UI region (Base, Menu, Dialog, Status). The nested ThemeColors specify the foreground, background, and focus colors, establishing a consistent look-and-feel across the application unless overridden by other theme configurations. Because it is static and readonly, the instance is created once at type initialization and cannot be reassigned, ensuring all consumers relying on the default palette see the same values.

## Remarks
Centralizes the default visual styling to ensure a single, shared baseline across the UI. It prevents scattering color choices across components and makes it easier to reason about the default appearance of the application. If a different baseline is needed for testing or special scenarios, a separate Theme can be created and applied through the ThemeManager, rather than modifying this field.

## Notes
- The field is private; external code cannot access or mutate DefaultTheme directly.
- Even though the reference is readonly, the nested ThemeColors objects may be mutable if their properties are settable; treat the default palette as effectively immutable at runtime unless you deliberately mutate its contents within ThemeManager.
- The color values are provided as names (e.g., 'Gray', 'White'); ensure the rendering layer recognizes these tokens to avoid unexpected visuals.

---

### DraculaTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme DraculaTheme = new()
```


DraculaTheme is a private static readonly Theme field that represents the Dracula-inspired color palette used by the theme system. It defines distinct color specifications for four UI surfaces—Base, Menu, Dialog, and Status—each with a foreground color, a background color, and explicit focus colors to ensure consistent, high-contrast visuals across the application. This field is intended for internal use by ThemeManager to apply a cohesive dark theme; external code should not rely on it directly.

## Remarks
Having a single DraculaTheme instance centralizes the Dracula look, preventing drift in color choices across components. By keeping it private and readonly, ThemeManager can switch to Dracula without duplicating palettes, while still allowing other themes to be composed similarly. The explicit focus colors help maintain clear keyboard-navigation states even on dark surfaces.

## Notes
- Private visibility prevents external code from referencing DraculaTheme directly.
- It is static readonly and assigned once; runtime mutation is not expected.
- Token names like BrightMagenta and Magenta map to concrete colors in the rendering layer; ensure the color system supports these tokens for accurate rendering.

---

### HackerTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme HackerTheme = new()
```


HackerTheme is a private, static, readonly Theme instance that encodes the color palette used by the Hacker appearance within the UI. It defines the colors for the Base, Menu, Dialog, and Status areas, providing a single source of truth that ThemeManager can apply to render a consistent dark-themed interface.

## Remarks
This field centralizes the Hacker color scheme, ensuring consistent foreground/background pairs across all UI regions and their focus states. Because HackerTheme is private to ThemeManager, external code cannot reference or mutate it directly; changes to the palette must go through ThemeManager's public API or future extensions. The nested ThemeColors per region make it easy to tweak the palette in one place when refining the visual language.

## Notes
- The static readonly modifier means HackerTheme is initialized once and its reference cannot be reassigned, but the contained ThemeColors objects may still be mutable depending on their type.
- External code should not rely on HackerTheme having a public accessor; to reuse the palette publicly, ThemeManager should expose a proper API rather than exposing internal details.

---

### HighContrastTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme HighContrastTheme = new()
```


Defines a private static readonly Theme instance named HighContrastTheme that captures a high-contrast color scheme used by the theming subsystem. It specifies color configurations for the Base, Menu, Dialog, and Status surfaces to maximize legibility and clearly indicate focus against a dark background.

## Remarks
HighContrastTheme centralizes the high-contrast styling to avoid scattering color values throughout the codebase. The ThemeManager can switch to this theme to satisfy accessibility requirements without exposing public API changes.

## Notes
- The nested ThemeColors objects may be mutable; mutating them would undermine the high-contrast guarantee. Treat HighContrastTheme as an internal constant and avoid altering its color properties at runtime.

---

### JsonOptions
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
```


JsonOptions is a privately scoped, preconfigured JsonSerializerOptions instance used by ThemeManager to serialize JSON with the project’s conventions. It enables indented output and camelCase property naming, ensuring that any JSON emitted while theming is both human-readable and aligned with the API surface.

## Remarks
By using a private static readonly field, ThemeManager avoids repeated allocations and guarantees a single shared configuration for its JSON serialization within the class. Note that JsonSerializerOptions is mutable; while the field reference cannot be reassigned, changing its properties at runtime can lead to subtle, cross-call side effects. Treat this instance as effectively immutable after initialization.

## Example
```csharp
// Within ThemeManager
var data = new { Theme = "Dark", Version = 1 };
string json = JsonSerializer.Serialize(data, JsonOptions);
```

## Notes
- Mutating JsonOptions at runtime can cause inconsistent formatting across serialized outputs; prefer making changes only during initialization.
- This field is internal to ThemeManager; if different parts of the application require alternative formatting, construct and pass their own JsonSerializerOptions instead of reusing JsonOptions.

---

### LightTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme LightTheme = new()
```


LightTheme is a predefined Theme instance that encodes the light-mode color configuration used by the UI. It centralizes the color values for the base surface and for Menu, Dialog, and Status regions so the theming system can apply a consistent light appearance without constructing a new Theme object each time.

## Remarks

By consolidating the light palette in a single static object, LightTheme ensures visual consistency across components that render base surfaces, menus, dialogs, and status bars. It serves as a canonical reference for the light aesthetic within the theming subsystem, enabling ThemeManager to switch to a known, shared configuration. Because the field is private static readonly, it should be treated as a shared, effectively immutable source at runtime; mutating its nested color objects could lead to inconsistent visuals.

## Notes

- It is a static shared instance; mutating its nested ThemeColors at runtime would have global effects; treat as read-only after initialization.

---

### MonokaiTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme MonokaiTheme = new()
```


MonokaiTheme is a private static readonly field that defines the Monokai color palette used by the theme system. It holds a Theme named "Monokai" composed of four color blocks (Base, Menu, Dialog, Status), each described by ThemeColors with specific foreground, background, and focus colors. This single, shared instance provides a consistent color vocabulary for the UI, allowing ThemeManager and related rendering code to apply the Monokai look uniformly without scattering literals across the codebase. Because the field is private, its usage is internal to the class that declares it.

## Remarks
MonokaiTheme serves as a centralized, reusable color configuration for the Monokai look. By grouping color sets into Base, Menu, Dialog, and Status, it expresses distinct chrome regions while keeping a single source of truth for the palette. This abstraction makes it straightforward for ThemeManager and UI components to consistently apply the Monokai styling.

## Notes
- The Theme and ThemeColors instances are mutable; altering their properties would mutate the shared theme at runtime and affect all consumers within the process.
- External code cannot replace MonokaiTheme, but internal code could adjust its nested properties unless immutability is enforced; consider making Theme/ThemeColors immutable if a fixed theme is intended.

---

### OceanTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme OceanTheme = new()
```


OceanTheme is a predefined ocean-inspired color palette represented as a Theme instance. It groups color configurations for four UI regions—Base, Menu, Dialog, and Status—each with foreground, background, and focus colors, enabling a cohesive look across the application. The field is private static readonly, so the same Theme object is created once and reused, preventing accidental reassignment while keeping internal mutability restricted to the defining class.

## Remarks
Centralizes theming decisions and reduces duplication by providing a single, cohesive palette that UI components can rely on. OceanTheme expresses a clear design intent (an ocean-like aesthetic) and is intended to be selected by theming logic to apply a consistent appearance across Base, Menu, Dialog, and Status surfaces. The per-area ThemeColors allow distinct focus and interaction states while preserving a unified visual language.

## Notes
- Access is private to the ThemeManager class, preventing external code from directly reusing or mutating OceanTheme.
- The reference is readonly, so the field cannot be reassigned; internal mutability would require explicit code within the defining class.
- The color tokens (e.g., BrightCyan, DarkBlue, White, DarkCyan) must be valid tokens within the project’s visual system for the palette to render correctly.

---

### SolarizedTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme SolarizedTheme = new()
```


SolarizedTheme is a private static readonly Theme instance that encapsulates the Solarized color palette used by the UI. It defines color roles for four UI surfaces—Base, Menu, Dialog, and Status—specifying both normal foreground/background and focused-state foreground/background colors. The field is initialized once at type-load time and is then reused wherever a Solarized look is required, providing a single source of truth for this color scheme and preventing runtime mutations.

## Remarks
This symbol acts as a centralized, immutable specification of the Solarized look. By housing the color tokens in a single Theme, ThemeManager can consistently apply the same palette across menus, dialogs, and status lines without scattering literals throughout the code. The private static readonly pattern communicates intent: SolarizedTheme is a predefined, non-changing theme available to internal consumers of ThemeManager, not something that should be modified at runtime.

## Notes
- The theme uses string color tokens (e.g., "Cyan", "BrightYellow"), which are resolved by the theming subsystem to actual display colors.
- Because the field is readonly, any changes require rebuilding the Theme instance; runtime mutation is prevented.
- The four ThemeColors sections (Base, Menu, Dialog, Status) each specify both normal and focused color states to support focus indication.

---

### ThemeDir
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly string ThemeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".echohub", "themes")
```


ThemeDir is a private, static readonly string that resolves to the user-specific themes directory by combining the current user’s profile folder with .echohub/themes. It provides a single, OS-agnostic path for ThemeManager to load and save theme files, avoiding scattered string literals.

## Remarks
Centralizing the location of theme assets decouples theme storage from OS conventions and hard-coded paths, making future relocations or tests simpler. The static readonly nature guarantees a consistent path across all ThemeManager operations, computed at type initialization. If the target directory doesn't exist at runtime, higher-level startup or initialization code should ensure it is created before any read/write of themes.

## Notes
- Directory existence: ensure creation to avoid IO errors when reading or writing themes.
- Hidden folder nuance: .echohub will be hidden on Unix-like systems; consider how this affects user visibility or directory listings in certain UI scenarios.

---

### TransparentLightTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme TransparentLightTheme = new()
```


Represents a canonical light-theme configuration used by the UI to render surfaces on light backgrounds. TransparentLightTheme is a private static readonly Theme instance that bundles a complete color palette for Base, Menu, Dialog, Status, and Border, enabling a consistent light appearance across the UI when a light or transparent background is in use. The defined colors map foregrounds, backgrounds, and focus states to maintain readability and clear focus cues (Blue for focused elements).

## Remarks
By centralizing the light-theme palette in a single internal Theme instance, this symbol reduces drift between UI surfaces and makes it straightforward to derive alternate light variants from a single baseline. Its private visibility signals it's an internal default rather than a public customization point; external code should define and consume their own Theme instances instead of mutating this one.

## Notes
- Border foreground uses #8F8F8F for softer borders on light terminals.
- Background values set to 'None' indicate transparency or reliance on the parent/background, aligning with a transparent-light aesthetic.

---

### TransparentTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme TransparentTheme = new()
```


Defines a single, shared Theme instance named TransparentTheme that implements a glassy, semi-transparent UI aesthetic. Declared private static readonly, it is initialized once and reused by the ThemeManager to apply a cohesive translucent look across Base, Menu, Dialog, Status, and Border color groups (most backgrounds are None to preserve translucency, with White foreground and BrightCyan focus colors; Dialog uses DarkGray to retain legibility; borders use muted grays to complete the glassy look).

## Remarks
This symbol centralizes the glassy appearance so all UI surfaces adopting transparency share a single color model. Being private ensures the theme is an internal implementation detail of ThemeManager and not part of the public theming surface. If a project needs a similar variant publicly, it should be created as a separate, publicly accessible theme instance rather than exposing this private field. The pattern reduces drift between components and simplifies maintenance of the transparent aesthetic.

## Notes
- The field is readonly, but its nested color objects are not guaranteed immutable; mutating their properties at runtime would alter the shared theme for all users. Treat the instance as immutable after initialization to preserve consistency.

---

## BuiltInThemes
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


BuiltInThemes is a private static readonly collection that enumerates the Theme instances shipped as built-in themes. It provides a stable, canonical set of themes (including DefaultTheme, TransparentTheme, TransparentLightTheme, ClassicTheme, LightTheme, HackerTheme, SolarizedTheme, DraculaTheme, MonokaiTheme, NordTheme, GruvboxTheme, OceanTheme, HighContrastTheme, and RosePineTheme) that ThemeManager can iterate over to present theme options and initialize theming state. Because the field is private and readonly, external code cannot modify this collection at runtime; it is intended as an internal baseline that ensures consistent theming behavior across the application.

## Remarks
Centralizes the shipped themes into a single place, guaranteeing a consistent ordering and a single source of truth for what counts as built-in. This reduces duplication and makes it easier to adjust defaults or add new themes by updating the initializer, rather than sprinkling Theme references throughout the code. Because it's private, consumers must rely on public Theme-related APIs or ThemeManager flows to query or apply themes.

## Notes
- The list is constructed from static Theme instances defined elsewhere (the DefaultTheme, TransparentTheme, etc.).
- As a private, readonly field, it cannot be replaced or mutated at runtime; new themes must be added via source changes.
- If you need to expose or customize the built-in set, provide a public API rather than accessing this field directly.

---

## GruvboxTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme GruvboxTheme = new()
```


GruvboxTheme is a private, static, readonly Theme instance that encodes the Gruvbox color palette for the EchoHub client UI. It defines colors for core regions—Base, Menu, Dialog, and Status—each with a Foreground, Background, FocusForeground, and FocusBackground value. This single object acts as the canonical Gruvbox styling source consumed by the theming subsystem to render a consistent look across the application. Because the field is private and readonly, external callers should rely on ThemeManager's public mechanisms to obtain themed resources rather than mutate or reference this field directly.

## Remarks

By centralizing the palette in one immutable object, GruvboxTheme reduces drift between UI regions and simplifies theming changes. The per-region color groups reflect a clean separation of concerns: Base handles the main chrome, Menu for navigation, Dialog for modal surfaces, and Status for status indicators; the consistent focus colors ensure accessible emphasis when keyboard navigation occurs. This pattern makes it straightforward to swap themes by replacing the underlying Theme instance without scattering color literals throughout the code.

## Notes

- The readonly reference prevents re-assignment, but if Theme or ThemeColors are mutable, their values can still be mutated at runtime.
- This field is private; there is no direct public API here—consumers should obtain theme data via ThemeManager's public surface rather than accessing GruvboxTheme directly.

---

## NordTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme NordTheme = new()
```


NordTheme defines the internal, immutable Nord color palette used by ThemeManager to style the UI. It is a single Theme instance configured with per-surface color mappings (Base, Menu, Dialog, Status) so the Nord look is applied consistently without duplicating color definitions throughout the code.

## Remarks
This symbol centralizes the Nord appearance, providing a single source of truth for foreground/background and focus colors across different UI surfaces. It is private to ThemeManager, which means external code should interact with the public theming API rather than reference or mutate this instance. The approach reduces drift between surfaces and makes it easy to switch themes by swapping higher-level theme providers rather than tweaking individual components.

## Notes
- Be aware that the readonly modifier applies to the field reference; nested ThemeColors instances may still be mutable if their properties expose setters. If true immutability is required, consider making Theme and ThemeColors immutable or returning defensive copies.


---

## RosePineTheme
> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** field

```csharp
private static readonly Theme RosePineTheme = new()
```


RosePineTheme is a private static readonly Theme instance that encapsulates the RosePine color palette used by the UI. It defines per-area color configurations for Base, Menu, Dialog, and Status, pairing foreground and background colors with their focused variants. This centralized definition provides a single source of truth for the RosePine look and is consumed by the theming subsystem rather than by external code, helping maintain a cohesive visual style across the application.

## Remarks
Centralizes theme-related color data to ensure visual consistency and to simplify theme swapping or adjustment. Keeping the field private hides implementation details from consumers and enforces usage through the theming infrastructure, reducing the risk of accidental divergence in color usage.

## Notes
- The field is private; external code cannot reference RosePineTheme directly.
- The field is readonly in reference, but its internal properties may be mutable depending on ThemeColors' mutability; if ThemeColors exposes setters, the palette could be modified after initialization.
- Static initialization order and potential side effects: If ThemeManager relies on RosePineTheme during application startup, ensure initialization order is correct.

---