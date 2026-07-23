# AuthController

> **File:** `src/EchoHub.Server/Controllers/AuthController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
```


AuthController is the API surface that coordinates user authentication. It exposes endpoints for registering, logging in, refreshing tokens, and logging out under /api/auth, and ties together user management, JWT token generation, and refresh-token persistence.

## Remarks
AuthController centralizes authentication concerns to enable consistent security policies such as token lifetimes and rotation. It orchestrates between user management (IUserService), token generation (JwtTokenService), and persistence of refresh tokens (EchoHubDbContext), including rotation semantics to revoke old tokens on each refresh.

## Notes
- Refresh token rotation: on a successful refresh, the old token is revoked (RevokedAt is set) and a new token pair is issued. Clients should replace the old token with the new one and avoid reusing the former.
- Security handles: access tokens have shorter lifetimes, refresh tokens are hashed in storage, and all token exchanges occur over HTTPS. Treat tokens as highly sensitive data and store them securely on the client side.