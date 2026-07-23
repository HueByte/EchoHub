# ChannelMembership

> **File:** `src/EchoHub.Core/Models/ChannelMembership.cs`  
> **Kind:** class

```csharp
public class ChannelMembership
```


The `ChannelMembership` class is a simple data container that models the association between a user and a channel within the EchoHub system. It stores the `UserId`, the `ChannelId`, and the time the membership was created (`JoinedAt`), which defaults to the current UTC time if not specified.

## Remarks
Locates a specific user's membership in a channel and records when it happened. It serves as a lightweight linkage between `UserId` and `ChannelId`, with `JoinedAt` providing a timestamp of when the membership was established.

## Notes
- `JoinedAt` defaults to `DateTimeOffset.UtcNow` at object creation; when loading from a data store this value may be overridden by stored data, so rely on the persisted timestamp in that case.
- There are no invariants enforced here; enforce uniqueness and referential constraints at the database or repository layer.