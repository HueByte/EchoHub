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


Represents a user's presence state within EchoHub. Use this enum to communicate whether a user is online, away, in Do Not Disturb mode, or invisible to others, without resorting to ad-hoc flags.

## Remarks
By centralizing presence states into a single enum, the system can consistently map state to UI cues (icons, colors) and to business rules (notification delivery, visibility). Invisible allows a user to appear hidden while still connected, a common pattern for privacy-aware presence. The remaining values—Online, Away, and DoNotDisturb—cover typical availability intents and guide both UI presentation and server-side behavior.

## Notes
- Be mindful of evolving the enum: adding values should not break existing serialized data or API contracts; plan for backward compatibility in versioning.
- If this enum is serialized for wire formats (e.g., JSON), prefer the named string representation over numeric values to avoid misinterpretation when values shift across versions.