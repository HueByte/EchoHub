# SignalRBroadcaster

> **File:** `src/EchoHub.Server/Services/SignalRBroadcaster.cs`  
> **Kind:** class

```csharp
public class SignalRBroadcaster : IChatBroadcaster
```


Implements IChatBroadcaster by forwarding chat events to connected SignalR clients using an `IHubContext<ChatHub, IEchoHubClient>`. Use this class when server-side chat events (messages, joins/lefts, kicks/bans, channel updates, errors, forced disconnects, etc.) need to be delivered to SignalR-connected clients; it explicitly ignores connections whose IDs begin with the "irc-" prefix (those are treated as non-SignalR clients).

## Remarks
This type is the SignalR-backed implementation of the IChatBroadcaster contract: it translates domain-level broadcast requests into Hub client calls. It relies on a PresenceTracker to resolve which connection IDs belong to which channels and lazily resolves the IHubContext from an IServiceProvider so the broadcaster can be composed into the DI graph without requiring the hub context at construction time.

## Notes
- The broadcaster treats any connection id that starts with the literal prefix "irc-" as a non-SignalR/IRC connection and filters those out of SignalR-targeted calls; ensure upstream code uses the same convention for connection IDs.
- HubContext is resolved lazily and cached in a private field; this avoids repeatedly asking the service provider but means the cached instance will be reused for the lifetime of this SignalRBroadcaster. Ensure this matches your service lifetimes (IHubContext is typically safe to reuse).
- The SendUserJoinedAsync implementation attempts to call GroupExcept with an array containing the excluded connection id. The source uses the literal syntax `[excludeConnectionId]`, which appears incorrect for C# array construction and will not compile as written; replace with a proper array or enumerable (for example: `new[] { excludeConnectionId }`).