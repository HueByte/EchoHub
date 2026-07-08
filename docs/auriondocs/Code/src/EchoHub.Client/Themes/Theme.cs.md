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


Represents a named UI theme that groups color palettes for distinct surfaces (Base, Menu, Dialog, Status). Use Theme when you want to apply a cohesive look across the application by swapping a single object, while still allowing per-surface customization.

## Remarks

Theme acts as a lightweight value object that centralizes styling decisions and decouples presentation from rendering logic. By bundling related ThemeColors under a single Theme, components can consume a consistent set of visuals and switch themes at runtime to alter the application's appearance. The default initialization of Base, Menu, Dialog, and Status to new ThemeColors ensures the object is ready to customize without null checks, while still permitting per-surface overrides to tailor the look.

## Notes

- Mutating the Theme's color surfaces after it has been applied may cause inconsistent visuals unless the UI subscribes to Theme changes or ThemeColors are immutable.
- Reusing the same Theme instance across many components can lead to shared mutable state; consider cloning or treating Theme as a value-like object when isolation is required.
- The Name property is marked required; the compiler enforces setting Name during initialization, so forgetting to provide a value will produce a compile-time error.

---

## ThemeColors
> **File:** `src/EchoHub.Client/Themes/Theme.cs`  
> **Kind:** class

```csharp
public class ThemeColors
```


ThemeColors is a lightweight data container that defines the color palette used by the UI theming system. It exposes four color properties: Foreground and Background for normal elements, and FocusForeground and FocusBackground for focused elements. The defaults encode a dark theme baseline: Foreground = "White", Background = "Black", FocusForeground = "White", FocusBackground = "Blue". Instantiate ThemeColors to supply a consistent color scheme across controls and views, rather than scattering color literals throughout rendering code.

## Remarks

By grouping color settings in a single object, ThemeColors decouples theming concerns from rendering logic and makes it easy to swap themes or tailor appearances for different environments (dark mode, accessibility). It is a plain data transfer object (DTO) that collaborators such as style managers and UI components consume to apply colors consistently.

## Example

```csharp
var colors = new ThemeColors
{
    Foreground = "Black",
    Background = "White",
    FocusForeground = "White",
    FocusBackground = "Blue"
};
```

## Notes

- Mutability: all properties have public setters; changing them at runtime affects all consumers sharing the same instance unless they clone the object.

---