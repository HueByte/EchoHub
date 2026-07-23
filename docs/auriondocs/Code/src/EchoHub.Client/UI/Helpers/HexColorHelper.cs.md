# HexColorHelper

> **File:** `src/EchoHub.Client/UI/Helpers/HexColorHelper.cs`  
> **Kind:** class

```csharp
public static class HexColorHelper
```


HexColorHelper is a small static utility that converts hex color strings into Terminal.Gui color representations. Use `ParseHexColor` when you need an `Attribute` for immediate application to a UI element, and `ParseHexToColor` when you only need the `Color` value (with an optional `fallback`) for other color-related properties.

## Remarks
These helpers centralize hex parsing to ensure consistent handling of hex colors across the UI layer. They both tolerate the common '#'-prefixed form and treat invalid inputs gracefully by returning null or a fallback color, preventing exceptions from propagating into UI code. By encapsulating parsing logic here, you avoid duplicating string-to-color conversions and make future changes (e.g., supporting shorthand hex) easier.

## Notes
- Leading whitespace before the optional '#' is not trimmed; strings starting with spaces will fail to parse gracefully.
