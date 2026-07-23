# ClipboardFiles

> **File:** `src/EchoHub.Client/Services/ClipboardFiles.cs`  
> **Kind:** class

```csharp
public static class ClipboardFiles
```


ClipboardFiles reads the OS clipboard to obtain a list of files when the clipboard holds a file-list (such as after copying files in a file manager). This enables scenarios where a copied set of files can be pasted or attached directly, without requiring the user to paste raw text paths. Call `TryGetFiles` to retrieve existing file paths from the clipboard; the method returns true when one or more valid paths are found, and false otherwise (including on platforms without file-list clipboard support).

## Remarks
This helper abstracts away platform differences in clipboard formats and presents a single, cohesive API for retrieving file lists from the clipboard. On Windows it enumerates files via the CF_HDROP channel and returns the paths that point to existing files. On Linux it reads a `text/uri-list` from the clipboard (via `wl-paste` or `xclip`), converts `file://` URLs to local paths, and keeps only paths that exist. The implementation favors a graceful failure path: any read-time exception is logged and the caller simply receives a non-success result, allowing callers to degrade gracefully without crashing. The API design emphasizes a simple success/failure boolean along with a concrete list of files, enabling straightforward integration into UX flows that want to treat copied files as attachable entities rather than plain text.

## Example
```csharp
if (ClipboardFiles.TryGetFiles(out var files))
{
    foreach (var path in files)
        Console.WriteLine(path);
}
```

## Notes
- macOS and other non-supported platforms do not provide a file-list clipboard, so `TryGetFiles` returns false there.
- The method only returns paths that actually exist on disk; non-existent or malformed clipboard entries are ignored, and an empty result yields false.
