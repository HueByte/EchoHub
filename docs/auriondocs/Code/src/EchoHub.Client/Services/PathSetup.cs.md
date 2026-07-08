# PathSetup

> **File:** `src/EchoHub.Client/Services/PathSetup.cs`  
> **Kind:** class

```csharp
public static class PathSetup
```


PathSetup is a small cross-platform utility that guarantees the application's directory is present on the system PATH, allowing the EchoHub executable to be run from any terminal session without manual PATH modification. Call EnsureOnPath during installation or startup to make the runtime location discoverable by users.

## Remarks
PathSetup centralizes platform-specific PATH augmentation behind a single API. On Windows it updates the per-user PATH so only the current user is affected; on Unix-like systems it appends an export line to common shell profiles (.profile, .bashrc, .zshrc) to persist the change across sessions. It avoids duplicating entries by checking the existing PATH and profile contents before writing. Failures are logged but do not throw, making this a best-effort enhancement rather than a hard dependency.

## Example
```csharp
// Typical usage at startup
PathSetup.EnsureOnPath();
```

## Notes
- It mutates the user's PATH and writes to shell profiles, causing the change to persist across sessions.
- It is best-effort: it logs issues and does not throw on failure.
- On Unix, if no profile exists, it creates a .profile to persist the PATH export.
- Potential issue observed in the source: the Unix profiles array is initialized with square brackets in C#; correct syntax should use curly braces. Ensure the code compiles by replacing the array initializer with braces, e.g., string[] profiles = { Path.Combine(home, ".profile"), Path.Combine(home, ".bashrc"), Path.Combine(home, ".zshrc") }; This is a diagnostic note; in docs you can point to the bug as a trap.