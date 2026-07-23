# ServerRole

> **File:** `src/EchoHub.Core/Models/ServerRole.cs`  
> **Kind:** enum

```csharp
public enum ServerRole
{
    Member = 0,
    Mod = 1,
    Admin = 2,
    Owner = 3
}
```


Represents the role a user holds within a server in EchoHub. It categorizes users into distinct permission tiers: `Member`, `Mod`, `Admin`, and `Owner`, which are used to drive authorization and feature availability without scattering numeric checks throughout the codebase.

## Remarks
This enum provides a stable abstraction for role-based access control, allowing components to reason about capabilities (moderation, configuration, ownership) by comparing against `ServerRole` values. Centralizing roles reduces duplication of permission logic and helps ensure consistent authorization across command handlers, UI components, and services. It also offers an extension point: adding a new role or reordering the hierarchy can be localized to this enum and its consumers.