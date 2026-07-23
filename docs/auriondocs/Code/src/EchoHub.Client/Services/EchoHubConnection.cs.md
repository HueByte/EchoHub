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


Thrown when joining a channel fails because a password is required or the provided password is incorrect. The UI catches this to prompt the user for credentials and retry the join, using ChannelName to provide channel context.

## Remarks
ChannelPasswordRequiredException provides a precise signal for a password-related join failure. By carrying the ChannelName, it enables the UI to present a meaningful prompt and retry flow without inspecting lower-level errors. This focused exception helps keep join logic cohesive and testable by separating password-entry concerns from generic failure handling.

## Example
```csharp
try
{
    // Code that attempts to join a channel and may throw ChannelPasswordRequiredException
}
catch (ChannelPasswordRequiredException ex)
{
    Console.WriteLine($"Password is required to join channel '{ex.ChannelName}'.");
    // Prompt the user for a password and retry the join using the provided channel name
}
```

## Notes
- Be mindful that ChannelName may be null if constructed with null; guard accordingly before displaying it to users.

---

## EchoHubConnection
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class EchoHubConnection : IAsyncDisposable
```


A SignalR-backed client wrapper that manages a HubConnection to the Echo chat hub, integrates client-side encryption/room-key lookup, and exposes simple event callbacks for incoming messages, presence and channel events. Reach for EchoHubConnection when you need a higher-level, event-driven connection to the server that automatically handles authentication token provisioning and reconnect behavior while decrypting incoming payloads for the UI.

## Remarks
EchoHubConnection encapsulates the SignalR HubConnection lifecycle and maps server callbacks onto plain .NET events (e.g. OnMessageReceived, OnUserJoined, OnChannelUpdated). It supplies the HubConnectionBuilder with an AccessTokenProvider using the provided ApiClient so calls are authenticated, and it wires automatic-reconnect handlers that surface connection state changes via OnConnectionStateChanged and OnReconnected. Incoming MessageDto instances are passed through the client-side encryption pipeline (ClientEncryptionService and RoomKeyStore) so the UI sees decrypted content or a locked placeholder when a room key is not available.

## Example
```csharp
// Subscribe to events and inspect connection state
var echo = new EchoHubConnection(serverUrl, apiClient, encryptionService, roomKeyStore);

echo.OnMessageReceived += message =>
{
    // MessageDto is provided by the library; content may be the LockedMessagePlaceholder
    Console.WriteLine($"Message received in {message.ChannelName}: {message.Content}");
};

if (echo.IsConnected)
{
    Console.WriteLine("Currently connected to the chat hub.");
}

// Remember to dispose when finished
await echo.DisposeAsync();
```

## Notes
- Events are raised directly from SignalR callbacks; handlers may not run on a UI thread — marshal to the UI thread if required.
- Encrypted messages for channels without a stored key are replaced with LockedMessagePlaceholder; supply the channel passphrase (through the app's key store flow) to see decrypted content.
- Call DisposeAsync to release the underlying HubConnection and related resources to avoid background network activity.

---

## RoomLockedException
> **File:** `src/EchoHub.Client/Services/EchoHubConnection.cs`  
> **Kind:** class

```csharp
public sealed class RoomLockedException : Exception
```


Thrown to signal a security-sensitive condition when attempting to send a message into an end-to-end encrypted channel whose room key isn't cached. The operation is blocked to prevent sending plaintext; catching this exception lets the UI prompt for the channel's passphrase and unlock the room before retrying.

## Remarks

RoomLockedException acts as a clear boundary between encryption policy and transport logic. By exposing the ChannelName, callers can present a channel-scoped unlock prompt without parsing the error text, and the sealed Exception type communicates a concrete, expected failure mode that downstream code can handle distinctly from generic errors.

## Example

```csharp
try
{
    // Simulated scenario: an attempt to send into a locked E2E channel
    throw new RoomLockedException("Lobby");
}
catch (RoomLockedException ex)
{
    // Use the information to drive the unlock UX
    Console.WriteLine(ex.Message);
    Console.WriteLine($"Unlock channel: {ex.ChannelName} by entering its passphrase.");
}
```

## Notes

- Do not swallow this as a generic error; catch RoomLockedException to trigger the unlock UX and use ex.ChannelName to identify the affected channel. The displayed message is user-facing and not localized.

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


JoinOutcome is a sealed record that represents the result of joining a channel: it includes the decrypted history (History) and, for end-to-end encrypted channels, the key envelope necessary to unlock the room's content key (WrappedRoomKey). EncryptionSalt is the salt used to derive the encryption key when applicable. This type is typically produced by the join logic and consumed by the UI to render messages and initialize decryption if needed.

## Remarks
This abstraction centralizes the outcome of a join into a single, immutable value that downstream components can rely on. The History is always present (even if empty), while EncryptionSalt and WrappedRoomKey are nullable to reflect that some rooms are not end-to-end encrypted or that keys may not be provisioned yet. By grouping history and encryption metadata together, the join logic can separate concerns: rendering chat versus handling cryptographic setup.

## Example

```csharp
using System.Collections.Generic;

List<MessageDto> history = new List<MessageDto>();
var joinResult = new JoinOutcome(history, null, null);
```

## Notes
- EncryptionSalt and WrappedRoomKey can be null; callers should verify non-null before attempting decryption-related steps.

---