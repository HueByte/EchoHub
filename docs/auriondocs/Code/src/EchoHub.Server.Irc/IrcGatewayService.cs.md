# IrcGatewayService

> **File:** `src/EchoHub.Server.Irc/IrcGatewayService.cs`  
> **Kind:** class

*Figure: How IrcGatewayService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
  Start["Start ExecuteAsync in IrcGatewayService"] --> CheckEnabled{"Check IrcOptions Enabled"}

  CheckEnabled -->|"no"| LogDisabled["Log 'IRC gateway is disabled' and return"] --> End["End ExecuteAsync"]
  CheckEnabled -->|"yes"| BuildListeners["Create listeners list"] --> AddPlainListener["Add RunListenerAsync for plain port (starts Task)"]

  AddPlainListener -->|"starts Task"| RunListenerPlain["RunListenerAsync(port, useTls=false)"]

  BuildListeners --> CheckTls{"TLS enabled and cert path set"}
  CheckTls -->|"no"| WaitAll["Await Task.WhenAll(listeners)"] --> End
  CheckTls -->|"yes"| AddTlsListener["Add RunListenerAsync for TLS port (starts Task)"]
  AddTlsListener -->|"starts Task"| RunListenerTls["RunListenerAsync(port, useTls=true)"]
  RunListenerPlain --> RunListenerCore
  RunListenerTls --> RunListenerCore

  RunListenerCore["RunListenerAsync body"] --> StartListener["Start TcpListener and log listening"]
  StartListener --> RegisterCancel["Register ct to stop listener"] --> ListenerLoop{"ct.IsCancellationRequested"}
  ListenerLoop -->|"no"| AcceptClient["AcceptTcpClientAsync"] --> SpawnHandle["Spawn HandleClientAsync(tcpClient, useTls) as fire and forget"] --> ListenerLoop
  ListenerLoop -->|"yes"| StopListener["Stop listener and return from RunListenerAsync"]

  SpawnHandle --> HandleClientStart["HandleClientAsync: get stream"] --> UseTls{"useTls"}
  UseTls -->|"yes"| TLSHandshakeTry["Attempt TLS handshake"]
  TLSHandshakeTry -->|"handshake failed"| TLSHandshakeFail["Log TLS handshake failed and close client, return"]
  TLSHandshakeTry -->|"handshake succeeded"| AfterTls
  UseTls -->|"no"| AfterTls["Proceed with plain stream"]

  AfterTls --> CreateConnection["Create IrcClientConnection instance"] --> AddConnection["Add connection to _connections"] --> EndHandle["Return from HandleClientAsync"]

  %% Simple getters
  GetAll["GetAllConnections returns authenticated IrcClientConnection entries"]
  GetInChannel["GetConnectionsInChannel(channelName) returns authenticated IrcClientConnection in channel"]

  EndHandle --> End
  LogDisabled --> End
  StopListener --> End
  AddConnection --> EndHandle
```

```csharp
public sealed class IrcGatewayService : BackgroundService
```


An always-on hosted gateway that accepts raw TCP (optionally TLS) connections and exposes an IRC-compatible surface backed by the EchoHub services. `IrcGatewayService` reads configuration from [`IrcOptions`](IrcOptions.cs.md), listens on the configured ports, accepts incoming `TcpClient` connections, wraps them in [`IrcClientConnection`](IrcClientConnection.cs.md) objects, and hands each connection to an [`IrcCommandHandler`](IrcCommandHandler.cs.md) that bridges IRC commands to the application services ([`IChatService`](../EchoHub.Core/Contracts/IChatService.cs.md), [`IUserService`](../EchoHub.Core/Contracts/IUserService.cs.md), [`IChannelService`](../EchoHub.Core/Contracts/IChannelService.cs.md), [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md)). Reach for `IrcGatewayService` when you want to run an IRC-facing adapter for the EchoHub system rather than implementing socket handling and protocol dispatch yourself.

## Remarks
`IrcGatewayService` is a long-running `BackgroundService` that centralizes network-level concerns for the IRC gateway: socket listening, optional TLS handshake, acceptance of clients, and registration of active connections in the concurrent `_connections` map. It delegates protocol parsing and business-logic handling to [`IrcCommandHandler`](IrcCommandHandler.cs.md), resolving the required domain services from the DI `IServiceProvider` per connection so the gateway stays thin and focused on I/O and lifecycle. The service uses [`IrcOptions`](IrcOptions.cs.md) to control whether the gateway is enabled, which ports to bind, and whether to offer TLS; listeners are run as independent tasks and shut down when the host cancellation token is triggered.

## Notes
- TLS requires a valid `IrcOptions.TlsCertPath` and password when `IrcOptions.TlsEnabled` is true; a failed TLS handshake will be logged and the connection closed (the code logs "TLS handshake failed" on exception).  
- Active connections are tracked in the `ConcurrentDictionary` `_connections` and can be inspected via `GetConnectionsInChannel` and `GetAllConnections`; the dictionary makes concurrent adds/removes safe, but callers should expect the set to change while enumerating.  
- The provided source was truncated inside `HandleClientAsync` in the task payload; I could not verify whether each [`IrcClientConnection`](IrcClientConnection.cs.md) is always removed from `_connections` and whether streams/clients are always disposed on disconnect. If you rely on deterministic cleanup, inspect the full `HandleClientAsync` implementation to confirm that connections are removed and resources are disposed on normal disconnect and on error.