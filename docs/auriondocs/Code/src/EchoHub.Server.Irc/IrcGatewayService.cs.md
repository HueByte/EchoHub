# IrcGatewayService

> **File:** `src/EchoHub.Server.Irc/IrcGatewayService.cs`  
> **Kind:** class

```csharp
public sealed class IrcGatewayService : BackgroundService
```


Hosts a TCP-based IRC gateway as a BackgroundService: it listens for plain and (optionally) TLS connections, accepts clients, wraps each socket in an IrcClientConnection, and hands that connection to an IrcCommandHandler which bridges the client into the server-side chat/channel/user services. Reach for this type when you need a long-running, DI-hosted component that exposes active IRC connections and runs the per-client command loop for each incoming connection.

## Remarks
This class is the network-facing boundary for the IRC protocol in the application. It reads configuration from IrcOptions to decide ports and TLS behavior, manages a concurrent map of live connections, resolves application services (chat, user, channel, encryption) from DI for each client, and delegates protocol logic to IrcCommandHandler. It is intentionally thin: networking, TLS negotiation, and connection lifecycle are handled here so the command handler can focus on protocol semantics and integration with core services.

## Example
```csharp
// enumerate authenticated IRC clients
var gateway = /* resolved IrcGatewayService from DI or test harness */;
foreach (var conn in gateway.GetAllConnections())
{
    Console.WriteLine($"Client: {conn.ConnectionId}, Nick: {conn.Nickname}");
}

// get members of a specific channel
var members = gateway.GetConnectionsInChannel("#general");
foreach (var member in members)
{
    Console.WriteLine(member.Nickname);
}
```

## Notes
- TLS listeners are only started when IrcOptions.TlsEnabled is true and TlsCertPath is provided; enablement without a valid cert path means no TLS listener will be created.
- Connections are added to the internal ConcurrentDictionary when a client connects; callers querying Connections (the IReadOnlyDictionary) will see all tracked entries, while GetAllConnections and GetConnectionsInChannel filter to authenticated clients (check IrcClientConnection.IsAuthenticated).
- The service runs as a BackgroundService and must be registered with the host so ExecuteAsync is invoked; it cooperates with cancellation tokens to stop listeners and client handling.
- Be cautious assuming immediate removal of entries: connection tracking and cleanup happen elsewhere in the implementation (connection lifecycle code); when in doubt, check IsAuthenticated or other connection state rather than assuming the dictionary contains only live, registered users.