# ImageToAsciiService

> **File:** `src/EchoHub.Server/Services/ImageToAsciiService.cs`  
> **Kind:** class

```csharp
public class ImageToAsciiService
```


ImageToAsciiService renders an image stream as colorized ASCII art by pairing two vertical pixels into a single character cell using half-block glyphs, and it encodes colors with {F:RRGGBB} foreground and {B:RRGGBB} background tags while emitting only printable ASCII. Use ConvertToAscii when you need a text-based representation of an image for environments (terminals, logs) that support this color-tag syntax, instead of embedding a binary image or relying on a GUI.

## Remarks

By delegating image decoding and resizing to ImageSharp and concentrating the ASCII composition in ConvertToAscii, this symbol provides a compact, testable path for ASCII rendering with careful color-change handling. It optimizes output by emitting color tags only when the foreground or background color changes, reducing tag churn in images with contiguous color regions. The GetDimensions helper offers quick s/m/l presets, centralizing common sizing logic and reducing boilerplate for consumers.

## Example

```csharp
using System.IO;

var service = new ImageToAsciiService();
using var stream1 = File.OpenRead("path/to/image.png");
string ascii = service.ConvertToAscii(stream1);

using var stream2 = File.OpenRead("path/to/another.png");
string asciiMedium = service.ConvertToAscii(stream2, width: 80, height: 80);
```

## Notes

- Height is adjusted to be even: if an odd height is supplied, the implementation increments it to ensure proper pairing of vertical pixels.
- Color tags are emitted only on changes, which keeps the output compact when adjacent pixels share the same color.
