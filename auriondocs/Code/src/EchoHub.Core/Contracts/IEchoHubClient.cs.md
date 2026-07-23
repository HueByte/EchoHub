# IEchoHubClient

> **File:** `src/EchoHub.Core/Contracts/IEchoHubClient.cs`  
> **Kind:** interface

```csharp
public interface IEchoHubClient
```


Represents the set of callbacks the server can invoke on a connected client. Implement this interface on client-side code that subscribes to the server's real-time hub so the client can react to server-initiated events such as incoming messages, presence updates, channel changes, and administrative actions.

## Remarks
This interface defines a stable, strongly-typed surface for server-to-client notifications. Each method corresponds to a distinct event the server may raise (message delivery, user presence changes, channel lifecycle events, errors, and forced disconnects). Implementations keep client-side handling decoupled from the transport layer and allow the server to call back into client logic without embedding client behavior in server code.

## Example
```csharp
// Minimal client-side implementation that logs events; real handlers should avoid long-running work.
public class EchoClientHandler : IEchoHubClient
{
    public Task ReceiveMessage(MessageDto message)
    {
        Console.WriteLine($"Received message: {message}");
        return Task.CompletedTask;
    }

    public Task UserJoined(string channelName, string username, UserPresenceDto? presence)
    {
        Console.WriteLine($"{username} joined {channelName}");
        return Task.CompletedTask;
    }

    public Task UserLeft(string channelName, string username)
    {
        Console.WriteLine($"{username} left {channelName}");
        return Task.CompletedTask;
    }

    // Other members can be implemented similarly; keep handlers quick and non-blocking.
    public Task ChannelUpdated(ChannelDto channel) => Task.CompletedTask;
    public Task UserStatusChanged(UserPresenceDto presence) => Task.CompletedTask;
    public Task UserKicked(string channelName, string username, string? reason) => Task.CompletedTask;
    public Task UserBanned(string username, string? reason) => Task.CompletedTask;
    public Task MessageDeleted(string channelName, Guid messageId) => Task.CompletedTask;
    public Task ChannelDeleted(string channelName) => Task.CompletedTask;
    public Task ChannelNuked(string channelName) => Task.CompletedTask;
    public Task ForceDisconnect(string reason) => Task.CompletedTask;
    public Task Error(string message)
    {
        Console.Error.WriteLine(message);
        return Task.CompletedTask;
    }
}
```

## Notes
- Handlers are asynchronous (return Task): keep implementations short and non-blocking to avoid delaying the server's invocation path.
- Nullable parameters (e.g. UserPresenceDto? and string?) may be null; check before accessing members.
- Server-driven callbacks can occur concurrently; ensure any shared client state mutated by these methods is accessed in a thread-safe manner.
- Catch and handle exceptions inside handlers — unhandled exceptions may affect the connection or be observable by the server depending on the transport behavior.