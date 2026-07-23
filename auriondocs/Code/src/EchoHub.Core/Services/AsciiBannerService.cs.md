# AsciiBannerService

> **File:** `src/EchoHub.Core/Services/AsciiBannerService.cs`  
> **Kind:** class

```csharp
public static class AsciiBannerService
```


AsciiBannerService renders a string as a five-row block-character banner using a hand-authored, figlet-style font defined entirely in code. It is self-contained — no dependencies and no network access — and returns plain text suitable for transport or encryption just like any other message. Use `Render` when you need a compact, dependency-free banner for logs, UI previews, or console-like output.

## Remarks
AsciiBannerService provides a centralized, self-contained banner rendering capability that does not rely on external resources. The glyphs are embedded in a private `Font` dictionary, ensuring deterministic rendering across environments. The banner width is bounded by `MaxInputLength` (20 characters) and the height is fixed to `Rows` (5), which keeps banners predictable in size and performance. Input is normalized by converting to uppercase with `ToUpperInvariant()`, and only characters present in `Font` are rendered; unsupported characters are skipped. The final output is assembled with a `StringBuilder`, joining glyph rows horizontally with spaces and replacing `#` (ink) with the solid block character `█` and `.` (blank) with spaces. Trailing spaces on each line are trimmed to minimize payload.

## Example
```csharp
var banner = AsciiBannerService.Render("TEST");
```

## Notes
- Returns `null` when the input is empty, whitespace, or contains no renderable characters.
- Non-renderable characters are skipped; only characters present in `Font` contribute to the banner.
- The input is capped at `MaxInputLength` characters, and the output always consists of exactly `Rows` lines if renderable content exists.
