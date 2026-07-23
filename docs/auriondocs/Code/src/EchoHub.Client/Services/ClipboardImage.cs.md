# ClipboardImage

> **File:** `src/EchoHub.Client/Services/ClipboardImage.cs`  
> **Kind:** class

```csharp
public static class ClipboardImage
```


Reads raw image bytes from the platform clipboard and returns them as a PNG byte array when available. Use `ClipboardImage.TryGetPng` when you need a canonical, pasteable PNG representation of whatever image the user has on the clipboard (for example, when accepting pasted screenshots or images in a terminal or chat input that cannot accept raw bitmap data).

## Remarks
`ClipboardImage` centralizes platform-specific clipboard handling: `TryGetPng` dispatches to `TryGetWindows`, `TryGetLinux`, or `TryGetMacOS` depending on `OperatingSystem` checks, and normalizes all outputs to PNG. When the clipboard format already contains PNG bytes (detected using the `PngMagic` signature or platform-registered PNG formats such as those discovered via `RegisterClipboardFormatW` on Windows), the bytes are passed through to preserve fidelity and transparency. When the clipboard exposes a DIB/bitmap (`CfDib` on Windows), the `DibToPng` helper builds a minimal BMP wrapper around the DIB bytes, decodes it with `Image.Load`, and re-encodes the result as PNG; this covers screenshots and editors that expose only device-independent bitmaps.

The class intentionally swallows and logs exceptions (via `Log.Warning`) from clipboard access and image decoding so callers get a simple success/failure result from `TryGetPng` instead of propagating clipboard or image-library exceptions.

## Notes
- Clipboard APIs are platform and threading sensitive. On Windows the OS clipboard typically requires running on an STA thread; calling `TryGetPng` from a non-STA thread may fail or return false. Ensure clipboard access is performed on an appropriate thread context for the platform.
- `DibToPng` validates the DIB header (minimum 40 bytes, header size bounds) and returns null for malformed input. Decoding can still fail at `Image.Load` for unsupported or corrupted bitmaps; such failures are logged and surface as a failure to `TryGetPng`.
- Re-encoding a DIB to PNG may not preserve alpha/transparency if the original bitmap format lacks alpha channels (DIB/CF_DIB often does not include alpha). If preserving exact alpha semantics is required, prefer sources that supply native PNG clipboard formats when possible.
- Converting clipboard data allocates buffers (the BMP wrapper and the resulting PNG byte array) and performs image decode/encode work; callers should expect a non-trivial CPU and memory cost for large images.