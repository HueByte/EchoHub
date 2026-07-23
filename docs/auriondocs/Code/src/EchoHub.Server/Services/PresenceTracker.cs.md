# PresenceTracker

> **File:** `src/EchoHub.Server/Services/PresenceTracker.cs`  
> **Kind:** class

```csharp
public class PresenceTracker
```


Tracks active connections and per-user channel membership for a server-side hub/service. Use this when you need to know which usernames are currently connected (counting a user with multiple connections only once), map individual connection IDs to their user, and query which users are in which chat channels.

## Remarks
PresenceTracker centralizes presence state in three concurrent dictionaries: a connectionId → (userId, username) map, a username → set-of-connectionIds map, and a username → set-of-channelNames map. It uses a single private lock to make operations on the HashSet values atomic because ConcurrentDictionary only protects access to individual slots, not the mutable collections stored as values. The UserCountChanged event is raised only when the distinct online user count changes (for example, when a user's first connection is added or their last connection is removed); the implementation invokes the event outside the lock to avoid holding the lock while user code runs.

## Example
```csharp
var tracker = new PresenceTracker();
tracker.UserCountChanged += count => Console.WriteLine($"Online users: {count}");

// user connects from two clients (only the first connection should trigger the count change)
tracker.UserConnected("conn-1", Guid.NewGuid(), "alice"); // triggers UserCountChanged -> 1
tracker.UserConnected("conn-2", Guid.NewGuid(), "alice"); // no count change

// join a channel and query who is in it
tracker.JoinChannel("alice", "general");
var usersInGeneral = tracker.GetOnlineUsersInChannel("general"); // contains "alice"

// disconnect one connection; user still online because another connection remains
tracker.UserDisconnected("conn-1"); // returns "alice"; no UserCountChanged

// final disconnect removes the user and triggers UserCountChanged
tracker.UserDisconnected("conn-2"); // returns "alice"; triggers UserCountChanged -> 0
```

## Notes
- The class treats usernames as dictionary keys using the string's default equality (case-sensitive by default). Normalize or use a consistent casing strategy before calling if your application expects case-insensitive behavior.
- The lock protects the HashSet instances stored in the dictionaries; callers do not need to synchronize when calling the public methods, but should avoid long-running work inside UserCountChanged handlers because the event is invoked from the presence-tracking flow (the implementation intentionally invokes the event outside the lock, but handlers that re-enter tracker methods could still affect ordering).
- The provided source appears truncated / contains a small syntax issue near GetChannelsForUser and GetConnectionsInChannels; verify the final implementation of those methods before relying on their exact return behavior.