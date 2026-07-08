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


Owns the application-level connection lifecycle: authentication, token rotation handling, end-to-end encryption key setup, SignalR/Echo hub wiring, and tracking of joined channels. Exposes a set of events that forward real-time hub notifications (messages, presence, channel updates, errors, connection status, etc.) so higher-level orchestration (for example an AppOrchestrator or UI layer) can react without dealing with low-level connection or auth details.

## Remarks
This class centralizes the integration between the ApiClient (HTTP API, login/refresh, server queries), ClientEncryptionService (E2E key management) and EchoHubConnection (SignalR realtime transport). It performs the full connect flow: authenticate (login/register/refresh), persist rotated refresh tokens, attempt to fetch and install the encryption key, create and wire an EchoHubConnection, establish the SignalR connection, then join the default channel and retrieve history. Events are surface-level forwards from the underlying connection so consumers only subscribe here instead of wiring multiple hub handlers themselves.

## Notes
- ConnectAsync will throw on authentication failure — callers are expected to handle saved-session expiry and related UX flows.
- Failure to fetch an encryption key is non-fatal: the code logs a warning and continues with an unencrypted message flow (messages will not be encrypted).
- IsAuthenticated reflects whether an ApiClient instance exists, not a guaranteed valid token; callers should rely on Connect/Disconnect semantics and ApiClient behavior for token validity.
- The class does not show explicit synchronization primitives in the visible code; avoid calling its public async operations concurrently from multiple threads without external coordination.

---

## ConnectResult
> **File:** `src/EchoHub.Client/Services/ConnectionManager.cs`  
> **Kind:** record

```csharp
internal record ConnectResult(
    LoginResponse Login,
    List<ChannelDto> Channels,
    List<MessageDto> DefaultHistory)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Login` | [`LoginResponse`](../../EchoHub.Core/DTOs/AuthDtos.cs.md) | — |
| `Channels` | `List<ChannelDto>` | — |
| `DefaultHistory` | `List<MessageDto>` | — |


ConnectResult represents the payload produced after a successful connection: it carries the Login (LoginResponse), the channels the user can access, and the initial message history to display. It is returned to the AppOrchestrator to drive the UI after a connection is established, ensuring a single, coherent payload is used to refresh the interface.

## Remarks
ConnectResult serves as a simple data-transfer object between the connection subsystem and the UI layer. By grouping login, channels, and history, it ensures that the UI can render the post-connect state consistently, and it provides a clear contract for what the connection operation yields. Keeping it as a single record also makes it straightforward to extend with additional post-connect data in the future without changing call sites.

## Notes
- The Channels and DefaultHistory properties are `List<T>`, which means the collections are mutable; callers should treat them as read-only or copy defensively.
- As an internal type, ConnectResult is not part of the public API surface; components outside the assembly should not rely on it directly.

---