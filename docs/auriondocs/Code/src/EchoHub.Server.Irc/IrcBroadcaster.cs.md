# IrcBroadcaster

> **File:** `src/EchoHub.Server.Irc/IrcBroadcaster.cs`  
> **Kind:** class

```csharp
public class IrcBroadcaster : IChatBroadcaster
```


Broadcasts chat events to IRC clients by translating application-level messages and room/user events into IRC protocol lines and sending them via the IrcGatewayService. Decrypts transport-layer-encrypted message content so IRC clients (which do not support the application's app-layer encryption) receive readable text; end-to-end room ciphertext markers (e.g. $RC1$) are preserved. Use this class when you need to mirror server chat rooms and user lifecycle events to connected IRC clients.

## Remarks
IrcBroadcaster is the IRC-specific implementation of the IChatBroadcaster contract and acts as the bridge between the chat model and the IRC wire format. It relies on IMessageEncryptionService to remove transport-layer encryption for IRC consumers and on IrcMessageFormatter to produce IRC-compliant lines (including splitting long messages and formatting reply prefixes/embeds). Messages are dispatched by enumerating connections returned from the gateway and sending formatted lines to each connection; the broadcaster also follows IRC conventions such as avoiding echoing a message back to the originating connection.

## Notes
- Message content is decrypted before formatting; E2E room ciphertext (explicit markers like $RC1$) is left unchanged so room-encrypted messages are not exposed.
- The broadcaster avoids echoing by comparing connection IDs (excludeConnectionId) when sending normal messages. Be aware this requires callers to pass the originating connection id to suppress local echoes correctly when appropriate.
- Sends are awaited sequentially per connection/line (each connection's SendAsync is awaited in a loop). Under high fan-out this can introduce latency; consider batching or parallelization at the caller/gateway level if latency becomes an issue.