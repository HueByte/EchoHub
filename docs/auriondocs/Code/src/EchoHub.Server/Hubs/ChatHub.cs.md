# ChatHub

> **File:** `src/EchoHub.Server/Hubs/ChatHub.cs`  
> **Kind:** class

*Figure: How ChatHub works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
User["User connects or calls JoinChannel"]
CH_OnConnected["ChatHub: OnConnectedAsync()"]
IChatServiceConn["IChatService: UserConnectedAsync(Context.ConnectionId, CurrentUserId, CurrentUsername)"]
BaseOnConnected["Call base.OnConnectedAsync()"]
OnConnectedCatch["On exception: Log error and rethrow"]

CH_Join["ChatHub: JoinChannel(channelName, password)"]
IChatServiceJoin["IChatService: JoinChannelAsync(Context.ConnectionId, CurrentUserId, CurrentUsername, channelName, password)"]
CheckError{"error is not null?"}
ReturnFail["Return JoinChannelResult(false, [], error, passwordRequired)"]
AddGroup["Add connection to SignalR group 'channelName.ToLowerInvariant().Trim()'"]
IChannelServiceNode["IChannelService: GetChannelKeyEnvelopeAsync(channelName)"]
ReturnSuccess["Return JoinChannelResult(true, history, EncryptionSalt, WrappedRoomKey)"]
JoinCatch["On exception: Log error"]
ReturnJoinCatch["Return JoinChannelResult(false, [], 'Failed to join channel: ex.Message')"]

User --> CH_OnConnected
CH_OnConnected --> IChatServiceConn
IChatServiceConn --> BaseOnConnected
CH_OnConnected -->|"exception"| OnConnectedCatch

User --> CH_Join
CH_Join --> IChatServiceJoin
IChatServiceJoin --> CheckError
CheckError -->|"yes"| ReturnFail
CheckError -->|"no"| AddGroup
AddGroup --> IChannelServiceNode
IChannelServiceNode --> ReturnSuccess

CH_Join -->|"exception"| JoinCatch
IChatServiceJoin -->|"exception"| JoinCatch
JoinCatch --> ReturnJoinCatch
```

```csharp
[Authorize]
public class ChatHub : Hub<IEchoHubClient>
```


A SignalR Hub that exposes real-time chat operations to authenticated clients. ChatHub mediates between connected clients and the server-side chat logic (IChatService) and channel management (IChannelService), handling user connection lifecycle, channel join/leave actions, message sending, and delivery of channel encryption envelopes when applicable.

## Remarks
ChatHub is an authorization-guarded entrypoint for real-time chat behavior: it uses the caller's claims to identify the user, registers and deregisters connection state with IChatService on connect/disconnect, and forwards channel and message operations to the underlying domain services. It centralizes error logging and converts service-level outcomes into client-facing responses (for example, returning a JoinChannelResult or invoking Error on the caller). The hub also normalizes group names (lowercasing and trimming) and returns channel key envelopes from IChannelService for encrypted rooms so clients can unwrap room keys locally.

## Notes
- The hub requires authentication claims: CurrentUserId and CurrentUsername read from the connection's ClaimsPrincipal and will throw a HubException if the expected claims are missing. Ensure clients authenticate and include these claims.
- Channel names are normalized via ToLowerInvariant().Trim() before being added to or removed from SignalR groups; callers should expect case-insensitive channel membership.
- When joining encrypted channels the hub obtains an encryption salt and a wrapped room key from IChannelService and returns them to the client so the client can decrypt room keys locally — the server does not hold the raw room key.
- Connection lifecycle failures in OnConnectedAsync/OnDisconnectedAsync are logged and rethrown, while most per-operation failures return structured results or invoke Clients.Caller.Error so callers receive a clear error message without server-side leaks.