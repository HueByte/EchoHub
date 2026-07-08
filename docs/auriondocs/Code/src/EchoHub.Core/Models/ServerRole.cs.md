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


Represents the role of a user on the EchoHub server and is used to drive access control decisions. Developers should use ServerRole instead of raw integers when writing permission checks or persisting a user's role, because it provides a clear, type-safe classification for Member, Mod, Admin, and Owner.

## Remarks

By design, the enum assigns explicit integral values (Member = 0, Mod = 1, Admin = 2, Owner = 3) to guarantee stable storage and predictable interop. Append any new roles to the end of the list to preserve backward compatibility and avoid breaking existing serialized data. The ordering enables simple range checks (e.g., role >= ServerRole.Mod) to gate access; if your project requires non-hierarchical permissions, consider a separate flag-based system.

## Example

```csharp
var role = ServerRole.Mod;

if (role >= ServerRole.Mod)
{
    // moderator-level capabilities
}
```

## Notes

- Use the enum instead of magic numbers to improve readability and maintainability.
- Do not rely on the underlying numeric values for logic that must remain stable if roles are reordered or extended.
- If you serialize ServerRole, ensure the same numeric values are preserved across platforms and versions; changing the underlying values or their meaning can break deserialization.