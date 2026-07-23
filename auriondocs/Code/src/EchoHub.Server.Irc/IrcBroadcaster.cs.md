# IrcBroadcaster

> **File:** `src/EchoHub.Server.Irc/IrcBroadcaster.cs`  
> **Kind:** class

```csharp
public class IrcBroadcaster : IChatBroadcaster
```


Bridges application chat events into IRC wire-protocol messages and sends them to connected IRC clients. Use `IrcBroadcaster` when you need chat activity (messages, joins/parts, kicks/bans, topic changes) reflected on an IRC gateway so traditional IRC clients see the room as an IRC channel; prefer using a plain chat broadcaster when you do not need IRC-formatted output.

## Remarks
`IrcBroadcaster` is an [`IChatBroadcaster`](../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) implementation that adapts the application's chat model to IRC conventions. It pulls active connections from the [`IrcGatewayService`](IrcGatewayService.cs.md) (`GetConnectionsInChannel` / `GetAllConnections`), formats user-visible text with `IrcMessageFormatter.FormatMessage` (using the gateway `Options.PublicBaseUrl` for absolute links), and sends one or more IRC lines to each connection via `conn.SendAsync`. Because IRC clients cannot handle app-layer encryption, the broadcaster uses the injected [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) to decrypt transport-encrypted payloads before formatting; end-to-end room ciphertext markers (`$RC1$`) are left intact. The implementation also follows IRC conventions for echo suppression (it excludes the originating connection by `excludeConnectionId`) and for addressing (matching by `ConnectionId` or `Nickname` where appropriate).

## Example
```csharp
// given existing instances of IrcGatewayService and IMessageEncryptionService
var gateway = /* existing IrcGatewayService */;
var encryption = /* existing IMessageEncryptionService */;
var broadcaster = new IrcBroadcaster(gateway, encryption);

// broadcast a message to the #general channel but don't echo back to the sender
await broadcaster.SendMessageToChannelAsync("general", messageDto, excludeConnectionId: "conn-123");
```

## Notes
- `IrcBroadcaster` explicitly decrypts transport-layer encrypted content using the injected [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md); this is necessary because IRC clients do not support the application's app-layer encryption. Messages that contain E2E room ciphertext markers (`$RC1$`) are preserved as-is.
- Echo suppression is performed by comparing `conn.ConnectionId` to the provided `excludeConnectionId`. This avoids re-sending a message to the origin connection while still delivering it to other sessions belonging to the same user (different connections/nicknames).
- `IrcMessageFormatter.FormatMessage` may split a single logical message into multiple IRC lines; each resulting line is sent individually with `conn.SendAsync`, so large messages can result in multiple network writes.
- `SendUserStatusChangedAsync` is a no-op because IRC lacks an active presence broadcast; clients discover away/idle state via `WHO`/`WHOIS` instead.
