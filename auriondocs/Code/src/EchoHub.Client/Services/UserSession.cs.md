# UserSession

> **File:** `src/EchoHub.Client/Services/UserSession.cs`  
> **Kind:** class

```csharp
internal sealed class UserSession
```


Represents the current user\'s session state within the client, encapsulating the `Username`, the presence `Status` from [`UserStatus`](../../EchoHub.Core/Models/UserStatus.cs.md), and an optional `StatusMessage`. It is a lightweight in-memory container used by UI and networking layers to track who is logged in and how they present themselves. The `Reset` method reinitializes all fields to their defaults: `Username` to empty, `Status` to `UserStatus.Online`, and `StatusMessage` to `null`.

## Remarks
This small class centralizes session-related data so multiple components can read and update the user\'s identity and presence from a single source of truth. By being `internal` and `sealed`, it communicates that this is an implementation detail of the client assembly and should not be extended or exposed publicly.