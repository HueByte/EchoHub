# Program

> **File:** `src/EchoHub.Client/Program.cs`  
> **Kind:** file


The `Program` file serves as the entry point for the EchoHub client. It bootstraps startup by handling a potential CLI rollback (`--rollback`), performing a best-effort Unix execute-permission check, provisioning configuration (loading from `appsettings.json` with a fallback embedded resource at `EchoHub.Client.appsettings.example.json`), and configuring `Serilog` from the configuration before loading the runtime settings via [`ConfigManager`](Config/ConfigManager.cs.md) and initializing the Terminal.Gui UI with `Application.Create().Init()`.

## Remarks
This file centralizes environment preparation and startup orchestration, encapsulating cross-platform concerns (rollback handling, permission checks, path setup, and post-update housekeeping) so the rest of the application can assume a ready, consistent runtime context. It also exposes a clear, testable bootstrap path that wires configuration, logging, and the UI startup in a single phase, reducing duplication across modules.

## Notes
- Rolling back can terminate startup early because `UpdateBackupService.RestoreBackup()` or subsequent error paths invoke `Environment.Exit`.
- Unix permission checks are best-effort and any failures are swallowed to avoid blocking startup on platform quirks.
- The initial configuration may be sourced from an embedded resource (`EchoHub.Client.appsettings.example.json`) if `appsettings.json` is absent, providing a safe fallback during first-run scenarios.
