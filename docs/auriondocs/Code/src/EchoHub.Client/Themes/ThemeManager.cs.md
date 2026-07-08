# ThemeManager

> **File:** `src/EchoHub.Client/Themes/ThemeManager.cs`  
> **Kind:** class

```csharp
public static class ThemeManager
```


ThemeManager is a static helper that centralizes the built-in theming for the EchoHub client. It defines several ready-made themes (Default, Classic, Light, Hacker, Solar) as Theme objects and configures how theme data is serialized and where it is stored. The class establishes a per-user themes directory under the current user’s profile and uses a JsonSerializerOptions instance that writes indented JSON with camelCase property names, ensuring theme data is easy to inspect and consistently formatted. Each Theme describes color palettes for four UI regions—Base, Menu, Dialog, and Status—covering both normal and focus states, so the rendering layer can apply a cohesive look across the interface.

## Remarks
This abstraction isolates theming concerns from the rendering and business logic, enabling runtime theme switching or extension with new presets without touching UI code. By acting as a single, centralized source of truth for color semantics, ThemeManager makes it straightforward to swap palettes at a system or user level while preserving layout and behavior across the app. The built-in themes provide a consistent baseline that can be extended or overridden by user-defined themes stored in the designated per-user directory. 