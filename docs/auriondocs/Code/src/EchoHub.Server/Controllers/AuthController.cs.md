# AuthController

> **File:** `src/EchoHub.Server/Controllers/AuthController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
```


AuthController is an API controller that hosts authentication endpoints under `/api/auth`, handling user registration, login, token refresh, and logout. It coordinates user management via [`IUserService`](../../EchoHub.Core/Contracts/IUserService.cs.md), issues access tokens with [`JwtTokenService`](../Auth/JwtTokenService.cs.md), and persists refresh tokens through the application's EF Core context [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md).