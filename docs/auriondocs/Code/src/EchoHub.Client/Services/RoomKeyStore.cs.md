# RoomKeyStore

> **File:** `src/EchoHub.Client/Services/RoomKeyStore.cs`  
> **Kind:** class

```csharp
public sealed class RoomKeyStore
```


Holds and manages end-to-end encrypted room keys for a single client instance: it keeps a decrypted, in-memory cache for the active session and a per-server persisted, encrypted copy so users do not have to re-enter passphrases each launch. Use RoomKeyStore when you need a thread-safe local store that provides room keys to the runtime and ensures keys are encrypted at rest via RoomKeyProtector.

## Remarks
RoomKeyStore links transient runtime state with the client's persisted configuration. It binds to a server (LoadForServer), loads that server's saved ChannelKeys (unprotecting them with RoomKeyProtector), and exposes methods to read, add, replace, or remove keys while persisting changes back to the SavedServer entry. It also records which channels are known to be encrypted so callers can avoid emitting plaintext into rooms without a cached key. The class performs a one-way upgrade of legacy unprotected entries to the protected format when possible and logs unreadable entries rather than failing.

## Example
```csharp
var store = new RoomKeyStore();
store.LoadForServer("https://chat.example.com");

// Generate and store a new room key for a channel
byte[] newKey = RoomCrypto.GenerateRoomKey();
store.StoreKey("#team-room", newKey);

// Retrieve a key for sending encrypted messages
if (store.TryGetKey("#team-room", out var key))
{
    // Use `key` with RoomCrypto API to encrypt message content
}

// Accept an encrypted envelope and store the unwrapped key only if the KEK opens it
string wrapped = "..."; // envelope string received
byte[] kek = /* key-encryption-key */ new byte[RoomCrypto.KeySizeBytes];
if (store.TryStoreFromEnvelope("#other-room", wrapped, kek))
{
    // successfully unwrapped and cached
}
```

## Notes
- Call LoadForServer(serverUrl) before persisting or retrieving server-scoped keys; the store clears and reinitializes its cache when bound to a server.  
- Legacy (plain/base64) saved entries are upgraded to the protector-backed format when possible; entries that cannot be unprotected are ignored and logged.  
- The class uses an internal lock for basic thread-safety of the in-memory cache; avoid holding returned keys while performing long synchronous work that might race with store mutations.