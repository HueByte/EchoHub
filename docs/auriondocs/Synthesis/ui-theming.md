# UI theming and theme management

> Theme data models and the system that loads, stores, and applies themes to the UI.

This guide explains the client-side theming pieces: the Theme data model, the ThemeManager that provides built-in and user-provided themes and applies them at runtime, and the AppOrchestrator that coordinates UI behavior (including theme usage). Read this when you need to add a new theme, wire theme selection into the UI, or understand how theme persistence and runtime application are handled.

## ThemeManager.cs
Manages built-in themes, theme lookup, and application.

[ThemeManager](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) is a static helper that centralizes theming for the client UI. The class defines a fixed set of built-in theme instances (named constants such as DefaultTheme, TransparentTheme, DraculaTheme, NordTheme, etc.), exposes a ThemeDir and JsonOptions for disk-backed theme discovery and persistence, and provides methods callers use to enumerate, fetch, apply, and save themes: GetAvailableThemes() merges built-ins with user theme files (skipping duplicates and malformed files and falling back to built-ins if the directory cannot be read), GetTheme(name) retrieves a theme by name, SaveTheme persists a Theme to disk, and ApplyTheme performs the runtime application of a Theme to the UI. The file also contains utility logic used by those flows — ParseColor to turn color strings into runtime values and BuildColorScheme(ThemeColors) which maps a Theme's ThemeColors into the editor/UI surfaces so properties like transparency are preserved. ThemeManager stores and manipulates instances of the [Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) model and is the primary integration point other code uses to present, switch, or persist themes.

## Theme.cs
Represents a theme data model used by the theming system.

[Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) is the data descriptor for a visual style. A Theme groups per-surface color sets (Base, Menu, Dialog, Status) and optionally supplies a Border color that overrides the window frame independently of the surface colors; if Border is null, consumers fall back to Base. Each surface is represented by a [ThemeColors](../Code/src/EchoHub.Client/Themes/Theme.cs.md) instance, which bundles Foreground, Background, FocusForeground, and FocusBackground tokens. ThemeColors provides sensible defaults (a high-contrast dark baseline) but is mutable via public setters, so callers can tweak palettes after construction; Theme objects are the units ThemeManager stores, enumerates, and writes to disk.

## AppOrchestrator.cs
`AppOrchestrator` collaborates directly with `Theme` and other members of this topic (2 dependency links).

[AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) is the application-level coordinator that owns the MainWindow and many UI command handlers and lifecycle operations (the class lists a constructor, MainWindow, and dozens of handler and utility methods). Per its declared relationships it depends on the [Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) model and the [ThemeManager](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) helper. In practice AppOrchestrator is the place where UI-driven behavior is orchestrated: it presents or responds to user actions and calls into ThemeManager to retrieve available Theme objects, fetch a Theme by name, or request that a Theme be applied or saved so the MainWindow and its child surfaces reflect the current style. Because AppOrchestrator centralizes command handling and window-level concerns, it is the natural integration point to wire theme selection UI into the running application and to persist user choices through ThemeManager.

How the pieces fit

ThemeManager is the provider and manipulator of Theme instances: it supplies built-in Theme objects, discovers and loads user themes from ThemeDir, parses color text, builds the UI color scheme, and persists themes to disk. The Theme class and its ThemeColors containers are the plain-data contract ThemeManager uses to describe a palette and to hand color sets to UI code. AppOrchestrator acts as the runtime coordinator: it uses ThemeManager to enumerate and fetch Theme objects in response to UI commands and ensures the MainWindow and related surfaces receive the Theme (and thus the color scheme) to render the chosen look. Together they form a simple pipeline: Theme data (Theme/ThemeColors) ↦ ThemeManager I/O and mapping (BuildColorScheme / ParseColor / SaveTheme) ↦ AppOrchestrator-driven application to the live UI.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:31:32 UTC*
