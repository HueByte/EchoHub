# User

> **File:** `src/EchoHub.Core/Models/User.cs`  
> **Kind:** class

```csharp
public class User
```


The User class is a domain model that represents a person using EchoHub, encapsulating identity (Id, Username, PasswordHash), profile details (DisplayName, Bio, NicknameColor, AvatarAscii), presence (Status, StatusMessage), role-based access (Role), moderation flags (IsMuted, MutedUntil, IsBanned), and auditing timestamps (CreatedAt, LastSeenAt). Username and PasswordHash are required to create a usable user, while other fields are optional to support rich profiles; defaults establish an online, member-facing user with current timestamps when a new instance is created.

## Remarks

This class serves as a central data container used across authentication, user management, presence rendering, and authorization checks. It’s designed to be lightweight and serializable for persistence, while keeping domain concerns cohesive with a single user entity. The defaults for Status and Role, along with the auditing timestamps, provide a sensible initial state for newly created users.

## Notes

- The required fields (Username and PasswordHash) enforce that essential credentials are provided when constructing a user instance.
- PasswordHash should be treated as sensitive data; avoid exposing it in logs or API responses and ensure the persistence layer handles security appropriately.
- If hydrating from storage, ensure CreatedAt and LastSeenAt reflect the persisted values rather than new defaults.
