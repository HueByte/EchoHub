# AuthController

> **File:** `src/EchoHub.Server/Controllers/AuthController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
```


AuthController is the HTTP boundary for the API's authentication flows. It coordinates user registration, login, refresh-token rotation, and logout by delegating to IUserService for user management, JwtTokenService for token generation, and the EchoHubDbContext for persisting refresh tokens. Clients interact with the endpoints under api/auth to obtain access tokens and refresh tokens, or to refresh the session without re-entering credentials.

## Remarks
AuthController centralizes the authentication policy of the API. It binds together user management, token generation, and token persistence, ensuring a consistent security model across all auth operations. By performing refresh-token rotation and revocation within this controller, it reduces the risk of token reuse and keeps token lifetimes aligned with JwtTokenService settings.

## Notes
- Refresh tokens are stored as hashed values in the database; the raw token is never persisted, and tokens are compared via their hashes.
- On refresh, the old refresh token is revoked (rotation) and a new token pair is issued to prevent token-reuse.
- The endpoints are rate-limited under the "auth" policy, and changes to JwtTokenService configuration (token lifetimes) affect all clients consistently.