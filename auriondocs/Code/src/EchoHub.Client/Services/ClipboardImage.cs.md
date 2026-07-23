# ClipboardImage

> **File:** `src/EchoHub.Client/Services/ClipboardImage.cs`  
> **Kind:** class

```csharp
public static class ClipboardImage
```


Reads raw image data from the OS clipboard and returns PNG-encoded bytes suitable for saving, embedding, or transmitting. Use this when you need a single, consistent PNG representation of whatever image the user has copied (browser-copied PNGs, screenshots, editor bitmaps) so callers don't need per-OS or per-format handling.

## Remarks
This class normalizes multiple clipboard image formats into PNG. It prefers native clipboard PNG formats when available (preserving transparency) and falls back to platform clipboard bitmaps (CF_DIB on Windows) by wrapping the DIB bytes in a minimal BMP file header and decoding/re-encoding them as PNG. TryGetPng routes to OS-specific helpers and catches/logs errors, returning false on failure rather than throwing.

## Example
```csharp
// Save whatever image is on the clipboard to a file named clipboard.png
if (ClipboardImage.TryGetPng(out var png))
{
    System.IO.File.WriteAllBytes("clipboard.png", png);
}
```

## Notes
- DibToPng returns null for malformed or undecodable DIB input; TryGetPng propagates that as a failure (false). 
- The implementation prefers registered PNG clipboard formats to preserve alpha; CF_DIB bitmaps are re-encoded and may lose or change metadata.
- Re-encoding a bitmap to PNG allocates memory and does CPU work; callers should avoid doing this in a tight loop.
- TryGetPng checks the platform (Windows/Linux/macOS) and will return false on unsupported platforms; failures are logged rather than thrown.