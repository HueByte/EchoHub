# ChatService

> **File:** `src/EchoHub.Server/Services/ChatService.cs`  
> **Kind:** class

```csharp
public class ChatService : IChatService
```


Coordinates real-time chat operations and state for the application. ChatService implements IChatService and is the façade a SignalR hub (or other callers) should use when handling user connections, disconnections, channel membership, presence updates, and broadcasting; it centralizes the interactions between the in-memory PresenceTracker, persistent EchoHubDbContext, channel membership logic, message encryption/embedding services, and the set of IChatBroadcaster implementations.

## Remarks
ChatService is an orchestration layer, not a data-access or broadcasting implementation. It delegates persistence to a scoped EchoHubDbContext (created via IServiceScopeFactory), presence to the in-memory PresenceTracker, channel validation/membership to IChannelService, and outgoing notifications to one or more IChatBroadcaster instances. This separation keeps short-lived request scopes for database work, while preserving an application-wide presence model and pluggable broadcasters for different transport channels.

## Notes
- ChatService creates a new IServiceScope for operations that touch the database; consumers should not assume a single DbContext instance is reused across calls.
- PresenceTracker is in-memory: presence state (online connections, channel joins) is transient and will be lost if the process restarts.
- Methods normalise channel names (e.g. lowercasing and trimming) before delegating to channel membership checks; callers should expect that channel names are compared case-insensitively.
- Disconnect handling reads the user's channels before removing the connection from PresenceTracker so it can notify those channels of the status change. Username lookups can return null; callers should tolerate null/unknown users in logging and downstream flows.