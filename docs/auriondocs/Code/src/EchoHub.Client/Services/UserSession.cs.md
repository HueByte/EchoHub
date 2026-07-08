# UserSession

> **File:** `src/EchoHub.Client/Services/UserSession.cs`  
> **Kind:** class

```csharp
internal sealed class UserSession
```


Holds the current user's session state (username, status, status message) as a lightweight, in-memory container used by the client to maintain a cohesive view of the active user. Username defaults to an empty string, Status defaults to Online, and StatusMessage is nullable. Reset clears all fields back to these defaults. This class centralizes the local user context to reduce scattered state across UI and networking code, and it can be exoticized by higher-level services that own the session lifecycle.

## Remarks
This abstraction provides a single, mutable representation of the local user identity and presence, decoupling various components from directly handling individual session fields. Being internal to the assembly, it is intended for use within the client layer; external consumers should rely on higher-level services to manage user session interactions. The Reset method offers a convenient reinitialization path (for example, after sign-out or user switch) to restore the default state.

## Notes
- UserSession is mutable and not inherently thread-safe. If the instance is shared across threads, external synchronization is required.
- Username defaults to an empty string and StatusMessage is nullable, so callers should handle the not-signed-in or no-message cases explicitly.
