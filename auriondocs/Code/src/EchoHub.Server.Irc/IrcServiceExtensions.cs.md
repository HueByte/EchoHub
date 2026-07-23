# IrcServiceExtensions

> **File:** `src/EchoHub.Server.Irc/IrcServiceExtensions.cs`  
> **Kind:** class

```csharp
public static class IrcServiceExtensions
```


Extends `WebApplicationBuilder` with `AddIrcGateway` to wire IRC gateway support into an ASP.NET Core app. It configures [`IrcOptions`](IrcOptions.cs.md) from configuration and, when `Irc:Enabled` is true, registers [`IrcGatewayService`](IrcGatewayService.cs.md) as a singleton, wires [`IChatBroadcaster`](../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) to [`IrcBroadcaster`](IrcBroadcaster.cs.md), and adds the gateway as a hosted service, returning the original builder for fluent chaining.

## Remarks
This extension encapsulates opt-in startup logic and centralizes the wiring of the IRC gateway, ensuring consistent DI lifetimes and configuration handling across the app. It coordinates the lifecycle of [`IrcGatewayService`](IrcGatewayService.cs.md) and the broadcaster ([`IChatBroadcaster`](../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) implemented by [`IrcBroadcaster`](IrcBroadcaster.cs.md)) by hosting the gateway as a background service.

## Notes
- Calling `AddIrcGateway` multiple times can register multiple hosted services and singletons; call it once during startup to avoid duplicate registrations.
- The extension only activates when `Irc:Enabled` is true. If the flag is false or missing, it will configure [`IrcOptions`](IrcOptions.cs.md) but will not start or register the gateway components. Ensure configuration sources are loaded before invocation.