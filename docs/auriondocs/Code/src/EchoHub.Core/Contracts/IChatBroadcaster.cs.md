# IChatBroadcaster

> **File:** `src/EchoHub.Core/Contracts/IChatBroadcaster.cs`  
> **Kind:** interface

```csharp
public interface IChatBroadcaster
```


A transport-agnostic abstraction for broadcasting chat events and presence changes to connected clients. Use `IChatBroadcaster` whenever server-side code (for example a hub, worker, or command handler) needs to notify one or more clients about messages, presence updates, channel lifecycle events, moderation actions, or errors without depending on a specific delivery mechanism.

## Remarks
`IChatBroadcaster` centralizes all outgoing chat-related notifications so callers do not need to know or implement the delivery/fan-out semantics. Each method maps to a well-defined event type: `SendMessageToChannelAsync` for chat messages, `SendUserJoinedAsync` / `SendUserLeftAsync` for presence changes, `SendChannelUpdatedAsync` / `SendChannelDeletedAsync` / `SendChannelNukedAsync` for channel lifecycle, moderation actions via `SendUserKickedAsync` / `SendUserBannedAsync`, and utility operations such as `SendMessageDeletedAsync`, `SendUserStatusChangedAsync`, `SendErrorAsync`, and `ForceDisconnectUserAsync` for forced disconnects. The interface is asynchronous (`Task`-based) so implementations can perform non-blocking I/O, retries, batching, or use different transports (for example SignalR, WebSockets, or a message bus) without changing callers. The `excludeConnectionId` parameter on message/presence methods encodes the common IRC convention of not echoing a message back to the originating connection while still delivering it to other connections belonging to the same user.

## Notes
- `excludeConnectionId` prevents delivery only to the specified connection; other connections for the same user still receive the event. Callers should pass the sending connection id to avoid echoing to that connection but should not rely on it to suppress notifications to other sessions of the same user.
- `SendChannelUpdatedAsync` includes an optional `channelName` parameter in addition to the [`ChannelDto`](../DTOs/ChatDtos.cs.md). The intent of the optional `channelName` (for example: target channel selection vs. previous name) is not obvious from the signature and should be clarified by the implementation or caller to avoid mismatched behavior.
- All methods return `Task` and must be awaited or otherwise observed by callers to ensure errors in the broadcasting layer are surfaced; implementations may perform I/O and should handle transient failures internally or propagate meaningful exceptions to callers.