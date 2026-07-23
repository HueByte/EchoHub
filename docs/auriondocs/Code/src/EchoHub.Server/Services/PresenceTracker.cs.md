# PresenceTracker

> **File:** `src/EchoHub.Server/Services/PresenceTracker.cs`  
> **Kind:** class

```csharp
public class PresenceTracker
```


Maintains an in-memory registry of active connections, per-user connection sets, and per-user channel memberships. Use `PresenceTracker` when a hub or real-time service needs a centralized, process-local view of who is online (distinct users are counted once even if they have multiple connections), which channels each user has joined, and to obtain connection IDs for broadcasting to members of channels.

## Remarks
`PresenceTracker` centralises presence state so hubs or services can avoid scattering connection and channel bookkeeping across call sites. It stores a mapping of connection id → `(userId, username)` in `_connections`, a mapping of `username` → connection id set in `_userConnections`, and a mapping of `username` → channel name set in `_userChannels`. The class deduplicates users with multiple connections (a user with N connections is one online user) and raises `UserCountChanged` only when the distinct online user count changes. Internally it uses a private lock (`_lock`) around operations that mutate the `HashSet` values because `ConcurrentDictionary` protects its buckets but not the mutability of objects stored inside them; this ensures consistency for the `TryGetValue` → modify sequences and for multi-step cleanup when the last connection for a user is removed.

## Example
```csharp
var tracker = new PresenceTracker();
tracker.UserCountChanged += count => Console.WriteLine($"Online users: {count}");

// A client connects with two transports (two connection IDs) for the same logical user
var aliceId = Guid.NewGuid();
tracker.UserConnected("conn-1", aliceId, "alice");
tracker.UserConnected("conn-2", aliceId, "alice");

// Join a channel; returns true only if this `username` was not already in the channel
var firstJoin = tracker.JoinChannel("alice", "general");

// Read who is in a channel (snapshot list)
var usersInGeneral = tracker.GetOnlineUsersInChannel("general");

// When a connection goes away
tracker.UserDisconnected("conn-1");
// When the last connection is removed, `UserCountChanged` will fire and channel membership for that user is cleaned up
```

## Notes
- `UserCountChanged` is invoked synchronously on the calling thread after the internal lock is released; subscribers are called inline and should avoid long-running work to prevent blocking the caller.  
- `JoinChannel` creates or updates the per-`username` channel set in `_userChannels` even if that `username` currently has no active connections; channel membership is tracked separately from `_connections`.  
- The implementation uses a single private lock (`_lock`) to protect mutations of the `HashSet` values stored in the `ConcurrentDictionary` instances; this simplifies correctness but can be a contention point at very large scale. Consider sharding presence state if you expect thousands of concurrent mutations per second.
