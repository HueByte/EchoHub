# Program

> **File:** `src/EchoHub.Server/Program.cs`  
> **Kind:** file


Program.cs is the entry point for the EchoHub.Server application. It bootstraps the server by performing the one-time setup via `FirstRunSetup.EnsureAppSettings()`, configuring the bootstrap logger, and then starting the ASP.NET Core host via `WebApplication.CreateBuilder(args)` inside an auto-restart loop. It wires core infrastructure such as [`EchoHubDbContext`](Data/EchoHubDbContext.cs.md) for EF Core and authentication, and server features like [`ServerLogsOptions`](Config/ServerLogsOptions.cs.md)/[`ServerLogsService`](Services/ServerLogs/ServerLogsService.cs.md), enabling the app to recover from startup failures by rebuilding and running the web host repeatedly.