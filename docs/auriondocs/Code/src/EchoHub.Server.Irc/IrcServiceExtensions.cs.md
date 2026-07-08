# IrcServiceExtensions

> **File:** `src/EchoHub.Server.Irc/IrcServiceExtensions.cs`  
> **Kind:** class

```csharp
public static class IrcServiceExtensions
```


IrcServiceExtensions provides a single extension to WebApplicationBuilder that wires up the IRC gateway components. Its AddIrcGateway method configures IrcOptions from the configuration and, when Irc:Enabled is true, registers the IrcGatewayService as a singleton, maps IChatBroadcaster to IrcBroadcaster, and starts the gateway as a hosted background service by resolving the IrcGatewayService from the DI container. The extension returns the builder to support fluent startup configuration.

## Remarks
By encapsulating startup wiring, this extension keeps IRC-related initialization isolated behind a single hook and makes activation configurable via Irc:Enabled. It centralizes how the IRC gateway is registered and started, so callers can enable or disable IRC support without needing to know about the concrete services or hosting details.

## Notes
- When Irc:Enabled is false (the default if the value is missing), no IRC services are registered and no IRC listeners run.
- IrcOptions is bound from the configuration section named by IrcOptions.SectionName; ensure your configuration provides that section with the expected keys.
- The hosted service is wired via a factory that resolves the IrcGatewayService from DI, so the gateway lifecycle follows the application's host lifecycle.