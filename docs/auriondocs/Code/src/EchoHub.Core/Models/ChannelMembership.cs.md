# ChannelMembership

> **File:** `src/EchoHub.Core/Models/ChannelMembership.cs`  
> **Kind:** class

```csharp
public class ChannelMembership
```


ChannelMembership is a small data model that represents the relationship between a user and a channel, capturing when the membership began. It exposes the identifiers for both sides (UserId and ChannelId) and a JoinedAt timestamp that defaults to the current UTC time when a new instance is created. Use this type whenever you need to persist, transfer, or reason about which users belong to which channels, instead of juggling three separate fields.

## Remarks
By design this is a lightweight POCO with mutable properties, suitable for serialization and ORM scenarios. JoinedAt's default to DateTimeOffset.UtcNow ensures a sane timestamp when memberships are created in-memory, but you should supply the value from storage when loading existing memberships. The class does not enforce uniqueness or referential integrity by itself; those concerns are left to the data store or surrounding services.

## Example
```csharp
var membership = new ChannelMembership
{
    UserId = Guid.NewGuid(),
    ChannelId = Guid.NewGuid(),
    JoinedAt = DateTimeOffset.UtcNow
};
```

## Notes
- JoinedAt defaults to the current UTC time on construction; deserialization may override this value. 
- It is a plain data holder (POCO) with mutable properties, suitable for simple persistence and data transfer scenarios. 
- Be mindful of relying on the default constructor for time-sensitive logic; consider always setting JoinedAt explicitly when accuracy matters.
