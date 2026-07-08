# ChatHub

> **File:** `src/EchoHub.Server/Hubs/ChatHub.cs`  
> **Kind:** class

```csharp
[Authorize]
public class ChatHub : Hub<IEchoHubClient>
```


A SignalR Hub that exposes chat-related operations to authenticated clients and delegates core business logic to an injected IChatService. It extracts the current user's ID and username from claims, manages SignalR group membership (channels), and translates service results and exceptions into client-facing responses or error notifications.

## Remarks
This hub is a thin transport adapter: it performs authentication-based identity resolution, normalises channel names, updates SignalR group membership, and forwards requests to IChatService for the actual chat behaviour (join/leave/send/history/status). It centralises logging and error handling so the chat service can remain focused on domain rules and persistence. Because Hub instances are per-connection in SignalR, the class does not attempt cross-connection synchronization — it expects the chat service to handle shared state.

## Notes
- The hub requires an authenticated user and relies on ClaimTypes.NameIdentifier for the user's GUID and a "username" claim for display name; missing claims cause a HubException and will cause connection handlers to fail.
- Channel names are normalised with ToLowerInvariant().Trim() before being used for group membership; callers should be aware the hub treats channels case-insensitively.
- JoinChannel returns a JoinChannelResult containing history or an error string; other failures are reported back to the caller via Clients.Caller.Error and are logged server-side.