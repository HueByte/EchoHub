# PathSetup

> **File:** `src/EchoHub.Client/Services/PathSetup.cs`  
> **Kind:** class

```csharp
public static class PathSetup
```


PathSetup is a small helper that ensures the application's directory is present on the system PATH so users can run the `echohub` CLI from any terminal without specifying the full path. The public entry point, `EnsureOnPath`, checks the current PATH and, if the app directory isn't already included, updates PATH in a platform-appropriate way: Windows adds the directory to the user-level PATH, while Unix-like systems append an export line to common shell profile files. The implementation derives the target directory from `AppContext.BaseDirectory`, normalizes path separators, and gracefully handles failures by logging at the debug level if PATH modification cannot be completed.

## Remarks
PathSetup centralizes platform-specific PATH augmentation behind a simple, testable API. It makes the side-effect of PATH modification explicit and isolated from business logic, reducing duplication and potential inconsistencies across the codebase. The class uses an idempotent approach: it first checks whether the directory is already on PATH and only proceeds if needed. On Unix-like systems, it uses a persistent marker (`# Added by EchoHub`) to identify its export line in shell profiles, and it guards against duplicating entries. The combination of platform-specific handling, guarded writes, and informative logging ensures predictable behavior during installation and first-run setup while minimizing surprises for end users.

## Example
```csharp
// Typical usage during installation or first-run setup
PathSetup.EnsureOnPath();
```

## Notes
- On Windows, the path update affects only the current user by modifying the user PATH environment variable, avoiding system-wide changes.
- On Unix-like systems, the code appends a PATH export line to common shell profiles (``.profile``, ``.bashrc``, ``.zshrc``); it skips profiles that already contain the app directory and creates ``~/.profile`` as a fallback when no profiles exist.
- A persistent marker (``# Added by EchoHub``) helps avoid duplicating the export line on repeated runs.
- The operation is best observed after restarting terminals or re-sourcing profiles; until that point, newly opened sessions may not reflect the updated PATH.
