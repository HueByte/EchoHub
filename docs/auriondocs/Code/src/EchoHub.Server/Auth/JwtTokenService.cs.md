# JwtTokenService

> **File:** `src/EchoHub.Server/Auth/JwtTokenService.cs`  
> **Kind:** class

```csharp
public class JwtTokenService
```


JwtTokenService centralizes creation of JWTs for API authentication: it issues short-lived access tokens via `GenerateAccessToken` (from a [`User`](../../EchoHub.Core/Models/User.cs.md) or a [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md)) and provides a cryptographically secure refresh token generator via `GenerateRefreshToken` (and `HashToken` to store a hashed form). It reads its secret, issuer, and audience from configuration and enforces token lifetimes defined by `AccessTokenLifetime` and `RefreshTokenLifetime`.