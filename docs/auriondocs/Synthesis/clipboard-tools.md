# Clipboard utilities

> Clipboard helpers for files and images used in the UI.

This topic covers small, focused helpers that let the UI treat clipboard contents as first-class attachments: one helper extracts file paths from a file-list clipboard, another canonicalizes whatever image bytes are available into a PNG, and the window UI calls them when the user pastes or triggers clipboard-driven actions. The helpers centralize platform differences (Windows, Linux, macOS fallbacks) and intentionally present a simple success/failure API so the UI can degrade gracefully when clipboard content or platform support is missing.

## ClipboardFiles.cs
Provides clipboard file handling utilities.

The [ClipboardFiles](../Code/src/EchoHub.Client/Services/ClipboardFiles.cs.md) type is a static helper that exposes a single, simple consumption pattern: call TryGetFiles to ask the OS clipboard for a list of file paths. TryGetFiles returns true only when one or more existing file paths are discovered; it filters out non-existent or malformed entries and returns false on platforms that don’t support a file-list clipboard or when no valid paths are present. Internally the helper normalizes platform differences (CF_HDROP on Windows, text/uri-list on Linux using command-line helpers) and logs exceptions rather than throwing, so callers receive a boolean+list result they can act on without having to catch clipboard-specific exceptions. In the app this helper is consumed by the UI layer: the [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) calls into ClipboardFiles.TryGetFiles to obtain file paths to be staged or attached.

## ClipboardImage.cs
Provides clipboard image utilities.

The [ClipboardImage](../Code/src/EchoHub.Client/Services/ClipboardImage.cs.md) static class exposes TryGetPng to produce a canonical PNG byte array from whatever image representation the OS clipboard currently holds. TryGetPng dispatches platform-specific work to methods such as TryGetWindows, TryGetLinux, or TryGetMacOS based on runtime OperatingSystem checks, preserves native PNG clipboard bytes when present (using PngMagic or platform-registered formats), and converts other formats — notably DIB/CF_DIB on Windows — by wrapping the DIB in a minimal BMP and using an image loader to re-encode as PNG via the DibToPng helper. The API favors robustness: all clipboard- and image-decoding exceptions are logged and swallowed so callers get a simple true/false outcome, and the docs call out platform and threading caveats (for example, STA requirements on Windows and header validation for DIB inputs). The [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) uses ClipboardImage.TryGetPng when it needs a pasteable PNG payload from the clipboard for staging or insertion.

## MainWindow.cs
`MainWindow` collaborates directly with `ClipboardFiles` and other members of this topic (2 dependency links).

The [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) source defines the interactive UI surface and numerous event handlers and helpers related to input, message lists, and clipboard interactions. Of particular relevance to this topic are methods such as GuardedClipboardAction, StageFiles, SetStagedAttachments, and CopyToClipboard: GuardedClipboardAction is the safe wrapper for performing clipboard operations (honoring the helpers’ failure semantics), StageFiles and SetStagedAttachments are the paths by which file lists or image bytes obtained from the clipboard are moved into the UI’s pending-attachment state, and CopyToClipboard implements copy behavior the UI exposes. When a paste or clipboard-driven accept occurs, MainWindow calls into the clipboard helpers — invoking [ClipboardImage](../Code/src/EchoHub.Client/Services/ClipboardImage.cs.md).TryGetPng to request a PNG payload or [ClipboardFiles](../Code/src/EchoHub.Client/Services/ClipboardFiles.cs.md).TryGetFiles to obtain file paths — and then uses its staging methods to present those attachments to the rest of the UI or to the send/attach pipeline.

How the pieces fit

MainWindow is the orchestrator: on paste or clipboard actions it uses GuardedClipboardAction to safely call the two helpers and translate their boolean+payload results into staged attachments (files or PNG bytes). The clipboard helpers isolate platform differences and error handling so the window code only needs to check success/failure and process the returned paths or bytes. This keeps clipboard I/O contained in small, testable utilities while the window code focuses on user flow and attachment lifecycle.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:33:02 UTC*
