# RefreshToken

> **File:** `src/EchoHub.Core/Models/RefreshToken.cs`  
> **Kind:** class

```csharp
public class RefreshToken
```


RefreshToken is a persistence model that represents a refresh token tied to a user. It stores a hashed token (TokenHash), the associated user via UserId, and validity information such as ExpiresAt and CreatedAt (which defaults to the current UTC time), plus an optional RevokedAt timestamp. It exposes IsExpired, IsRevoked, and IsActive to quickly assess the token’s state. A developer would create and persist these tokens when issuing refresh tokens in an authentication flow, check IsActive (or IsExpired/IsRevoked) when validating a refresh attempt, and use RevokedAt to mark a token as revoked.

## Remarks
This class serves as a persistence-facing token entity with a foreign key to User and a corresponding navigation property, enabling lifecycle management (creation, expiry, revocation) at the data layer while providing simple state checks for business logic.