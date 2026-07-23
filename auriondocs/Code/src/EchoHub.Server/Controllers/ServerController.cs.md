# ServerController

> **File:** `src/EchoHub.Server/Controllers/ServerController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
```


ServerController is an ASP.NET Core API controller that exposes server-wide information and administrative operations under the `/api/server` route. It wires together runtime configuration, persistence, and directory-state to provide a concise snapshot of the server and a small admin surface for privileged tasks. The public `GetInfo` endpoint returns a [`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) containing the server name, description, user and channel counts, and the current registration mode derived from config. The `GetEncryptionKey` endpoint is protected by `[Authorize]` and returns an [`EncryptionKeyResponse`](../../EchoHub.Core/DTOs/ServerDtos.cs.md) containing the configured key, or a 503 if encryption is not configured. The `GetDirectoryStatus` endpoint is admin-only and surfaces directory registration state, including the server identifier and whether a claim token exists, while never exposing the token itself. A private helper `GetCallerAsync` centralizes authentication and authorization checks for admin actions. 

## Remarks

By centralizing server-wide information and admin operations in a single controller, the architecture cleanly separates concerns: data access ([`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md)), configuration (`IConfiguration`), and directory registration state ([`DirectoryClaimStore`](../Services/DirectoryClaimStore.cs.md)) are coordinated behind stable, contract-driven DTOs ([`ServerStatusDto`](../../EchoHub.Core/DTOs/ServerDtos.cs.md), [`EncryptionKeyResponse`](../../EchoHub.Core/DTOs/ServerDtos.cs.md)). Authorization boundaries are explicit: open information through `GetInfo`, authenticated access for the encryption key, and admin-only access for directory status. The internal `GetCallerAsync` encapsulates common identity/role validation, reducing duplication and potential security gaps across admin endpoints.

## Notes

- The admin surface is guarded: `GetDirectoryStatus` relies on `GetCallerAsync` to enforce that the caller has at least `ServerRole.Admin`; non-admins will receive an appropriate 403/Unauthorized response. 
- If encryption is not configured on the server, the `GetEncryptionKey` endpoint returns a 503 Service Unavailable, signaling to clients that encryption is not currently available despite the endpoint being accessible.