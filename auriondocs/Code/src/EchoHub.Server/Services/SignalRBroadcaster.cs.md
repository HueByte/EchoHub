# SignalRBroadcaster

> **File:** `src/EchoHub.Server/Services/SignalRBroadcaster.cs`  
> **Kind:** class

```csharp
public class SignalRBroadcaster : IChatBroadcaster
```


Implements IChatBroadcaster to deliver chat events to SignalR-connected clients (via an `IHubContext<ChatHub, IEchoHubClient>`). Use this implementation when the application should push messages, presence updates, channel changes and moderation events to SignalR clients; it routes events to groups, specific clients, or all connected SignalR clients as appropriate.

## Remarks
This class adapts the generic chat-broadcasting contract to SignalR: it resolves an IHubContext lazily from an IServiceProvider and uses the ChatHub/IEchoHubClient surface to send notifications. It cooperates with a PresenceTracker to map channel lists to active SignalR connection ids and intentionally filters out connections belonging to the IRC gateway. The implementation keeps broadcasting logic simple (group vs. all vs. specific clients) and relies on SignalR's client invocation Tasks for async behavior.

## Notes
- The implementation treats connection IDs prefixed with "irc-" as non-SignalR (IRC gateway) and excludes or ignores those ids in several methods; callers must follow that convention if mixing IRC and SignalR connections.
- SendMessageToChannelAsync intentionally ignores the excludeConnectionId parameter (comment: SignalR clients render their own message echo). For selective exclusion of a SignalR connection use SendUserJoinedAsync (which excludes non-IRC ids) or other targeted methods that call GroupExcept/Clients.
- SendUserStatusChangedAsync will return Task.CompletedTask when no SignalR connections are found for the provided channels — callers should expect no-op behavior in that case.
- HubContext is cached in a private field after first resolution from IServiceProvider; the lazy resolution avoids constructor-time resolution (useful to prevent dependency cycles) and subsequent accesses reuse the same IHubContext instance.