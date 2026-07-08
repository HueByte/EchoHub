# PresenceTracker

> **File:** `src/EchoHub.Server/Services/PresenceTracker.cs`  
> **Kind:** class

```csharp
public class PresenceTracker
```


Tracks active client connections, per-user connection sets, and per-user channel memberships for an in-memory SignalR-style presence service. Use this when you need a concurrent-friendly registry of who is online, which channels each user has joined, and to obtain connection IDs for sending messages to users in channels.

## Remarks
PresenceTracker keeps three maps: connectionId → (userId, username), username → set of connectionIds, and username → set of channel names. ConcurrentDictionary is used for the maps, but a private lock is taken around operations that read or mutate the HashSet values (and around multi-step dictionary sequences) because `HashSet<T>` is not thread-safe and individual dictionary operations alone do not provide atomicity for those compound operations. The UserCountChanged event is raised when the distinct online user count changes; it is invoked outside the lock to avoid deadlocks and to minimize lock hold time.

## Example
```csharp
var tracker = new PresenceTracker();
tracker.UserCountChanged += count => Console.WriteLine($"Online users: {count}");

// A client connects
tracker.UserConnected("conn-1", Guid.NewGuid(), "alice");

// Alice joins a channel; returns true if this was a new join for that user+channel
var wasNewJoin = tracker.JoinChannel("alice", "lobby");

// Query who is in the lobby and what channels Alice is in
var usersInLobby = tracker.GetOnlineUsersInChannel("lobby");
var aliceChannels = tracker.GetChannelsForUser("alice");

// Get all connection IDs for members of one or more channels
var connections = tracker.GetConnectionsInChannels(new List<string> { "lobby" });

// Client disconnects
var username = tracker.UserDisconnected("conn-1");
```

## Notes
- The tracker keys users by the provided username string. If your system permits duplicate or non-normalized usernames, entries will collide; ensure usernames are unique or normalized before using this tracker.  
- Public getters return snapshot lists (`List<string>`) — callers receive copies and should not expect live views into internal collections.  
- JoinChannel returns a bool indicating whether the user newly joined the channel (true) or was already a member (false).  
- UserCountChanged is invoked after the internal state change and outside the lock; handlers will run on the thread that performed the change, so keep handlers short to avoid delaying the caller.  
- Connection IDs passed to UserConnected overwrite any existing entry for the same connectionId; connectionId uniqueness is assumed by the tracker.