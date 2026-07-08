# DatabaseSetup

> **File:** `src/EchoHub.Server/Setup/DatabaseSetup.cs`  
> **Kind:** class

```csharp
public static class DatabaseSetup
```


DatabaseSetup is a startup bootstrapper that ensures EchoHub's database is ready before the application proceeds. Its InitializeAsync method creates a short-lived DI scope, resolves the DbContext and a logger, applies migrations, seeds a default channel, and triggers data migrations via DataMigrationService.

## Remarks
By centralizing migration, legacy-database handling, and initial data seeding, this class encapsulates startup persistence concerns and reduces coupling elsewhere in the codebase. It relies on a scoped service lifetime to properly dispose the DbContext and related services after use, and it uses HubConstants.DefaultChannel to seed the initial channel. The static design reflects its one-time bootstrap role during application startup, not per-request work.

## Notes
- Legacy-database handling is potentially destructive: if a legacy SQLite database is detected (no EF migrations history) and legacy tables exist, the code may backup the database file (when possible) and recreate the database to enable migrations. This behavior is logged and intended to facilitate forward compatibility, but is important to understand in environments with historic data.
- If migrations fail, the exception is logged and rethrown, which will surface startup failures to the host. Depending on deployment needs, callers may wish to handle startup failures gracefully or retry under controlled conditions.