# IChatBroadcaster

> **File:** `src/EchoHub.Core/Contracts/IChatBroadcaster.cs`  
> **Kind:** interface

```csharp
public interface IChatBroadcaster
```


An abstraction for broadcasting chat-related events and notifications to connected clients. Implementations deliver channel messages, presence updates, moderation events and connection-specific errors or disconnects to the appropriate recipients; use this interface when you want hub/transport-agnostic broadcasting logic (for example to decouple business logic from SignalR or another realtime transport).

## Remarks
This interface centralizes all outbound chat notifications the server emits: channel messages, user join/leave/presence events, channel lifecycle events (updated, deleted, nuked), moderation notifications (kicked, banned), message deletions, error messages to a particular connection, and forced disconnects. It exists to keep broadcasting responsibilities in one place so higher-level code can invoke intent ("send this message to the channel" or "force-disconnect these connections") without knowing how connections are routed or how the underlying transport addresses individual connections or groups.

Implementations must honor the documented routing hints (for example, do not echo a message back to an excluded connection when excludeConnectionId is supplied). Use the channelName and connectionId parameters to determine recipients; SendErrorAsync targets a single connection, while ForceDisconnectUserAsync targets a set of connection ids.

## Example
```csharp
// typical usage from server-side chat logic
// (messageDto and presenceDto are prepared elsewhere)
await broadcaster.SendMessageToChannelAsync("#general", messageDto, excludeConnectionId: currentConnectionId);
await broadcaster.SendUserJoinedAsync("#general", "alice", presenceDto, excludeConnectionId: currentConnectionId);

// send an error to a single connection
await broadcaster.SendErrorAsync(connectionId, "You are not authorized to perform that action.");

// force-disconnect multiple connections for a user session cleanup
await broadcaster.ForceDisconnectUserAsync(new List<string> { connA, connB }, "Session revoked");
```

## Notes
- excludeConnectionId is documented for SendMessageToChannelAsync to avoid echoing the origin connection; other methods that lack an exclude parameter (for example SendUserLeftAsync) will be delivered to all intended recipients unless an implementation-specific filter is applied.
- SendUserStatusChangedAsync accepts a list of channel names so presence updates can be routed only to relevant channels; callers should pass the minimal set of channels that need the update to reduce unnecessary traffic.
- Implementations should be asynchronous and non-blocking; broadcasting to many recipients may be best-effort and not transactional across multiple method calls.