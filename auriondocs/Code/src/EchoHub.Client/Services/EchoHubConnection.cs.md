# EchoHubConnection.cs

> **Source:** `src/EchoHub.Client/Services/EchoHubConnection.cs`

## Contents

- [ChannelPasswordRequiredException](#channelpasswordrequiredexception)
- [EchoHubConnection](#echohubconnection)
- [RoomLockedException](#roomlockedexception)
- [JoinOutcome](#joinoutcome)

---

## ChannelPasswordRequiredException
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class ChannelPasswordRequiredException : Exception
```


ChannelPasswordRequiredException represents the domain condition that a join operation on a channel cannot proceed because a password is required or the provided password was invalid. It is intended to be caught by the UI layer, which then prompts the user for the correct password and retries the join operation. The exception carries the channel name via the `ChannelName` property to identify which channel needs authentication.

## Remarks
Using a distinct exception type to signal password-related authentication flows keeps the connection logic decoupled from the UI. The `ChannelName` property provides channel-specific context for prompts, enabling precise feedback such as prompting for the password of the channel identified by `ChannelName` when retrying.

## Notes
- Use a specific catch for `ChannelPasswordRequiredException` rather than a broad catch of `Exception`, to avoid handling unrelated failures; access the `ChannelName` to present a contextual, channel-specific prompt.

---

## EchoHubConnection
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class EchoHubConnection : IAsyncDisposable
```


A lightweight, event-driven wrapper around a SignalR `HubConnection` that manages authentication, reconnection and client-side handlers for the chat protocol. Use `EchoHubConnection` when you need a high-level, strongly-typed bridge between the server's [`IEchoHubClient`](../../EchoHub.Core/Contracts/IEchoHubClient.cs.md) callbacks and your UI or application logic — it registers the server method handlers, decrypts incoming content, exposes simple events (for messages, presence, channel updates, errors, etc.), and surfaces connection state changes.

## Remarks
`EchoHubConnection` centralizes SignalR integration concerns: it creates and configures the underlying `HubConnection` (including token provisioning via the provided [`ApiClient`](ApiClient.cs.md)), wires up automatic reconnect behavior, and maps server-invoked methods to public events such as `OnMessageReceived`, `OnUserJoined`, `OnChannelUpdated`, and others. Incoming [`MessageDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) instances are run through the connection's decryption path (see `DecryptMessage`/`DecryptField`) before being forwarded, and encrypted content that cannot be unlocked is replaced by the `LockedMessagePlaceholder`. The class implements `IAsyncDisposable` so consumers should `await DisposeAsync()` to cleanly stop the connection.

## Example
```csharp
// Assume these are already created: serverUrl (string), apiClient (ApiClient),
// encryption (ClientEncryptionService), roomKeys (RoomKeyStore).
var connection = new EchoHubConnection(serverUrl, apiClient, encryption, roomKeys);

connection.OnConnectionStateChanged += state => Console.WriteLine($"State: {state}");
connection.OnMessageReceived += message => Console.WriteLine($"Message from {message.From}: {message.Content}");
connection.OnReconnected += () => Console.WriteLine("Reconnected to hub");

// When finished with the connection:
await connection.DisposeAsync();
```

## Notes
- Event handlers are invoked from the SignalR callbacks — subscribers should ensure any UI updates or shared-state mutations are marshalled to the correct synchronization context or made thread-safe.
- Encrypted message content is represented by the `LockedMessagePlaceholder` when the client lacks the room key; rejoining the channel with the passphrase (and so populating [`RoomKeyStore`](RoomKeyStore.cs.md)) is required to decrypt those contents.
- `IsConnected` reflects the underlying `HubConnection.State` at the moment of access and may change shortly after; use `OnConnectionStateChanged` and `OnReconnected` for lifecycle-driven logic.
- Attempting to join a password-protected channel can surface a `ChannelPasswordRequiredException` — callers that perform join flows should handle that explicitly.

---

## RoomLockedException
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class RoomLockedException : Exception
```


`RoomLockedException` is thrown when attempting to send into an end-to-end encrypted channel whose room key isn’t cached. Without the key, the operation would emit plaintext, which must never happen, so the exception blocks the send. The `ChannelName` property exposes which channel is locked, and the constructor formats the failure message to include `#{channelName}` to guide unlocking.

## Remarks
This exception acts as a boundary between encryption state and message-sending logic. It is a domain-level signal distinct from other transport or I/O failures, enabling callers to trigger a user prompt to unlock the channel and retry the operation once unlocked. The `ChannelName` property ties the failure to a specific channel, enabling precise remediation flows.

---

## JoinOutcome
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** record

```csharp
public sealed record JoinOutcome(List<MessageDto> History, string? EncryptionSalt, string? WrappedRoomKey)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `History` | `List<MessageDto>` | — |
| `EncryptionSalt` | `string?` | — |
| `WrappedRoomKey` | `string?` | — |


Represents the result of joining a channel: the decrypted message history and, for end-to-end encrypted rooms, the key envelope needed to unlock the room content key. `History` is a `List<MessageDto>` containing the decrypted messages, and `WrappedRoomKey` (with optional `EncryptionSalt`) provides the cryptographic envelope when encryption is in play.

## Remarks
By encapsulating the join outcome in a single type, the caller can render history and prepare for decryption in one step. The nullable `WrappedRoomKey` and `EncryptionSalt` signal whether encryption is active for the channel; callers not using end-to-end encryption can ignore them. This keeps the join path concise while preserving a clear contract about what data is available after join.

## Notes
- `EncryptionSalt` and `WrappedRoomKey` are nullable; guard for nulls and only attempt decryption when these values are provided.

---