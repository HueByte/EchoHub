# FileValidationHelper

> **File:** `src/EchoHub.Server/Services/FileValidationHelper.cs`  
> **Kind:** class

```csharp
public static class FileValidationHelper
```


Provides a focused, centralized way to verify whether a stream contains a common image format by inspecting the leading header bytes; use it whenever you need a quick, non-destructive check before processing image data. It also offers a lightweight IsAudioFile helper to detect popular audio file extensions by name.

## Remarks
Centralizes image-format recognition, so callers don't need to embed magic-byte checks. It preserves the original stream position after validation to avoid side effects. Constants for image signatures (JPEG, PNG, GIF, WebP) make the logic easy to extend with additional formats as needed.

## Example
```csharp
using System.IO;

using var imageStream = File.OpenRead("image.webp");
bool isImage = FileValidationHelper.IsValidImage(imageStream);

bool isAudio = FileValidationHelper.IsAudioFile("song.mp3");
```

## Notes
- Requires a seekable stream; non-seekable streams will be rejected (IsValidImage returns false).
- WebP detection relies on a 12-byte header; shorter data won't be recognized.
- IsAudioFile is based on extension only; it does not validate MIME types.
