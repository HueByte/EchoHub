# ServerController

> **File:** `src/EchoHub.Server/Controllers/ServerController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
```


ServerController is an ASP.NET Core API controller that exposes the EchoHub server's administrative surface: endpoints to fetch live server statistics, retrieve the configured encryption key (when authorized), and inspect the directory registration state without exposing the claim token itself. Use it when you need operational visibility or admin actions, rather than wiring multiple components yourself.

## Remarks

This symbol acts as a unified HTTP boundary for server-wide concerns, coordinating three collaborators: EchoHubDbContext for live data (Users and Channels), IConfiguration for server configuration (name, description, and registration mode), and DirectoryClaimStore for directory registration state. The private GetCallerAsync helper centralizes authentication and role checks, ensuring privileged endpoints (e.g., GetDirectoryStatus) are accessible only to Admins. By composing a ServerStatusDto from runtime metrics and configuration-derived values, the controller provides a lightweight, admin-focused surface without leaking sensitive tokens.

## Notes

- GetEncryptionKey returns 503 if Encryption:Key is not configured on the server, signaling that encryption readiness is unavailable.
- GetDirectoryStatus is admin-only; if the caller lacks Admin rights, the endpoint yields an Unauthorized/403 response via GetCallerAsync.
- GetCallerAsync enforces authentication by reading the NameIdentifier claim, loading the user from the database, and validating their ServerRole; failures surface as Unauthorized or 403 with a clear message.
