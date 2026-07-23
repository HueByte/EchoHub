# Program

> **File:** `src/EchoHub.Client/Program.cs`  
> **Kind:** file


The Program file serves as the application's entry point and startup bootstrap for the EchoHub client. It coordinates early startup tasks such as rollback handling, permission checks, configuration provisioning, logging setup, PATH preparation, post-update cleanup, and UI initialization before handing control to the main orchestrator and theme system.

## Remarks
It functions as a central bootstrap that hides cross-cutting concerns from downstream components. By coordinating UpdateBackupService for rollback support, PathSetup for PATH hygiene, and ThemeManager for theming, it decouples startup sequencing from the rest of the application and ensures the runtime begins in a well-defined state.

## Notes
- Rollback path exits the process after attempting a restore; normal startup does not proceed.
- If appsettings.json is missing, the code seeds it from an embedded example; if the resource isn't available, startup continues with defaults.
- Several operations are best-effort and exceptions are swallowed to avoid stopping startup (e.g., Unix permissions adjustments, cleanup of a leftover .old executable).