# ImageToAsciiService

> **File:** `src/EchoHub.Core/Services/ImageToAsciiService.cs`  
> **Kind:** class

```csharp
public class ImageToAsciiService
```


ImageToAsciiService converts an image stream into ASCII art using two vertical pixels per character and half-block characters. The static `GetDimensions` translates a size code (`'s'`, `'m'`, `'l'`) into ASCII art dimensions (40x40, 80x80, 120x120 respectively) and returns the default dimensions from `HubConstants.AsciiArtWidth` and `HubConstants.AsciiArtHeightHalfBlock` for other codes. The instance method `ConvertToAscii` accepts a `Stream` containing an image and returns a string composed of color tokens and block characters. Each character cell encodes two vertical pixels; a foreground color token `{F:RRGGBB}` and a background color token `{B:RRGGBB}` are emitted when colors change, followed by a block character (either `█` or `▀`), with `{X}` used to reset coloring. The output uses only printable ASCII and avoids terminal escape sequences. 

## Remarks
`ImageToAsciiService` encapsulates the image-to-ASCII rendering logic, separating it from image loading and presentation concerns. It centralizes the color-token encoding and block-character strategy so callers can produce text-based previews in environments that cannot render images. By relying on [`HubConstants`](../Constants/HubConstants.cs.md) for defaults, global rendering preferences propagate naturally to this converter. 

## Example
```csharp
using System.IO;

using var fs = File.OpenRead("path/to/image.png");
var service = new ImageToAsciiService();
string ascii = service.ConvertToAscii(fs);
```

## Notes
- The converter emits color-change tokens only when the foreground or background color differs from the previous pixel pair, which keeps the output compact for large areas of uniform color.
- Each ASCII cell represents two vertical pixels; the image is resized to the requested `width` and `height` (defaulting to `HubConstants.AsciiArtWidth` and `HubConstants.AsciiArtHeightHalfBlock` if not specified). This can alter aspect ratio, so choose dimensions with that in mind.
- The format relies on the tokenized color syntax (e.g. `{F:RRGGBB}` and `{B:RRGGBB}`) being understood by the consumer; renderers that ignore these tokens will display plain block characters without color.</