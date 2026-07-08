# JwtTokenService

> **File:** `src/EchoHub.Server/Auth/JwtTokenService.cs`  
> **Kind:** class

```csharp
public class JwtTokenService
```


JwtTokenService centralizes the creation of JSON Web Tokens used to authenticate and authorize requests in EchoHub. It exposes two GenerateAccessToken overloads—one that accepts a User and another that accepts a UserProfileDto—so you can issue a signed access token regardless of which user representation you have at hand. The tokens include standard claims (sub as the user id, username, display_name, and role) and a unique jti, are signed with a symmetric key derived from configuration, and expire after a short, fixed lifetime (15 minutes). A companion GenerateRefreshToken method provides cryptographically random refresh tokens, and HashToken returns a SHA-256 hash of a token, suitable for persisting a token representation without storing the raw value. Use this service as the single place in your authentication flow to issue tokens after login or refresh, rather than building tokens ad hoc.

## Remarks

JwtTokenService centralizes issuing JWT access tokens for authenticated users, ensuring consistent signing, claims, and expiration across different user representations. It reads configuration keys for issuer, audience, and secret and offers two overloads to generate the same token structure from User or UserProfileDto. This isolation makes it easier to swap signing algorithms or adjust claims without touching callers, and it enforces a single, consistent token format across the application.

## Notes

- Missing configuration keys cause the constructor to throw InvalidOperationException; ensure Jwt:Secret, Jwt:Issuer, and Jwt:Audience are configured.
- Access token lifetime is fixed at 15 minutes via a private static constant; to change it, modify the code or recompile.
- GenerateRefreshToken uses 64 cryptographically secure random bytes and is Base64-encoded; treat the raw value as sensitive and store only a hashed representation when persisting.
