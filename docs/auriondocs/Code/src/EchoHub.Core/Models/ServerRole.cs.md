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


Represents the role assigned to a member within a server context in EchoHub. It defines four distinct levels of authority: Member, Mod (moderator), Admin, and Owner. Use this enum whenever you need to distinguish capabilities, gate UI or actions, or persist role information instead of relying on magic numbers.

## Remarks
By centralizing roles in a single enum, the codebase can map each role to its corresponding permissions in one place, enabling consistent authorization checks across services. The explicit integer values also support stable serialization and interop when persisting or transmitting role data, without forcing string-based representations.

## Example
```csharp
var role = ServerRole.Admin;
switch (role)
{
    case ServerRole.Owner:
    case ServerRole.Admin:
        // elevated permissions
        break;
    case ServerRole.Mod:
        // moderation tasks
        break;
    case ServerRole.Member:
        // regular user actions
        break;
}
Console.WriteLine($"User role: {role}"); // prints Owner, Admin, Mod, or Member
```

## Notes
- Do not treat ServerRole as a Flags enum; do not combine roles with bitwise operators.
- Prefer using the named constants in checks; avoid relying on numeric ordering for access decisions.
- Changing the underlying values (0–3) can affect serialized data; coordinate evolution across all consumers to preserve compatibility.