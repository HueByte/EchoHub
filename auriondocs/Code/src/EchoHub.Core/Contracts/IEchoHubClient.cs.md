# IEchoHubClient

> **File:** `src/EchoHub.Core/Contracts/IEchoHubClient.cs`  
> **Kind:** interface

```csharp
public interface IEchoHubClient
```


Represents the callback contract for notifications and control messages the server can invoke on connected clients. Implement this interface on the client side (or provide a test double) when you need a strongly-typed set of server-to-client RPCs for events such as new messages, presence changes, channel updates, moderation actions, and error or disconnect notifications.

## Remarks
This interface centralizes all server-originated client callbacks into a single, versioned surface so the server can address connected clients with a known set of operations. Each method returns a `Task` to allow asynchronous client implementations (IO, UI dispatching, persistence) and to make the callbacks composable for test harnesses and runtime adapters. The nullable annotations on parameters (for example the `UserPresenceDto?` in `UserJoined` and `string?` in `UserKicked`) indicate which values the server may omit; implementations must handle those cases.

## Example
```csharp
using System;
using System.Threading.Tasks;

public class ConsoleEchoClient : IEchoHubClient
{
    public Task ReceiveMessage(MessageDto message)
    {
        Console.WriteLine($"[{message.Channel}] {message.Sender}: {message.Text}");
        return Task.CompletedTask;
    }

    public Task UserJoined(string channelName, string username, UserPresenceDto? presence)
    {
        Console.WriteLine($"User joined {channelName}: {username}");
        return Task.CompletedTask;
    }

    public Task UserLeft(string channelName, string username)
    {
        Console.WriteLine($"User left {channelName}: {username}");
        return Task.CompletedTask;
    }

    public Task ChannelUpdated(ChannelDto channel)
    {
        Console.WriteLine($"Channel updated: {channel.Name}");
        return Task.CompletedTask;
    }

    public Task UserStatusChanged(UserPresenceDto presence)
    {
        Console.WriteLine($"Status changed: {presence.Username} -> {presence.Status}");
        return Task.CompletedTask;
    }

    public Task UserKicked(string channelName, string username, string? reason)
    {
        Console.WriteLine($"User kicked from {channelName}: {username} Reason: {reason ?? "(none)"}");
        return Task.CompletedTask;
    }

    public Task UserBanned(string username, string? reason)
    {
        Console.WriteLine($"User banned: {username} Reason: {reason ?? "(none)"}");
        return Task.CompletedTask;
    }

    public Task MessageDeleted(string channelName, Guid messageId)
    {
        Console.WriteLine($"Message deleted in {channelName}: {messageId}");
        return Task.CompletedTask;
    }

    public Task ChannelDeleted(string channelName)
    {
        Console.WriteLine($"Channel deleted: {channelName}");
        return Task.CompletedTask;
    }

    public Task ChannelNuked(string channelName)
    {
        Console.WriteLine($"Channel nuked: {channelName}");
        return Task.CompletedTask;
    }

    public Task ForceDisconnect(string reason)
    {
        Console.WriteLine($"Force disconnect: {reason}");
        return Task.CompletedTask;
    }

    public Task Error(string message)
    {
        Console.WriteLine($"Error from server: {message}");
        return Task.CompletedTask;
    }
}
```

## Notes
- Respect nullability: parameters annotated with `?` (for example `UserPresenceDto?` and `string?`) may be `null` and callers should handle those cases gracefully.
- All methods return `Task`: implementations should avoid long-running synchronous work on the calling thread (use `async`/`await` or schedule work) to prevent blocking the runtime that invokes these callbacks.
- Implementations should avoid throwing exceptions from these methods where possible; unhandled exceptions may surface to the caller or the hosting infrastructure depending on how the callbacks are invoked.