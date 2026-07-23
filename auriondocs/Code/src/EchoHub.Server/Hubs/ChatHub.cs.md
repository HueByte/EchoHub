# ChatHub

> **File:** `src/EchoHub.Server/Hubs/ChatHub.cs`  
> **Kind:** class

*Figure: How ChatHub works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
start["Incoming Hub action to ChatHub"]

start --> onConn["OnConnectedAsync"]
onConn --> callUserConnected["Call IChatService.UserConnectedAsync(Context.ConnectionId, CurrentUserId, CurrentUsername)"]
callUserConnected --> baseOnConn["Call base.OnConnectedAsync()"]
callUserConnected -->|"exception"| onConnLog["Log error via ILogger and rethrow"]
baseOnConn --> onConnEnd["OnConnectedAsync returns"]

start --> onDisc["OnDisconnectedAsync"]
onDisc --> callUserDisconnected["Call IChatService.UserDisconnectedAsync(Context.ConnectionId)"]
callUserDisconnected --> baseOnDisc["Call base.OnDisconnectedAsync(exception)"]
callUserDisconnected -->|"exception"| onDiscLog["Log error via ILogger and rethrow"]
baseOnDisc --> onDiscEnd["OnDisconnectedAsync returns"]

start --> join["JoinChannel(channelName, password?)"]
join --> callJoinService["Call IChatService.JoinChannelAsync(Context.ConnectionId, CurrentUserId, CurrentUsername, channelName, password)"]
callJoinService --> joinDecision{"error is not null?"}
joinDecision -->|"yes"| joinReturnError["Return JoinChannelResult(false, [], error, passwordRequired)"]
joinDecision -->|"no"| addGroup["Call Groups.AddToGroupAsync(Context.ConnectionId, channelName.ToLowerInvariant().Trim())"]
addGroup --> getEnvelope["Call IChannelService.GetChannelKeyEnvelopeAsync(channelName)"]
getEnvelope --> joinReturnSuccess["Return JoinChannelResult(true, history, EncryptionSalt, WrappedRoomKey)"]
callJoinService -->|"exception"| joinExceptionLog["Log error via ILogger; Return JoinChannelResult(false, [], Failed to join channel: ex.Message)"]

start --> leave["LeaveChannel(channelName)"]
leave --> normalize["Normalize channelName to lowerInvariant and trim"]
normalize --> callLeaveService["Call IChatService.LeaveChannelAsync(Context.ConnectionId, CurrentUsername, channelName)"]
callLeaveService --> removeFromGroup["Call Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName)"]
callLeaveService -->|"exception"| leaveExceptionLog["Log error via ILogger"]
```

```csharp
[Authorize]
public class ChatHub : Hub<IEchoHubClient>
```


A SignalR hub that exposes real-time chat operations (connect/disconnect, join/leave channel, send messages) and bridges authenticated SignalR connections with the domain services that manage presence, channels and message delivery. Reach for `ChatHub` when you need a server-side, authenticated entry point that coordinates [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) and [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md), manages SignalR groups, and forwards notifications to clients via the [`IEchoHubClient`](../../EchoHub.Core/Contracts/IEchoHubClient.cs.md) callbacks.

## Remarks
`ChatHub` is a thin application-layer adapter: it enforces authentication (the class is decorated with `Authorize`), resolves the current user from the SignalR `Context` claims via `CurrentUserId` and `CurrentUsername`, and delegates core logic to [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) and [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md). It is responsible for SignalR group membership using `Groups.AddToGroupAsync` / `Groups.RemoveFromGroupAsync`, for logging errors via `ILogger<ChatHub>`, and for returning protocol-shaped results such as [`JoinChannelResult`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) and client-facing error messages through the `IEchoHubClient.Error` callback. The hub intentionally does not perform domain operations itself — it translates connection-level events and client requests into calls to the underlying services and shapes the responses for connected clients.

## Notes
- `CurrentUserId` and `CurrentUsername` throw a `HubException` if the expected claims are absent; the `Authorize` attribute reduces this risk, but any token must contain `ClaimTypes.NameIdentifier` and a `"username"` claim for the hub to function.
- Channel names are normalized with `channelName.ToLowerInvariant().Trim()` before being used with SignalR groups; callers and services must use the same normalization to avoid mismatches in group membership.
- When `JoinChannel` succeeds for an encrypted channel the hub returns the channel key envelope obtained from `IChannelService.GetChannelKeyEnvelopeAsync` (the server comment notes members unwrap the room key client-side); do not expect the server to decrypt room content for clients.