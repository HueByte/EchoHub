# User

> **File:** `src/EchoHub.Core/Models/User.cs`  
> **Kind:** class

```csharp
public class User
```


Represents a user entity in the EchoHub domain, aggregating identity, profile, presence, and lifecycle data. It is the primary model used when creating, retrieving, and persisting user information, with required credentials enforced at construction via the `required` modifiers on `Username` and `PasswordHash`.

## Remarks
Designed to be a single source of truth for user state, it coordinates authentication, authorization via `Role`, and moderation flags such as `IsMuted` and `IsBanned`. The default values — `Status` set to `UserStatus.Online`, `Role` set to `ServerRole.Member`, and timestamps on creation — provide sensible startup behavior while keeping optional fields available for richer profiles. It serves as the canonical user payload across core services and data stores, reducing duplication and drift between layers.

## Example
```csharp
var user = new User
{
    Id = Guid.NewGuid(),
    Username = "jdoe",
    PasswordHash = "pbkdf2$...",
    DisplayName = "Jane Doe",
    Status = UserStatus.Online,
    Role = ServerRole.Member
};
```

## Notes
- The `required` modifier on `Username` and `PasswordHash` enforces initialization when constructing a `User` via object initializers (compile-time check).
- `CreatedAt` and `LastSeenAt` default to the moment of object creation but may be replaced by deserialized data from storage.
- `MutedUntil` is meaningful only when `IsMuted` is true; it can be null if not muted.
