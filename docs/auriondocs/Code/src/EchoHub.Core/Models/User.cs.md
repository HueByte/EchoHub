# User

> **File:** `src/EchoHub.Core/Models/User.cs`  
> **Kind:** class

```csharp
public class User
```


Represents a user within EchoHub as a domain model that carries identity, profile data, presence state, and moderation flags. Use this class when loading, persisting, or transmitting user information; it requires Username and PasswordHash to be provided, while other fields are optional and can be enriched over time.

## Remarks
Designed as a cohesive aggregate of user-related data, it coordinates with the UserStatus and ServerRole enumerations to express presence and access level. The default Status Online and Role Member reflect typical initial states for a newly created account, while CreatedAt and LastSeenAt record temporal metadata to support auditing and activity tracking. Optional fields such as DisplayName, Bio, NicknameColor, and AvatarAscii enable richer profiles without forcing churn in systems that only care about identity or authentication.