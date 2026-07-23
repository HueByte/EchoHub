# AsciiBannerService

> **File:** `src/EchoHub.Core/Services/AsciiBannerService.cs`  
> **Kind:** class

```csharp
public static class AsciiBannerService
```


Renders input text as a 5-row block-character banner (the /banner command). It uses a self-contained, hand-authored font defined in code, with no dependencies or network access, producing plain text content that can be transmitted like any other message; the renderer trims input to the maximum length and skips characters not defined in the font.

## Remarks
This symbol provides a deterministic, dependency-free banner renderer that can be used anywhere a compact ASCII-art label is desirable. The font is embedded in code as a glyph dictionary, so rendering is purely local and consistent across environments. Input is uppercased to match the glyph keys, glyphs are joined per row with a single space, and ink is rendered by replacing the '#' glyphs with the block character '█' and '.' with spaces; trailing spaces on each line are trimmed to minimize payload.

## Example
```csharp
string? banner = AsciiBannerService.Render("EchoHub");
if (banner != null)
    Console.WriteLine(banner);
```

## Notes
- Non-renderable input (no supported characters) yields null; callers should handle null results to avoid printing empty banners.
- The method trims whitespace and enforces a maximum length of 20 characters; longer input is truncated before rendering.