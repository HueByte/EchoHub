# UserStatus

> **File:** `src/EchoHub.Core/Models/UserStatus.cs`  
> **Kind:** enum

```csharp
public enum UserStatus
{
    Online,
    Away,
    DoNotDisturb,
    Invisible
}
```


Represents a user's presence status within the application, guiding UI rendering, presence-based filtering, and notification behavior. The enum exposes four discrete states: `Online`, `Away`, `DoNotDisturb`, and `Invisible` to express typical availability scenarios.

## Remarks
Centralizing presence into `UserStatus` prevents scattered string values or boolean flags across the codebase, promoting consistent semantics for how users are shown and how presence-related logic runs. It also future-proofs the API by allowing new statuses to be added without changing call-sites that consume the type. This enum typically intersects with UI components that render status indicators and with services that filter or route behavior based on a user's current state.

## Notes
- Changing the set of statuses (adding/removing/reordering enum members) is a breaking change that can affect serialization, persistence, and cross-boundary API compatibility; prefer backward-compatible extensions by adding new members rather than reordering existing ones.