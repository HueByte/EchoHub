# ServerController

> **File:** `src/EchoHub.Server/Controllers/ServerController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
```


ServerController is an ASP.NET Core API controller that exposes server-related admin endpoints under `/api/server`. It assembles live server state by querying the database context for `Users` and `Channels`, reading server metadata from `IConfiguration` (name, description, and registration mode), and deriving the server version from the executing assembly. It returns a `ServerStatusDto` via the `GetInfo` endpoint. The protected endpoints `GetEncryptionKey` and `GetDirectoryStatus` require authentication (and admin privileges for directory status) and return either the configured encryption key or directory-registration state, respectively, without exposing the claim token. A small helper, `GetCallerAsync`, enforces the required role before performing admin-only operations.

## Remarks
ServerController acts as a focused orchestration boundary that surfaces operator-facing server state by weaving together data from the data layer, configuration, and directory claim store. It centralizes admin concerns (health, configuration, and directory registration) behind clear HTTP endpoints, enabling simple client UIs and tooling. The design emphasizes guarded access for sensitive data (encryption keys and directory status) and relies on role-based checks to restrict those capabilities to admins.

## Notes
- Access to `/api/server/encryption-key` and `/api/server/directory` is protected by authentication; admins only for the latter.
- The code path for `GetCallerAsync` relies on the `NameIdentifier` claim being a valid GUID; malformed claims could cause an exception at runtime.
- If encryption is not configured, `/api/server/encryption-key` responds with HTTP 503 to indicate the service is not ready.