# IChatService

> **File:** `src/EchoHub.Core/Contracts/IChatService.cs`  
> **Kind:** interface

```csharp
public interface IChatService
```


Defines the contract for server-side chat functionality: connection lifecycle, channel membership, messaging, presence, broadcasting and simple queries. Implement this interface when you need a reusable application service that encapsulates chat logic used by hubs, controllers, or gateway components (for example an IRC gateway).

## Remarks
IChatService centralizes chat-related responsibilities so transport-specific code (SignalR hubs, HTTP controllers, or gateway adapters) can remain thin. It groups concerns into clear areas — connection lifecycle, channel operations, messaging, presence, broadcasting and queries — letting implementations choose an in-memory, persistent, or distributed backing without changing callers.

## Example
```csharp
// Typical usage from a transport layer: register the connection, join a channel,
// send a message, read recent history and broadcast a message to the channel.
async Task UseChatServiceAsync(IChatService chat, string connectionId, Guid userId, string username)
{
    await chat.UserConnectedAsync(connectionId, userId, username);

    var (history, joinError) = await chat.JoinChannelAsync(connectionId, userId, username, "general");
    if (joinError != null) 
        return; // handle join failure

    await chat.SendMessageAsync(userId, username, "general", "Hello everyone!");

    var recent = await chat.GetChannelHistoryAsync("general", 50);

    if (recent.Count > 0)
        await chat.BroadcastMessageAsync("general", recent[0]);
}
```

## Notes
- Several methods return `Task<string?>`; the meaning of the nullable string is implementation-defined (common uses include an error message or an identifier). Callers should check for a non-null result before assuming success.
- Implementations will be called concurrently from multiple connections; avoid long-running synchronous work inside these async methods and ensure any shared state is properly synchronized or made concurrency-safe.