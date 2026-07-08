# Program

> **File:** `src/EchoHub.Client/Program.cs`  
> **Kind:** file


This file is the application's entry point and bootstrapper for the EchoHub client. It coordinates the initial startup sequence, handling early-defense tasks, configuration, logging, and the transition from startup to the main UI/orchestrator flow. Developers reach for this symbol when they need to understand how the application prepares its environment before rendering the user interface and engaging the core UI/orchestrator logic.

## Remarks
The Program centralizes cross-cutting startup concerns (CLI flags, permission checks, default configuration provisioning, and logging) so the rest of the application can rely on a prepared, consistent environment. By performing tasks like rollback handling, post-update housekeeping, and PATH setup up front, it ensures the runtime starts from a known, healthy state and that lifecycle events are recorded early in the process. This separation also keeps UI and business logic focused on their respective responsibilities, with the orchestration and theming wired after the bootstrap steps complete.

## Notes
- Publishing the application as a single-file bundle may require explicit references to Serilog sink assemblies; the startup path relies on Serilog’s configuration discovery, which may not find sinks in a bundled executable unless those assemblies are included.
- The Unix-specific safety checks (e.g., attempting to grant UserExecute permission on the running binary) are best-effort and performed behind try/catch blocks; failures are non-fatal and only affect the defensive setup, not the overall startup flow.