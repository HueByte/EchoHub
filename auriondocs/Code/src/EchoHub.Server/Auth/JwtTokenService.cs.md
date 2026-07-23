# JwtTokenService

> **File:** `src/EchoHub.Server/Auth/JwtTokenService.cs`  
> **Kind:** class

```csharp
public class JwtTokenService
```


JwtTokenService centralizes the creation of JSON Web Tokens used for authenticating API requests. It reads the signing secret, issuer, and audience from configuration and exposes two overloads of GenerateAccessToken for User and UserProfileDto, returning the token string along with its expiration timestamp. Each generated token includes standard claims such as sub (the user/profile id), username, display_name (falling back to username if not provided), role, and a unique jti, and is signed with HmacSha256 using the configured secret. Access tokens expire after 15 minutes, while a companion refresh token can be generated with GenerateRefreshToken and hashed with HashToken for secure storage. 

## Remarks
JwtTokenService centralizes token creation, ensuring consistent signing, claims, and expiry semantics across authentication flows. By loading Jwt:Secret, Jwt:Issuer, and Jwt:Audience from configuration in one place, it reduces the risk of mismatched values and scattered configuration access. The two overloads for GenerateAccessToken allow tokens to be produced from either a User or a UserProfileDto while preserving a uniform JWT shape and claims set, including a unique jti for traceability.

## Notes
- If Jwt:Secret, Jwt:Issuer, or Jwt:Audience is missing from configuration, the constructor throws an InvalidOperationException with a clear message, preventing startup with a misconfigured token engine.
- GenerateRefreshToken produces a cryptographically random 64-byte value and returns it as a base64 string; HashToken provides a SHA-256-based digest suitable for secure, persisted storage. The class itself does not persist refresh tokens, so you should implement storage and revocation logic in your authentication flow if needed.
