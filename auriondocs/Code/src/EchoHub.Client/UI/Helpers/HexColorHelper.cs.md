# HexColorHelper

> **File:** `src/EchoHub.Client/UI/Helpers/HexColorHelper.cs`  
> **Kind:** class

```csharp
public static class HexColorHelper
```


HexColorHelper is a small utility that converts hex color strings into Terminal.Gui coloring primitives. Use ParseHexColor to obtain an Attribute suitable for styling a control's foreground, and ParseHexToColor when you need a Color value with a safe fallback for invalid input.

## Remarks
By centralizing hex parsing, HexColorHelper avoids duplicating color-conversion logic and provides predictable fallbacks for malformed input. It interprets a hex string as an RGB triplet and applies it as the foreground color (with no explicit background). This keeps styling decisions consistent across the UI while keeping the parsing logic isolated in one place.

## Notes
- Invalid input yields null (for ParseHexColor) or the provided fallback (for ParseHexToColor); no exceptions are thrown.
- A 6-digit hex value is required after an optional leading '#'. Non-hex characters or incorrect length return fallback/null.