# RefreshToken

> **File:** `src/EchoHub.Core/Models/RefreshToken.cs`  
> **Kind:** class

```csharp
public class RefreshToken
```


RefreshToken is a data model that represents a stored refresh token for a user. It encapsulates the token's identity, its secure hash, the owning user reference, and lifecycle information (expires at and potential revocation), plus convenience predicates for common checks (IsExpired, IsRevoked, IsActive) used in the refresh flow.

## Remarks
This type centralizes the lifecycle of refresh tokens in the identity system. The TokenHash stores a cryptographic hash of the token rather than the plaintext, reducing exposure if the data store is compromised. The computed properties IsExpired, IsRevoked, and IsActive reveal the token's current viability without requiring business logic to re-derive state; together with ExpiresAt and RevokedAt they enable straightforward authorization checks and auditability.

## Notes
- The IsActive property is true only when the token is not expired and not revoked, making it a convenient single check in refresh flows.
- CreatedAt is initialized to the current UTC time when a new instance is created, aiding auditing and ordering.