# IrcBroadcaster

> **File:** `src/EchoHub.Server.Irc/IrcBroadcaster.cs`  
> **Kind:** class

```csharp
public class IrcBroadcaster : IChatBroadcaster
```


An IChatBroadcaster implementation that adapts application chat events into raw IRC protocol lines and sends them to connected IRC clients via an IrcGatewayService. Use this when you need to bridge the application's chat model (messages, joins/parts, kicks, bans, topics, deletes, etc.) to classic IRC clients that expect server-prefixed commands and NOTICE/TOPIC/KICK semantics.

## Remarks
IrcBroadcaster acts as an adapter between the higher-level chat domain and the IRC protocol. It delegates message formatting to IrcMessageFormatter, relies on IMessageEncryptionService to decrypt payloads before sending (IRC clients can't handle the app-layer encryption), and uses IrcGatewayService to enumerate and write to client connections. The class intentionally translates events into protocol-native lines (for example, prefixing with the server name, using JOIN/PART/TOPIC/KICK commands, and sending NOTICEs) so IRC clients receive familiar, interoperable behavior.

## Example
```csharp
// Assume gateway and encryption are provided by the hosting environment (DI, test doubles, etc.).
var broadcaster = new IrcBroadcaster(gateway, encryption);
var message = new MessageDto { Content = "ENCRYPTED_PAYLOAD", SenderUsername = "alice" };
await broadcaster.SendMessageToChannelAsync("general", message);
```

## Notes
- Messages are decrypted before formatting and sending because IRC clients do not understand application-layer encryption; ensure the IMessageEncryptionService provided can decrypt payloads intended for IRC.
- SendUserStatusChangedAsync is intentionally a no-op: IRC has no active presence broadcast mechanism and clients discover away/status via WHO/WHOIS queries.
- The broadcaster follows IRC conventions such as not echoing a sender's own messages back to their connection and using the gateway's Options.ServerName as the origin for server-sent commands; callers should supply channel names in the form expected by the gateway (the broadcaster prefixes IRC commands with '#' where appropriate).