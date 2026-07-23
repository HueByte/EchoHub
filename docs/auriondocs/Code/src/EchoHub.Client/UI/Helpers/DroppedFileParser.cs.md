# DroppedFileParser

> **File:** `src/EchoHub.Client/UI/Helpers/DroppedFileParser.cs`  
> **Kind:** class

```csharp
public static class DroppedFileParser
```


DroppedFileParser is a small utility that interprets terminal-dropped input as potential file paths and resolves them to existing files. Use it when you need to convert user-typed or pasted text into concrete file paths without scattering filesystem checks across callers.

## Remarks
This abstraction centralizes the logic for recognizing path-like input and for extracting one or more existing file paths from either a single path or a space-separated list of paths. It exposes a fast pre-check (LooksLikePath) to avoid expensive filesystem calls for clearly non-path input, and a test-friendly parser (TryGetFiles) that can inject a custom file existence predicate. The design favors explicit handling of both Windows (drive letters and UNC) and POSIX-style absolute paths, including quoted components and spaces.

## Example
```csharp
var input = "\"C:\\Temp\\report.pdf\" C:\\Data\\log.txt";
if (DroppedFileParser.TryGetFiles(input, out var files))
{
    // files contains: ["C:\\Temp\\report.pdf", "C:\\Data\\log.txt"]
}
```

## Notes
- LookSLikePath may return true for strings that resemble paths (e.g., starting with a quote, a slash, UNC prefix, or a drive letter), so TryGetFiles should be used to confirm actual file existence.
- TryGetFiles enforces that all tokens are fully-qualified paths and that each path exists (via the injectable fileExists predicate, which defaults to File.Exists). This reduces accidental assumptions about the input.
- The tokenization logic respects quoted segments so that spaces within a single path do not split tokens unintentionally.
