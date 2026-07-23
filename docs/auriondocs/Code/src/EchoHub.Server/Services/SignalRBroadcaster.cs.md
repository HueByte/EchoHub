# SignalRBroadcaster

> **File:** `src/EchoHub.Server/Services/SignalRBroadcaster.cs`  
> **Kind:** class

```csharp
public class SignalRBroadcaster : IChatBroadcaster
```


Broadcasts chat events to connected SignalR clients and adapts the generic [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) contract to an `IHubContext<ChatHub, IEchoHubClient>`-backed implementation. Use `SignalRBroadcaster` when you need server-side broadcasting of messages, presence updates, channel lifecycle events and administrative actions to SignalR clients; the class centralizes SignalR-specific delivery details so callers can work with the [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) abstraction.

## Remarks
`SignalRBroadcaster` resolves and caches an `IHubContext<ChatHub, IEchoHubClient>` lazily from the provided `IServiceProvider`, and uses a [`PresenceTracker`](PresenceTracker.cs.md) to map channels to live connection IDs. It implements the [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) surface by translating high-level events (message send, user joined/left, status changes, channel updates, kicks/bans, deletes, nukes, errors, and forced disconnects) into SignalR calls on `Clients.Group`, `Clients.All`, `Clients.Clients` and `Clients.Client`. The implementation intentionally treats connection IDs that start with the `irc-` prefix as non-SignalR (they are handled by a separate IRC gateway), so several methods either filter those IDs out or no-op for them.

## Notes
- The `excludeConnectionId` parameter is ignored by `SendMessageToChannelAsync` (the comment in-source explains the IRC exclusion only applies to the IRC gateway because SignalR clients render their own broadcast echo). Callers expecting the exclude behavior for SignalR clients should not rely on it for this method.
- Several methods filter out connection IDs that start with `irc-` (for example `SendUserStatusChangedAsync` and `ForceDisconnectUserAsync`); this convention must be followed by any component that produces or stores mixed connection IDs, otherwise intended recipients may be missed or IRC gateways may receive inappropriate signals.
- `HubContext` is resolved once via `IServiceProvider.GetRequiredService<IHubContext<ChatHub, IEchoHubClient>>()` and cached in a private field. If the application's DI configuration does not provide that service the call will throw at first use; caching avoids repeated resolution but means any change in the resolved instance after first access will not be observed.
- Methods return the `Task` returned by SignalR calls directly; any exceptions thrown by SignalR delivery will propagate to the caller of the [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) method.