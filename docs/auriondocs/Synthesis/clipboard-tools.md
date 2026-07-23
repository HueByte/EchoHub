# Clipboard utilities

> Helpers for clipboard interactions: files and images.

This guide describes the clipboard-focused utilities in the client: one helper that exposes file-list clipboard contents, another that normalizes image clipboard data into PNG bytes, and the UI entry points that call those helpers to stage attachments or consume images.

## ClipboardFiles.cs
Clipboard file utilities for handling file lists.

The [ClipboardFiles](../Code/src/EchoHub.Client/Services/ClipboardFiles.cs.md) static class provides a single, platform-aware API (exposed via TryGetFiles) to read file paths when the OS clipboard contains a file-list. It hides OS-specific handling—on Windows it reads CF_HDROP with a short retry loop to tolerate clipboard contention, on Linux it uses text/uri-list through wl-paste or xclip—and it performs existence checks and filters out non-file entries so callers receive only existing paths. TryGetFiles returns true only when at least one valid file path is found, otherwise false, allowing callers to fall back if no usable file-list is present; this class is consumed by the UI layer ([MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md)).

## ClipboardImage.cs
Clipboard image utilities for copying images to the clipboard.

The [ClipboardImage](../Code/src/EchoHub.Client/Services/ClipboardImage.cs.md) static class exposes TryGetPng to extract whatever image is currently on the OS clipboard and return it as PNG-encoded bytes suitable for saving, embedding, or transmitting. It normalizes multiple clipboard image formats: it prefers a native PNG clipboard format to preserve alpha, and falls back to platform bitmaps (CF_DIB on Windows) by wrapping DIB bytes in a minimal BMP header and decoding/re-encoding to PNG via DibToPng; malformed DIB input yields null and TryGetPng surfaces that as a failure (false). TryGetPng routes to OS-specific helpers, logs errors rather than throwing, and returns false on unsupported platforms or on failure; [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) depends on this helper to obtain clipboard image bytes.

## MainWindow.cs
`MainWindow` collaborates directly with `ClipboardFiles` and other members of this topic (2 dependency links).

The [MainWindow](../Code/src/EchoHub.Client/UI/MainWindow.cs.md) UI class defines a large set of interactive behaviors and a handful of members that interact with the clipboard: notably methods named StageFiles, SetStagedAttachments and GuardedClipboardAction appear in its surface. Per the documented relationships, MainWindow delegates platform specifics to the clipboard helpers: it invokes [ClipboardFiles](../Code/src/EchoHub.Client/Services/ClipboardFiles.cs.md).TryGetFiles to obtain file paths copied by the user and then uses its own staging APIs (SetStagedAttachments/StageFiles) to prepare those paths for attachment. Likewise, MainWindow can call [ClipboardImage](../Code/src/EchoHub.Client/Services/ClipboardImage.cs.md).TryGetPng to obtain a normalized PNG byte array when the user has copied an image, allowing the UI to save, embed, or attach that image without per-OS handling. GuardedClipboardAction provides a place to centralize error handling and UI feedback around those clipboard calls so failures from the helpers (they return false rather than throwing) can be handled gracefully.

How the pieces fit

- The two service classes encapsulate platform-specific clipboard concerns: [ClipboardFiles] returns a filtered list of existing file paths or false; [ClipboardImage] returns a PNG byte array or false.  
- [MainWindow] orchestrates user-facing clipboard flows: it calls those helpers from StageFiles/SetStagedAttachments and related clipboard actions, then integrates the results into the message-composition and attachment UI.  
- The helpers favor returning a simple success/failure result (and normalized data) so the UI can decide whether to stage attachments, embed image bytes, or fall back to alternative input methods.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:53:51 UTC*
