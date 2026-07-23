# Theming and UI color management

> Representing themes, color palettes, and runtime theme application.

Theming and UI color management

The files in this topic define how the EchoHub client represents color themes, exposes a curated set of built-in and user-provided themes, and wires theme selection into the running application. Read these three artifacts to understand the Theme data model, the static ThemeManager API that discovers/applies/persists themes, and the AppOrchestrator entry point that reacts to user commands and delegates theme work to the manager.

## Theme.cs
Represents a UI theme.

The [Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) class is the data container for a complete UI appearance. It exposes a required Name plus four area-specific palettes—Base, Menu, Dialog, and Status—each typed as a [ThemeColors](../Code/src/EchoHub.Client/Themes/Theme.cs.md) instance, and an optional Border palette that, when set, overrides only frame-border colors while leaving other chrome tied to Base. The writer notes sensible defaults: each palette initializes to a new ThemeColors so a Theme is usable with minimal configuration, and Border accepts hex literals or named colors to let designers tint edges without touching text palettes. This file is the canonical representation of a theme and is consumed by the [ThemeManager](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) to build and persist theme choices and by the [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) when the application needs to apply or react to theme changes.

## ThemeManager.cs
Loads, caches, and applies themes across the app.

[ThemeManager](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) is a static API that bridges theme data and runtime application. It exposes discovery and retrieval functions such as GetAvailableThemes and GetTheme, mutation points like SaveTheme, and the runtime switch ApplyTheme; utility functions include ParseColor and BuildColorScheme, the latter ensuring colors for editable/read-only roles and transparency behave correctly so inputs remain legible under transparent themes. ThemeManager maintains a curated set of built-in theme factory methods (DefaultTheme, DraculaTheme, LightTheme, etc.), attempts to load additional themes from a user directory (ThemeDir), and falls back to built-ins if the directory cannot be read; SaveTheme is implemented best-effort and quietly swallows failures. Because it returns and manipulates [Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) instances, ThemeManager is the component the [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) calls when the app needs to enumerate, choose, or persist a theme and when it needs the computed color scheme to apply to the UI.

## AppOrchestrator.cs
`AppOrchestrator` collaborates directly with `Theme` and other members of this topic (2 dependency links).

[AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) is the application-level coordinator that owns the MainWindow and a large set of command handlers; among its many responsibilities it includes a handler named HandleCmdSetTheme which responds to theme-change requests. In practice the orchestrator calls into [ThemeManager](../Code/src/EchoHub.Client/Themes/ThemeManager.cs.md) to fetch or apply a [Theme](../Code/src/EchoHub.Client/Themes/Theme.cs.md) (for example via GetTheme and ApplyTheme) and then ensures the active UI reflects the manager-provided color scheme. The doc block lists the constructor and MainWindow property plus the command handlers (including HandleCmdSetTheme) so the intended runtime flow is: user or code issues a theme command to AppOrchestrator, AppOrchestrator delegates theme discovery/load/apply to ThemeManager, and the Theme instance shapes the MainWindow styling.

How the pieces fit

Theme is the immutable-ish data model for visual choices; ThemeManager is the static service that discovers, builds, parses, and persists those models and produces a concrete color scheme via BuildColorScheme; AppOrchestrator is the runtime conductor that responds to user commands and uses ThemeManager to fetch and ApplyTheme to the UI. The dependency direction is AppOrchestrator -> ThemeManager -> Theme, with ThemeManager also responsible for supplying built-in Theme instances and reading user themes from disk when available.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:51:21 UTC*
