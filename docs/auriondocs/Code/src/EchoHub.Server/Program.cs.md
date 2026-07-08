# Program

> **File:** `src/EchoHub.Server/Program.cs`  
> **Kind:** file


Program is the entry point for EchoHub.Server. It bootstraps app settings, configures logging, and hosts the web application inside a resilient, self-restarting loop. On each iteration it wires up critical startup components (EF Core with SQLite, JWT authentication, controllers, and SignalR), applies host options, and registers middleware, effectively centralizing the server startup and lifecycle management.

## Remarks
Program serves as the central startup orchestrator for EchoHub.Server, coordinating first-run setup, logging bootstrap, database context wiring, authentication, and middleware registrations. By encapsulating the host lifecycle in a loop, it provides a straightforward path to recover from transient startup failures and keeps the server running without manual intervention. This is the primary touchpoint for cross-cutting startup concerns and host lifecycle decisions.

## Notes
- Jwt:Secret must be configured; if absent, startup throws an InvalidOperationException during host construction.
- SignalR authentication token handling: the OnMessageReceived hook demonstrates supplying a JWT via the query string for the chat hub (token extraction logic is shown in the snippet). In production, ensure secure transport and consider token exposure risks when using query-string-based tokens.
- Self-healing loop: the code defines maxConsecutiveFailures and a counter to guard against rapid, repeated restarts; be mindful of how failures are surfaced and monitored during development.
