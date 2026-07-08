# ServerController

> **File:** `src/EchoHub.Server/Controllers/ServerController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
```


ServerController is an ASP.NET Core API controller that exposes read-only server state and admin-oriented utilities under /api/server by coordinating EchoHubDbContext, IConfiguration, and DirectoryClaimStore. It provides GetInfo for a quick server summary, GetEncryptionKey for authorized clients to retrieve the encryption key (or a 503 status if not configured), and GetDirectoryStatus for admins to inspect directory registration state without leaking the claim token; a private GetCallerAsync centralizes authentication and authorization checks to enforce the minimum ServerRole consistently across admin endpoints.

## Remarks
GetCallerAsync encapsulates authentication and authorization logic used by admin-protected endpoints, ensuring consistent enforcement of the minimum ServerRole without duplicating checks. The controller acts as a thin façade that aggregates data from the database, configuration, and the directory claim store to present a coherent view of server state while keeping sensitive details, like the claim token, out of responses.

## Notes
- Be mindful that GetCallerAsync uses Guid.Parse on the NameIdentifier claim; if the claim is present but not a valid GUID, this can throw.
- The EncryptionKey endpoint returns 503 when encryption is not configured; clients should handle this as a temporary unavailability signal.
- Fetching user and channel counts via CountAsync can be expensive on large tables; consider caching or rate-limiting such queries if this endpoint is invoked frequently.