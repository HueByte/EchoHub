# IChatBroadcaster

> **File:** `src/EchoHub.Core/Contracts/IChatBroadcaster.cs`  
> **Kind:** interface

```csharp
public interface IChatBroadcaster
```


Routes outgoing chat-related events to connected clients. Implementations deliver messages, presence updates, moderation events and control signals (errors / forced disconnects) to channels or specific connections; prefer this abstraction when hub or service logic should emit events without depending on a specific transport (SignalR, raw WebSockets, etc.).

## Remarks
This interface centralizes all outbound notifications for the chat subsystem so callers only express what should be sent (message, join/leave, ban, etc.) and not how it is delivered. It keeps hub and business logic decoupled from the delivery mechanism, making it straightforward to swap or mock the underlying transport in tests. Methods are asynchronous (return Task) because delivery is IO-bound and implementations are expected to perform non-blocking network operations.

## Example
```csharp
// Example usage inside a hub or service that has IChatBroadcaster injected
public class ChatService
{
    private readonly IChatBroadcaster _broadcaster;

    public ChatService(IChatBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    public async Task HandleIncomingMessageAsync(string channelName, MessageDto message)
    {
        // Persist message, run business rules, then broadcast
        await _broadcaster.SendMessageToChannelAsync(channelName, message);
    }

    public async Task NotifyJoinAsync(string channelName, string username, UserPresenceDto presence, string connectionId)
    {
        // Tell other clients in the channel that this user joined; exclude the joining connection if desired
        await _broadcaster.SendUserJoinedAsync(channelName, username, presence, excludeConnectionId: connectionId);
    }
}
```

## Notes
- All methods are asynchronous; callers should await Tasks to observe delivery failures and avoid unobserved exceptions.
- The excludeConnectionId parameter on SendUserJoinedAsync is typically used to avoid echoing a join event back to the origin connection.
- SendChannelUpdatedAsync accepts an optional channelName — callers can provide an explicit target, otherwise an implementation may use identifying information carried in the ChannelDto to determine recipients.
- SendUserBannedAsync has no channel parameter and therefore represents a user-ban notification that is not scoped to a single channel (interpretation depends on the implementation).