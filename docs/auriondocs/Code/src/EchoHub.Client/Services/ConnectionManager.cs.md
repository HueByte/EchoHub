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


Manages a server connection end-to-end: handles authentication via [`ApiClient`](ApiClient.cs.md), establishes end-to-end encryption, creates and wires an [`EchoHubConnection`](EchoHubConnection.cs.md), tracks joined channels, and exposes SignalR events so higher-level orchestrators can react without touching connection internals. Reach for `ConnectionManager` when you want UI code (for example an [`AppOrchestrator`](../AppOrchestrator.cs.md)) to observe connection and chat events through simple events rather than managing [`ApiClient`](ApiClient.cs.md) and [`EchoHubConnection`](EchoHubConnection.cs.md) yourself.

## Remarks
`ConnectionManager` centralizes lifecycle concerns: it authenticates (login/registration/refresh), subscribes to token rotation, attempts to fetch and apply the E2E encryption key, constructs and registers handlers on the [`EchoHubConnection`](EchoHubConnection.cs.md), and ensures channel membership state is tracked. It forwards the hub's runtime events (for example `MessageReceived`, `UserJoined`, `ChannelUpdated`) so callers receive high-level notifications and do not need to bind SignalR handlers directly. The class is intended as the single place that composes [`ApiClient`](ApiClient.cs.md), [`ClientEncryptionService`](ClientEncryptionService.cs.md)/[`RoomKeyStore`](RoomKeyStore.cs.md), and [`EchoHubConnection`](EchoHubConnection.cs.md) into a usable connection for the UI.

## Notes
- `ConnectAsync` reports progress via the `onStatus` callback and will throw on authentication failure — callers are expected to handle saved-session expiry and similar error flows.  
- Event handlers (for example `MessageReceived`, `UserJoined`, `ConnectionStatusChanged`) may be invoked from signalr/connection threads; subscribers should not assume they run on the UI thread and must marshal to the UI thread when necessary.  
- Always `await` disposing the manager (it implements `IAsyncDisposable`) so underlying resources such as the [`EchoHubConnection`](EchoHubConnection.cs.md) and [`ApiClient`](ApiClient.cs.md) are cleanly released; failing to do so can leave connections or background work active.

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


ConnectResult represents the payload returned after a successful connection, carrying everything the [`AppOrchestrator`](../AppOrchestrator.cs.md) needs to update the UI. It includes the authenticated login information (`Login`), the collection of available channels (`Channels`), and the initial per-channel histories (`Histories`), where each channel name maps to its starting list of messages, always including the default channel.

## Remarks
ConnectResult is a `record`, so it participates in value-based equality and can be treated as a single unit when comparing connection outcomes. Note that its `Channels` and `Histories` collections are mutable (`List<ChannelDto>` and `Dictionary<string, List<MessageDto>>`); if you need true immutability, expose read-only wrappers or clone the collections when passing them onward.

## Notes
- The contained `List<ChannelDto>` and `Dictionary<string, List<MessageDto>>` are mutable; avoid mutating them in place and consider treating the `ConnectResult` as a snapshot that should be cloned if you require immutability downstream.

---