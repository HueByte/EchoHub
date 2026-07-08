# EchoHubConnection

> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class EchoHubConnection : IAsyncDisposable
```


EchoHubConnection is a client-side wrapper around a SignalR hub that connects to the EchoHub chat service. It decrypts incoming MessageDto content, exposes a set of strongly-typed events (such as OnMessageReceived, OnUserJoined, OnChannelUpdated, etc.), and handles connection lifecycle (auto-reconnect and token provisioning) so the rest of the app can react to chat activity without dealing with the hub specifics.

## Remarks
EchoHubConnection decouples the UI from the transport and encryption details by translating server callbacks into domain events. It centralizes token retrieval via ApiClient and keeps encryption concerns isolated in ClientEncryptionService, while the internal RegisterHandlers wiring maps the hub's IEchoHubClient events to the corresponding public events. This abstraction provides a resilient, easy-to-consume surface for chat functionality without requiring UI code to manage SignalR directly.

## Example
```csharp
var hub = new EchoHubConnection("https://server.example", apiClient, encryption);
hub.OnMessageReceived += message => Console.WriteLine(message.Content);
hub.OnChannelUpdated += channel => Console.WriteLine($"Channel updated: {channel.Name}");
```

## Notes
- Dispose of the connection when it's no longer needed using asynchronous disposal to gracefully stop the hub and release resources.
- The connection state messages exposed via OnConnectionStateChanged are human-friendly strings (e.g., "Reconnecting...", "Connected", "Disconnected"); rely on OnReconnected for confirmation that the connection has been re-established. If you need strict state monitoring, combine these signals rather than parsing exact strings.