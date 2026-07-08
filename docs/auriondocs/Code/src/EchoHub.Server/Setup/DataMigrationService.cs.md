# DataMigrationService

> **File:** `src/EchoHub.Server/Setup/DataMigrationService.cs`  
> **Kind:** class

```csharp
public static partial class DataMigrationService
```


DataMigrationService is a startup-time orchestrator that coordinates a small set of one-off migrations to normalize the database state before the application handles regular requests. Its RunAsync method creates a DI scope, retrieves the EchoHubDbContext, IConfiguration, and an ILogger, then sequentially applies the defined migrations: ensuring the default #general channel is public, migrating legacy ANSI color codes in image messages to a color-tag representation, converting embed JSON to an array format, and enforcing configured admins. This centralizes initialization-time data changes behind a single entry point so downstream logic can rely on a consistent data model after startup.

## Remarks
This class acts as a bootstrapper for startup-time migrations, encapsulating the concerns behind a single entry point and leveraging dependency injection for testability and traceability. Each migration is guarded with checks and logs its progress, which helps keep startup predictable and auditable. The ANSI-to-color-tags migration preserves message presentation while modernizing storage, and the admin-enforcement step provides a safety net for bootstrap users defined in configuration.

## Notes
- The migration routines are designed to be safe to re-run on startup and avoid unnecessary database writes when no changes are needed.
- Changes are logged at Information level to aid diagnostics without overwhelming normal operation logs.
- The code paths assume existing data shapes (e.g., a #general channel) and configuration sections (Server:Admins).