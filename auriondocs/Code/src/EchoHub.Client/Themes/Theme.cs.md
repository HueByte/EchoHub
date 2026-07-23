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


Theme is the central container for theming the EchoHub client UI. It holds a Name and four color palettes (Base, Menu, Dialog, Status) used across the main window and its chrome; plus an optional Border palette that can override only the frame borders while the rest remains tied to Base. If Border is null, the border colors fall back to the Base palette, letting themes tone borders down independently from text to achieve effects like glassy translucency. The palettes default to new ThemeColors instances, so a Theme is immediately usable and developers only configure what they need. Border supports hex literals like "#6E6E6E" and named colors, enabling quick tweaks without changing the rest of the palette.

## Remarks
Theme isolates brand identity and UI chrome from layout logic, enabling themes to be swapped at runtime or per user preference. The per-area color groups—Base, Menu, Dialog, and Status—provide visual consistency while allowing targeted overrides; Border offers a focused knob for edge treatment without touching text colors. This composition reduces duplication: a single Theme can render across the chrome, with optional Border overrides to achieve distinctive looks without rewriting color logic.

## Notes
- Name is marked as required; always provide a non-empty value during initialization.
- Border is nullable. If you don't set it, the UI uses Base colors for borders; set Border when you want to tint borders independently.
- Hex codes and named colors: ensure strings you assign are valid color tokens understood by the theming system to avoid fallback or misrendering.

---

## ThemeColors
> **File:** `src/EchoHub.Client/Themes/Theme.cs`  
> **Kind:** class

```csharp
public class ThemeColors
```


ThemeColors is a small data container that holds the color choices used by the UI theme. It exposes four properties—Foreground, Background, FocusForeground, and FocusBackground—each with a sensible default (White on Black for normal state, and White on Blue for focused state). This class centralizes theming values so UI components can render consistently and themes can be swapped by supplying a ThemeColors instance rather than scattering color literals throughout rendering code.

## Remarks
- It acts as a cohesive value object for theming, separating concerns between color data and rendering logic.
- It enables swapping themes by replacing one ThemeColors instance rather than modifying rendering code.
- It is mutable, allowing runtime theme adjustments; if a ThemeColors instance is shared across threads, consider synchronization to avoid race conditions.

## Notes
- If you mutate and share ThemeColors across threads, you may encounter race conditions; prefer per-thread copies or proper synchronization when updating values.

---