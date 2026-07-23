# ConnectionManager.cs

> **Source:** `src/EchoHub.Client/Services/ConnectionManager.cs`

## Contents

- [ConnectionManager](#connectionmanager)
- [ConnectResult](#connectresult)

---

## ConnectionManager
> **File:** `src/EchoHub.Client/Services/ConnectionManager.cs`  
> **Kind:** class

```csharp
internal sealed class ConnectionManager : IAsyncDisposable
```


Manages the full lifecycle of a live chat connection: authenticating with the server, establishing end-to-end encryption keys, creating and wiring the SignalR (EchoHub) connection, tracking joined channels, and exposing a thin event surface that the UI (AppOrchestrator) can subscribe to. Use this when you want a single, high-level component to own connection state and SignalR event forwarding instead of manipulating ApiClient and EchoHubConnection directly.

## Remarks
This class centralizes the responsibilities that would otherwise be scattered across UI code: authentication and token rotation, attempting to fetch and apply the E2E encryption key, instantiating and wiring an EchoHubConnection, and keeping track of which channels have been joined. It forwards SignalR events as simple .NET events so the UI layer can react without needing to know SignalR details. ConnectionManager also implements IAsyncDisposable so callers can cleanly tear down both the EchoHubConnection and the underlying ApiClient.

## Notes
- ConnectionManager may raise forwarded events from background threads (SignalR callbacks). UI handlers should marshal to the UI thread if required by the UI framework.
- ConnectAsync reports progress via the onStatus callback and will throw on authentication failure; callers are expected to handle expired saved sessions or retry logic.
- Failure to fetch the encryption key is treated as non-fatal: the manager logs a warning and proceeds without message encryption.
- Dispose of the manager (DisposeAsync) when the app shuts down to ensure the hub connection and ApiClient are cleaned up.

---

## ConnectResult
> **File:** `src/EchoHub.Client/Services/ConnectionManager.cs`  
> **Kind:** record

```csharp
internal record ConnectResult(
    LoginResponse Login,
    List<ChannelDto> Channels,
    Dictionary<string, List<MessageDto>> Histories)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Login` | [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) | — |
| `Channels` | `List<ChannelDto>` | — |
| `Histories` | `Dictionary<string, List<MessageDto>>` | — |


Represents the outcome of a successful connection, returned to AppOrchestrator for UI updates. It bundles the authentication result, the current set of channels, and the initial histories for every auto-joined channel (keyed by channel name and including the default channel). As an immutable record, it serves as a single, self-contained snapshot that the UI can bootstrap from after a connect.

## Remarks
This object centralizes the data needed to render the initial connected state, decoupling the connection logic from the UI orchestration. By passing a single ConnectResult, the AppOrchestrator can immediately populate channel lists and histories without issuing additional fetches, promoting a clean separation between connection handling and presentation concerns.

## Notes
- ConnectResult is immutable; to reflect changes (e.g., new messages or channels), construct and pass a new instance rather than mutating the existing one.
- Histories is a dictionary keyed by channel name that contains the initial per-channel histories; ensure channel names in the dictionary align with the Channels list to avoid inconsistencies.

---