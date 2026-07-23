# FileValidationHelper

> **File:** `src/EchoHub.Core/Services/FileValidationHelper.cs`  
> **Kind:** class

```csharp
public static class FileValidationHelper
```


FileValidationHelper centralizes lightweight, stream-based validation for common image formats and audio file names. Its IsValidImage(Stream) method reads the stream header (without changing the stream's position) and recognizes JPEG, PNG, GIF, and WebP by their magic numbers, returning true for known formats and false otherwise. IsAudioFile(string) validates a file name’s extension against a predefined set of audio extensions in a case-insensitive manner. Together, these helpers let callers pre-filter content before attempting to decode or process media data.

## Remarks
This symbol provides a single, testable utility to detect supported media formats without pulling in a full decoder. By encapsulating the magic-number checks and the extension-based guard, it reduces duplication and concentrates format-coverage decisions in one place. It favors a fast, low-allocation validation path and leaves actual parsing to dedicated components.

## Notes
- Non-seekable streams cause IsValidImage to return false (the check stream.CanSeek is performed up-front).
- IsAudioFile relies solely on the file extension and does not inspect file contents.
- WebP detection requires a RIFF header followed by a WEBP tag at the expected offsets; malformed headers degrade gracefully to false.