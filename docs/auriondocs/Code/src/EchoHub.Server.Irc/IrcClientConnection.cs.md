# IrcClientConnection

> **File:** `src/EchoHub.Server.Irc/IrcClientConnection.cs`  
> **Kind:** class

```csharp
public sealed class IrcClientConnection : IAsyncDisposable
```


Manages a single IRC client TCP connection: reading lines from the client, sending IRC-formatted lines (including convenience numeric replies), and tracking per-connection registration and channel state. Use this type when you need a lightweight, connection-scoped object that encapsulates I/O, basic identity (ConnectionId, Nickname, Hostmask) and mutable session state for one client.

## Remarks
IrcClientConnection is a small, self-contained abstraction that pairs a TcpClient/stream with StreamReader/StreamWriter to present an IRC-oriented API. It centralizes two concurrency concerns: outgoing writes are serialized with a SemaphoreSlim (_writeLock) so concurrent callers won't interleave lines, and joined channel membership is protected by a simple lock to allow safe reads by broadcaster threads while being mutated by command handlers. The class swallows I/O exceptions on read/write operations to simplify higher-level connection-management logic; callers should treat null from ReadLineAsync or a silently-failing SendAsync as an indicator that the connection is no longer usable.

## Notes
- Writes are serialized by a SemaphoreSlim; awaiting SendAsync concurrently is safe (it will queue) but callers should still handle the possibility that the send silently fails.
- ReadLineAsync returns null on any read error (including disconnection); the caller must treat null as end-of-connection.
- GetJoinedChannels returns a snapshot of channel names; modifying the returned list does not affect the connection's internal membership.
- Channel membership is compared case-insensitively (StringComparer.OrdinalIgnoreCase).
- Hostmask is computed from Nickname and Username; if those properties are not set by registration code, the Hostmask value will include the current (possibly null) field values.
- DisposeAsync closes the underlying TcpClient and disposes the reader/writer and semaphore; using the instance after disposal may throw or behave as if the connection is closed.