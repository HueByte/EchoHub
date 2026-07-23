# DatabaseSetup

> **File:** `src/EchoHub.Server/Setup/DatabaseSetup.cs`  
> **Kind:** class

```csharp
public static class DatabaseSetup
```


DatabaseSetup is a startup-time orchestration helper that ensures the database is ready for use by applying migrations, seeding initial data, and performing post-migration data transformations. When you call `InitializeAsync` with an `IServiceProvider`, it creates a scoped container, resolves the required [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) and a logger from `ILoggerFactory`, and then runs three phases: migrate the database (including legacy-handling) via `MigrateAsync`, seed the default channel via `SeedDefaultChannelAsync`, and finally invoke `DataMigrationService.RunAsync` to apply data migrations such as ANSI-to-color-tag conversions.

## Remarks
DatabaseSetup centralizes the startup bootstrap workflow for the database, encapsulating migrations, schema upgrades, seeding, and legacy handling behind a single entry point. It coordinates collaborators like [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) and [`DataMigrationService`](DataMigrationService.cs.md), and uses a scoped `IServiceProvider` so bootstrapping code does not leak scoped lifetimes to the caller. The legacy handling path ensures a clean migration story for older SQLite databases by backing up the file when a legacy schema is detected and then recreating the database with migrations support.

## Notes
- Legacy backup: If a legacy SQLite database is detected, the code may back up the original file to a path like `{dbPath}.legacy_{timestamp}` before deletion. This preserves a recoverable snapshot when possible.
- Startup failure: If `MigrateAsync` fails, the exception is logged and rethrown, which can cause startup to fail so the issue is addressed before the app runs.
