# IrcClientConnection

> **File:** `src/EchoHub.Server.Irc/IrcClientConnection.cs`  
> **Kind:** class

```csharp
public sealed class IrcClientConnection : IAsyncDisposable
```


Manages a single IRC client TCP connection: wraps a TcpClient/Stream pair, serializes outgoing IRC lines, provides simple registration and channel membership state, and exposes read/write helpers for the IRC protocol. Use this when handling a single connected client in server code so you get consistent CRLF framing, UTF-8 encoding without BOM, and serialized writes.

## Remarks
This type represents the per-connection state and I/O for one IRC client. It centralizes the socket-level StreamReader/StreamWriter setup (UTF-8 without BOM, CRLF line endings, AutoFlush) and enforces serialized writes with an internal SemaphoreSlim. The class keeps mutable registration and presence properties (nickname, username, authentication flags, away message, joined channels) so higher-level command handlers and broadcaster threads can consult or update a single source of truth. Channel membership access is guarded by a private lock and exposed via snapshot methods so broadcaster threads can read without additional synchronization.

## Notes
- ReadLineAsync swallows read exceptions and returns null; treat a null result as "connection closed" or irrecoverable read error.
- SendAsync swallows write exceptions (connection lost) after serializing via an internal semaphore; callers cannot observe write failures directly.
- Registration properties (Nickname, Username, UserId, IsRegistered, etc.) are not synchronized by this class — callers should coordinate concurrent access if needed.
- Channel membership APIs (JoinChannel, LeaveChannel, IsInChannel, GetJoinedChannels) are thread-safe: the implementation takes a lock and GetJoinedChannels returns a snapshot list to avoid callers iterating the internal set directly.
- Hostmask composes Nickname and Username; Username may be null so Hostmask uses Username ?? Nickname in its string formatting.
- DisposeAsync closes the underlying TcpClient and disposes the reader/writer and semaphore; do not use the connection after disposing.