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


Owns the full client-side connection lifecycle: authenticating via the `ApiClient`, establishing and wiring an `EchoHubConnection` (SignalR) for realtime events, enabling end-to-end encryption via the `ClientEncryptionService`, and tracking channel membership in `RoomKeyStore` and `_joinedChannels`. Reach for `ConnectionManager` when you want a single, high-level component to manage connection setup, token refresh handling, event forwarding, and channel join/leave logic instead of manipulating `ApiClient` and `EchoHubConnection` directly.

## Remarks
`ConnectionManager` is the orchestration point between the networking primitives (`ApiClient` and `EchoHubConnection`) and the UI layer. It centralizes responsibility for: authenticating (including login, register, and refresh-token flows), persisting rotated refresh tokens via `OnTokensRefreshed`/`HandleTokensRefreshed`, attempting to establish an E2E encryption key with `ClientEncryptionService`, and forwarding SignalR events to consumers through its public events (for example `MessageReceived`, `UserJoined`, `ChannelUpdated`, and `ConnectionStatusChanged`). By exposing `IsConnected`, `IsAuthenticated`, `Api`, and `RoomKeys`, it gives callers enough state to update UI and perform API operations without needing to manage low-level connection state or event wiring.

## Notes
- `ConnectAsync` throws on authentication failure — callers are expected to handle saved-session expiry and related UI flows.  See the `ConnectAsync` progress messages for how the method reports intermediate status.
- The class disposes and replaces the internal `ApiClient` during `ConnectAsync` (it calls `_apiClient?.Dispose()`), and implements `IAsyncDisposable`; callers should ensure `DisposeAsync` is invoked when the manager is no longer needed to avoid resource leaks.
- Encryption is best-effort: if fetching the encryption key fails (`GetEncryptionKeyAsync`), the manager logs a warning and continues with an unencrypted session — consumers should not assume messages are always encrypted.
- The implementation mutates internal fields like `_apiClient`, `_connection`, and `_joinedChannels` without visible synchronization. The class appears intended for single-threaded/UI-thread usage; consumers that access it from multiple threads should serialize calls externally to avoid race conditions.

---

## ConnectResult
> **File:** `src/EchoHub.Client/Services/ConnectionManager.cs`  
> **Kind:** record

```csharp
internal record ConnectResult(
    LoginResponse Login,
    List<ChannelDto> Channels,
    Dictionary<string, List<MessageDto>> Histories,
    ServerStatusDto? ServerInfo = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Login` | `LoginResponse` | — |
| `Channels` | `List<ChannelDto>` | — |
| `Histories` | `Dictionary<string, List<MessageDto>>` | — |
| `ServerInfo` | `ServerStatusDto?` | `null` |


ConnectResult is an internal, immutable `record` that represents the successful outcome of establishing a connection and is returned to the `AppOrchestrator` to drive UI updates. It bundles the login information (`Login`) of type `LoginResponse`, the joined channels (`Channels`) as `List<ChannelDto>`, the initial per-channel histories (`Histories`) as `Dictionary<string, List<MessageDto>>`, and optional server status (`ServerInfo`) as `ServerStatusDto?`. The `Histories` dictionary maps channel names to their corresponding history lists and always includes the default channel.

## Remarks
ConnectResult acts as a single, UI-facing snapshot of the connected state. It collects authentication results, channel roster, initial per-channel histories, and optional server health/status so the `AppOrchestrator` can immediately render the connected view without issuing further requests.

## Notes
- ConnectResult is immutable; use a `with` expression to derive a modified copy rather than mutating the existing instance.
- `ServerInfo` may be null; callers should handle absence gracefully.

---