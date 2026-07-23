# FirstRunSetup

> **File:** `src/EchoHub.Server/Setup/FirstRunSetup.cs`  
> **Kind:** class

```csharp
public static class FirstRunSetup
```


FirstRunSetup is a small bootstrap utility that guarantees a usable appsettings.json and seeds it with cryptographic secrets on first run. On startup, it copies appsettings.example.json to appsettings.json if the destination is missing, then ensures a valid JWT secret and an encryption key exist by generating them when needed.

## Remarks
Centralizing this bootstrap logic keeps startup concerns cohesive and makes the secrets generation deterministic and auditable. It relies on cryptographically secure RNG and writes back to the configuration file with indentation for human readability, while tolerating JSON comments during read.

## Example
```csharp
// Typical usage during application startup
FirstRunSetup.EnsureAppSettings();
```

## Notes
- Idempotent: existing Jwt.Secret or Encryption.Key are preserved; new values are generated only if missing or marked as CHANGE_ME.
- Path dependency: relies on the current working directory (the project root). In non-standard deployments, you may need to adjust the working directory or extend the helper to accept explicit paths.
- Silent on parse failure: if the JSON cannot be parsed (root is null), the method returns without writing changes.