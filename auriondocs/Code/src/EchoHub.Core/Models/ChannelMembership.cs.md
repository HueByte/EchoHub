# ChannelMembership

> **File:** `src/EchoHub.Core/Models/ChannelMembership.cs`  
> **Kind:** class

```csharp
public class ChannelMembership
```


ChannelMembership is a lightweight data container that models the association between a user and a channel, recording when the user joined. It is intended for persistence and transport of membership data; instantiate and persist this model when recording channel participation rather than scattering ad-hoc data structures.

## Remarks
ChannelMembership encapsulates the many-to-many relationship between users and channels along with a join timestamp, enabling straightforward CRUD operations, serialization, and display of membership data. As a plain DTO, it contains no behavior beyond storage of UserId, ChannelId, and JoinedAt; it complements User and Channel entities by representing their linkage. The JoinedAt default is DateTimeOffset.UtcNow at construction, which is convenient for new memberships but should be overridden or preserved from storage when loading existing records.

## Example
```csharp
var membership = new ChannelMembership
{
    UserId = Guid.NewGuid(),
    ChannelId = Guid.NewGuid()
    // JoinedAt defaults to DateTimeOffset.UtcNow
};
```

## Notes
- The default JoinedAt value applies only to newly created instances; deserialization from a data store will populate JoinedAt from the stored value.
- This class is a plain data holder with no validation or invariants; enforce domain rules at a higher layer when necessary.