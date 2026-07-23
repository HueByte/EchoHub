# IrcServiceExtensions

> **File:** `src/EchoHub.Server.Irc/IrcServiceExtensions.cs`  
> **Kind:** class

```csharp
public static class IrcServiceExtensions
```


Extends WebApplicationBuilder with AddIrcGateway to wire up IRC gateway support. It reads a configuration flag to enable or disable the gateway and wires the necessary services when enabled, returning the builder for fluent startup configuration.

## Remarks
Centralizes startup concerns for the IRC gateway: the extension reads IrcOptions from a configured section and conditionally registers the gateway components, enabling the feature via configuration. It keeps startup code concise and tests-focused by encapsulating the wiring behind a single extension method.

## Notes
- IrcGatewayService and IrcBroadcaster registrations are conditional on Irc:Enabled; if false, IRC components are not registered.
- Ensure IrcOptions.SectionName matches your configuration so there is a valid section to bind from.
- Returning the builder enables fluent chaining like builder.AddIrcGateway().<other extensions>()