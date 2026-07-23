# DatabaseSetup

> **File:** `src/EchoHub.Server/Setup/DatabaseSetup.cs`  
> **Kind:** class

```csharp
public static class DatabaseSetup
```


DatabaseSetup is a startup bootstrapper that ensures the EchoHub database is ready by applying migrations, seeding a default channel, and running data migrations. It creates a scoped DbContext and logger, migrates the database, seeds a default channel when missing, and then triggers data migrations; if a legacy SQLite database is detected (no migrations history but legacy tables exist), it backs up the current file and recreates the database to enable the modern migration path.

## Remarks
DatabaseSetup centralizes the one-time bootstrap concerns for the database, isolating migration, seeding, and legacy-handling logic from the rest of the startup flow. It relies on EF Core’s migration pipeline and coordinates with DataMigrationService to perform data transformations (e.g., ANSI-to-color-tag conversions) and to ensure essential defaults (like the General channel) exist, aligning the persisted state with the application's current expectations.

## Notes
- Legacy-path destructive behavior: when a legacy database is detected, the code creates a timestamped backup and then deletes the database so migrations can proceed against a fresh schema. This trade-off is intentional to enable a safe migration path from older schemas.
- Startup-time invocation: the initialization runs at application startup and establishes its own service scope; avoid multiple concurrent invocations to prevent duplicate work or conflicting migrations during a single process lifecycle.