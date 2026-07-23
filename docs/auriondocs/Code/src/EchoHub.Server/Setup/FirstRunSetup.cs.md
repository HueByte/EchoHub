# FirstRunSetup

> **File:** `src/EchoHub.Server/Setup/FirstRunSetup.cs`  
> **Kind:** class

```csharp
public static class FirstRunSetup
```


FirstRunSetup is a small bootstrap utility that ensures essential configuration exists on the first run of the application. Calling `EnsureAppSettings` will create `appsettings.json` from `appsettings.example.json` if the former is missing, and then guarantee that security-related values are present by generating them when necessary. Specifically, it will ensure the JWT secret at `Jwt.Secret` is non-empty and not a placeholder, and it will ensure an AES encryption key at `Encryption.Key` is present. Generated secrets are cryptographically strong base64 values written back into `appsettings.json`, and progress is reported to the console.

## Remarks
The class centralizes the bootstrapping of critical security configuration, enabling a smooth first-run startup without manual edits. It is designed to be invoked during startup or a dedicated setup routine, populating missing cryptographic material so downstream components can rely on `Jwt.Secret` and `Encryption.Key` being present from the outset. The implementation favors an in-place, file-based approach that aligns with conventional .NET configuration loading, so subsequent code that reads configuration from `appsettings.json` will see the generated values.

## Notes
- IO or JSON parsing/writing operations may throw if the filesystem is inaccessible or the JSON is malformed; there is no explicit exception handling in this bootstrap path.
- The JWT secret is only regenerated if it is missing, empty, or begins with `CHANGE_ME`, preventing accidental overwrites on a healthy existing configuration.
- The encryption key is generated only when the `Encryption.Key` value is absent; existing keys are preserved to avoid needless rotation.
- The logic relies on the current working directory to locate `appsettings.json` and `appsettings.example.json`, so running from an unexpected directory can affect behavior.