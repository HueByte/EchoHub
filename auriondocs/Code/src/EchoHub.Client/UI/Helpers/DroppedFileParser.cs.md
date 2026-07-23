# DroppedFileParser

> **File:** `src/EchoHub.Client/UI/Helpers/DroppedFileParser.cs`  
> **Kind:** class

```csharp
public static class DroppedFileParser
```


DroppedFileParser exposes a small, focused set of helpers for recognizing and extracting absolute file path(s) from terminal input that arrives via drag-and-drop. It understands common path forms (quoted text, Windows drive-letter paths like `X:\`, UNC paths like `\\server\share`, and POSIX absolute paths starting with `/`) and uses a cheap pre-check (`LooksLikePath`) to avoid filesystem access unless the input plausibly contains a path. The primary entry point, `TryGetFiles`, returns true when the input resolves to one or more existing files and returns the discovered paths in the `files` out parameter; it supports a single path (quoted or not) or multiple space-separated tokens (each optionally quoted) and lets callers inject a `fileExists` predicate for testability (defaults to `File.Exists`).

## Remarks
DropppedFileParser centralizes the path-detection logic that UI input handlers would otherwise duplicate, simplifying callers and reducing unnecessary file-system work. `LooksLikePath` provides a fast-path signal so the expensive existence check runs only when the input plausibly represents a path, while `TryGetFiles` performs the actual existence checks and returns the concrete file list. The API supports both single-path and multi-path inputs, correctly handling spaces inside quoted paths by tokenizing tokens and stripping surrounding quotes where applicable; it enforces that all tokens are fully-qualified and existing, otherwise the call fails. The `fileExists` parameter makes unit tests deterministic by allowing injection of a fake predicate instead of touching the real filesystem.

## Notes
- Relative paths are not accepted by `TryGetFiles`; it requires fully-qualified paths for each token (and for single-path input).
- Quote handling is strict: [`StripQuotes`](../../Commands/CommandHandler.cs.md) removes matching leading/trailing quotes only when both ends use the same quote character; mismatched quotes may leave quotes in the token and affect parsing.
- For testing, pass a custom `fileExists` delegate to avoid real I/O; otherwise the default uses `File.Exists`.
