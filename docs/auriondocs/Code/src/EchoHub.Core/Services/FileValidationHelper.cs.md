# FileValidationHelper

> **File:** `src/EchoHub.Core/Services/FileValidationHelper.cs`  
> **Kind:** class

```csharp
public static class FileValidationHelper
```


FileValidationHelper is a compact utility that centralizes quick, non-destructive checks for media file types. It exposes `IsValidImage(Stream)` to determine if the provided stream represents a known image format by peeking at its header bytes, while always restoring the stream's original position. It also exposes `IsAudioFile(string)` to decide whether a file name uses one of the recognized audio extensions. Use these helpers to validate inputs in upload or ingestion paths without loading or parsing full files, and to keep format-detection logic consistent across the codebase.

## Remarks
By coalescing the magic-byte checks in one place, this abstraction reduces duplication and the risk of inconsistent format handling across components that ingest media. The detection rules cover JPEG, PNG, GIF, and WebP at the header level, with WebP requiring a RIFF header followed by the WebP tag; the private helper `StartsWith` encapsulates the prefix comparison to keep `IsValidImage` focused on intent. The `AudioExtensions` set drives a fast, case-insensitive extension lookup for `IsAudioFile` without touching disk data.

## Example
```csharp
using System.IO;

byte[] header = new byte[] { 0xFF, 0xD8, 0xFF };
using var ms = new MemoryStream(header);
bool isImage = FileValidationHelper.IsValidImage(ms);

bool isAudio = FileValidationHelper.IsAudioFile("song.MP3");
```

## Notes
- The stream passed to `IsValidImage` must be seekable; non-seekable streams will not have their position reset and may lead to false results.
- `IsAudioFile` performs a purely extension-based check and does not inspect file contents.
- The image-detection logic recognizes specific headers (JPEG, PNG, GIF, WebP) and is not a full format validator; for strict validation, perform content analysis beyond these checks.