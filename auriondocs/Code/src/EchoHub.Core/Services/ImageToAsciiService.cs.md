# ImageToAsciiService

> **File:** `src/EchoHub.Core/Services/ImageToAsciiService.cs`  
> **Kind:** class

```csharp
public class ImageToAsciiService
```


ImageToAsciiService is a lightweight utility that converts an input image stream into color-aware ASCII art by packing two vertical pixels into a single character cell using half-block characters and per-cell color tags. Use GetDimensions to pick a target resolution and ConvertToAscii when you need a textual, ASCII-only representation of an image for logs, chat, or environments without graphical support.

## Remarks
The class embodies a small, focused translation between raster images and ASCII art. It emits inline color tokens only when the color changes, preserving color fidelity while keeping the output readable in plain-text environments. The two-pixel vertical mapping (top pixel as the foreground color, bottom pixel as the background) enables higher-density representation than single-character ASCII, while remaining printable and parseable by consumers that understand the {F:...}{B:...}{X} tags. An even-height safeguard ensures the processing loop always handles complete pixel pairs, resizing the image as needed to maintain consistent output.

## Example
```csharp
using System.IO;

var stream = File.OpenRead("path/to/image.png");
var service = new ImageToAsciiService();
string ascii = service.ConvertToAscii(stream, 80, 40);
Console.WriteLine(ascii);
```

## Notes
- The ASCII output relies on the presence of the {F:RRGGBB}{B:RRGGBB}{X} tags and the block characters; ensure your rendering environment understands these tokens, otherwise you will see literal tags.
- If a height is provided as an odd number, the implementation advances to an even height, which may slightly alter the aspect ratio of the produced art.