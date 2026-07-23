# IrcClientConnection

> **File:** `src/EchoHub.Server.Irc/IrcClientConnection.cs`  
> **Kind:** class

```csharp
public sealed class IrcClientConnection : IAsyncDisposable
```


Manages a single IRC-over-TCP client connection: reads and writes CRLF-delimited lines, holds simple registration state, and tracks joined channels. Use `IrcClientConnection` when the server needs a lightweight, per-socket representation of a connected IRC client (rather than passing raw `TcpClient`/`Stream` around).

## Remarks
`IrcClientConnection` encapsulates the I/O and minimal protocol state for one IRC client. It creates `StreamReader`/`StreamWriter` pair configured with UTF-8 without BOM (via `UTF8Encoding`) and `StreamWriter` options `AutoFlush` and `NewLine = "\r\n"` so callers can read/write logical IRC lines. Outgoing writes are serialized using the private `SemaphoreSlim` (`_writeLock`) so concurrent senders do not interleave data. Channel membership is stored in the private `_joinedChannels` and guarded by `_channelLock`, allowing safe reads by broadcaster threads while command handlers mutate the set. Connection identity is exposed through `ConnectionId` (prefixed with `HubConstants.IrcConnectionIdPrefix` and a `Guid`), and a simple `Hostmask` string is provided based on `Nickname` and `Username`.

## Notes
- `ReadLineAsync` returns `null` on error or when the underlying read fails; callers must treat `null` as a disconnected/error condition rather than a valid empty line.  
- `SendAsync` and the numeric helpers (`SendNumericAsync`) swallow exceptions raised while writing (connection loss is silently ignored), so sending failures will not throw — design choice to avoid bubbling socket errors to callers.  
- Only channel-related state (`_joinedChannels`) is synchronized. Properties such as `Nickname`, `Username`, `IsRegistered`, `IsAuthenticated`, and `AwayMessage` are not individually thread-safe; if you access them concurrently from multiple threads, add external synchronization.
- `DisposeAsync` closes the underlying `TcpClient` and disposes the reader, writer, and `_writeLock`, but does not attempt to coordinate or await in-flight operations beyond disposing those resources.