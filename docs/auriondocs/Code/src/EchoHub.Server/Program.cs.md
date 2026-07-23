# Program

> **File:** `src/EchoHub.Server/Program.cs`  
> **Kind:** file


Bootstraps and hosts the EchoHub.Server application as a resilient, self-hosting startup routine. It is the entrypoint that wires configuration, logging, data access, authentication, and service registrations, then starts the ASP.NET Core web host inside a self-healing loop that restarts on startup failures.

## Remarks
Program serves as the composition root of EchoHub.Server. It orchestrates essential cross-cutting concerns—initial configuration via FirstRunSetup, bootstrap logging, server-logs integration with Serilog, data access via EF Core SQLite, and authentication via JWT—by registering the relevant services and options early in the host lifecycle. The self-restarting loop provides resilience during startup and deploy-time hiccups, ensuring the server recovers automatically while debugging and monitoring can observe repeated failures. It ties together the server's operational concerns and acts as the single entry point that other components rely upon to start the web application.

## Notes
- The startup loop restarts the host on failures, enabling resilience but potentially causing rapid churn if issues persist; external monitoring is recommended to detect persistent problems.
- Jwt:Secret must be configured; missing configuration leads to startup failure (an InvalidOperationException is thrown during startup).
- The app uses SQLite by default (echohub.db) located in AppContext.BaseDirectory; ensure filesystem permissions and migrations are properly managed in deployment.