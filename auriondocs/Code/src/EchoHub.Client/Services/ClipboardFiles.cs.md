# ClipboardFiles

> **File:** `src/EchoHub.Client/Services/ClipboardFiles.cs`  
> **Kind:** class

```csharp
public static class ClipboardFiles
```


ClipboardFiles reads file paths from the clipboard when the clipboard contains a file-list (such as after copying files in Explorer/Finder). Use TryGetFiles to retrieve those paths so you can attach copied files directly without pasting textual paths; this works on Windows and Linux, while macOS and other platforms do not expose a file-list clipboard.

## Remarks
ClipboardFiles encapsulates platform differences behind a single API. It isolates Windows-specific CF_HDROP handling and Linux's text/uri-list retrieval, performing path existence checks and filtering out non-file entries to return a clean list of existing paths. It returns true only when at least one file is found; otherwise false, letting callers gracefully fall back to other input methods.

## Example
```csharp
if (ClipboardFiles.TryGetFiles(out var files))
{
    Console.WriteLine($"Clipboard contains {files.Count} file(s): {string.Join(", ", files)}");
}
else
{
    Console.WriteLine("Clipboard does not contain a file-list or contains only non-existent paths.");
}
```

## Notes
- Returns only existing files; non-existent or inaccessible paths are ignored.
- Windows implementation relies on CF_HDROP with a brief retry loop to tolerate clipboard contention.
- Linux implementation uses wl-paste or xclip (one must be available for success).
- macOS and other platforms do not provide file-list clipboard support.