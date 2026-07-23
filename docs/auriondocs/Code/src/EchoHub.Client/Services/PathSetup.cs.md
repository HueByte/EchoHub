# PathSetup

> **File:** `src/EchoHub.Client/Services/PathSetup.cs`  
> **Kind:** class

```csharp
public static class PathSetup
```


PathSetup is a cross-platform helper that ensures the application's directory is present on the system PATH, enabling commands like echohub to be run from any terminal session without specifying the full path. EnsureOnPath checks for the directory and, if missing, updates PATH in a platform-appropriate way: Windows updates the user PATH; Unix-like systems append an export line to common shell profile files.

## Remarks
By centralizing PATH manipulation, this abstraction reduces code duplication and the risk of divergent PATH states across platforms. It uses a lightweight, best-effort approach and logs outcomes to aid diagnostics when PATH updates fail or are skipped. The addition is clearly marked by a PathMarker to avoid duplicating lines in shell profiles.

## Example
```csharp
PathSetup.EnsureOnPath();
```

## Notes
- The method swallows exceptions and logs at debug level, so callers should not rely on exceptions to signal failure.
- Unix updates affect the user's shell environment; new terminal sessions are typically required to observe changes.
- Windows updates are done at the per-user level; system-wide PATH is not modified.