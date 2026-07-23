# RefreshToken

> **File:** `src/EchoHub.Core/Models/RefreshToken.cs`  
> **Kind:** class

```csharp
public class RefreshToken
```


Represents a `RefreshToken` that carries the metadata and state needed to sustain a user session via token-based authentication. It encapsulates the token hash, the owning user, expiration, and revocation data, and exposes simple predicates to answer the token's current validity. The `TokenHash` is marked `required`, guaranteeing a hash is provided during initialization; `CreatedAt` records when the token was created (defaulting to `DateTimeOffset.UtcNow`); `ExpiresAt` defines when the token becomes invalid; `RevokedAt` records a revocation timestamp when the token is revoked. The computed properties `IsExpired`, `IsRevoked`, and `IsActive` reflect the token's lifecycle status, so callers can check validity without inspecting each field. The [`User`](User.cs.md) navigation property links the token to its owner for convenient data access in domain services or ORMs.

## Dependencies
- `DateTimeOffset`

## Remarks
Architecturally, this symbol serves as the boundary for token-based authentication in the domain. It centralizes lifecycle logic (expiry and revocation) into a single place, enabling consistent checks via `IsActive` across services. The presence of [`User`](User.cs.md) further supports straightforward navigation to the owner, which is helpful when presenting token data in dashboards or auditing scenarios.

## Example
```csharp
// Example: creating a new `RefreshToken` (TokenHash is required)
Guid userId = Guid.NewGuid();
var token = new RefreshToken
{
    Id = Guid.NewGuid(),
    TokenHash = "sha256-abc123",
    UserId = userId,
    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
};
```

## Notes
- Be aware that `CreatedAt` is initialized to the current UTC time at construction. If you load an existing token from storage, ensure the stored value for `CreatedAt` is preserved.
- `IsActive` depends on both `IsExpired` and `IsRevoked`. If you set `RevokedAt` but forget to update `IsRevoked`, the token might appear active.