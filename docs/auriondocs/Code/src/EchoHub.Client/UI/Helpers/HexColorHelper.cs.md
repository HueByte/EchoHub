# HexColorHelper

> **File:** `src/EchoHub.Client/UI/Helpers/HexColorHelper.cs`  
> **Kind:** class

```csharp
public static class HexColorHelper
```


HexColorHelper is a compact utility for converting hex color strings into Terminal.Gui styling constructs. It exposes two static helpers: ParseHexColor, which yields an Attribute? from a 6-digit hex color, and ParseHexToColor, which returns a Color and accepts a fallback when parsing fails. Both accept an optional leading '#' and trim whitespace, returning null or the provided fallback on invalid input.

## Remarks
HexColorHelper centralizes hex parsing so theming and color configuration can be expressed as hex strings without duplicating parsing logic throughout the UI layer. It acts as a bridge between string-based color definitions and Terminal.Gui's Attribute and Color types, reducing boilerplate in components that style controls. It also encapsulates validation rules (exactly six hex digits) to prevent subtle color misconfigurations from spreading.

## Notes
- ParseHexColor returns null for null/whitespace input or invalid hex length; handle nulls before use.
- ParseHexToColor returns the provided fallback when input is invalid or parsing fails.
- Only 6-digit hex strings are supported; shorthand 3-digit hex or hex with alpha are not handled by these methods.