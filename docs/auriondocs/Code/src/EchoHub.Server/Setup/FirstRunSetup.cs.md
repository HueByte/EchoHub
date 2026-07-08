# FirstRunSetup

> **File:** `src/EchoHub.Server/Setup/FirstRunSetup.cs`  
> **Kind:** class

```csharp
public static class FirstRunSetup
```


Bootstrapper for the application's configuration on first run. FirstRunSetup.EnsureAppSettings ensures appsettings.json exists (creating it from appsettings.example.json when present) and then guarantees that a valid JWT secret and an encryption key are stored in the configuration, generating cryptographically strong values when they are missing. This method is intended to be called during startup to bootstrap secure defaults without requiring manual file edits.

## Remarks

Centralizes first-run initialization and makes the startup process idempotent: repeated runs won't overwrite existing secure values. It reads and updates appsettings.json via JsonNode, creating Jwt and Encryption sections as needed while respecting existing settings. It uses cryptographically secure random bytes to populate secrets and keys and logs its actions to the console to aid troubleshooting.

## Example

```csharp
// Typical usage during application startup
FirstRunSetup.EnsureAppSettings();
```

## Notes
- It mutates appsettings.json, creating missing sections and keys as needed, based on the current content root.
- It only generates a new secret if the current one is missing, empty, or begins with "CHANGE_ME"; otherwise existing valid values are preserved.
- If appsettings.example.json is absent or unreadable, the bootstrap path may skip creation, and the method will proceed to the next steps only if appsettings.json exists.
