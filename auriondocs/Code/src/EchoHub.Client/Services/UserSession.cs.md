# UserSession

> **File:** `src/EchoHub.Client/Services/UserSession.cs`  
> **Kind:** class

```csharp
internal sealed class UserSession
```


Stores the current user's session state on the client, including username, online status, and an optional status message. Use this type as a lightweight, centralized container when you need to read or mutate the ephemeral session data for the active user, and call Reset to return all fields to their defaults (empty username, Online status, and no status message).

## Remarks
Internally sealed and non-public, this class keeps the session representation stable within the client service layer and prevents inheritance. It relies on the UserStatus enum from the core models to express the user's current state consistently across the application.

## Notes
- Not thread-safe by default; coordinate concurrent access if used from multiple threads.
- Reset mutates state in place; if you require preserving data, capture it before calling Reset.
- StatusMessage is nullable; null indicates that no message is provided.