# RoomKeyStore

> **File:** `src/EchoHub.Client/Services/RoomKeyStore.cs`  
> **Kind:** class

```csharp
public sealed class RoomKeyStore
```


Holds and manages room content keys for end-to-end encrypted channels for the active session and the persisted per-server client configuration. Use `RoomKeyStore` when you need a single place to cache decrypted room keys in memory, persist them encrypted to the local config (so users don't retype passphrases on each launch), and track which channels are known to be end-to-end encrypted.

## Remarks
`RoomKeyStore` is the in-process authority for room keys: it keeps a memory cache (`_keys`) for the running session and a set (`_encryptedChannels`) to mark channels that are treated as encrypted. It delegates on-disk protection to [`RoomKeyProtector`](RoomKeyProtector.cs.md) so keys never leave the machine in plaintext. Calling `LoadForServer` binds the store to a specific server URL, loads that server's `SavedServer.ChannelKeys` via `ConfigManager.Load()`, and hydates the in-memory cache (skipping unreadable entries). Legacy plaintext/legacy-storage entries detected by `RoomKeyProtector.TryUnprotect` are re-encrypted and re-persisted as a one-way upgrade. All public mutation and lookup methods synchronize on the internal `Lock` (`_lock`) to provide basic thread-safety for concurrent callers.

## Notes
- `TryGetKey` returns the stored byte array reference from the internal `_keys` map (no defensive copy). Callers must not mutate the returned `byte[]` in-place — clone it first if modification is required.
- Channel name lookup is case-insensitive because the internal collections use `StringComparer.OrdinalIgnoreCase`. Treat channel names consistently to avoid duplicate/lookup surprises.
- Loading ignores unreadable cached entries and will re-persist only entries that [`RoomKeyProtector`](RoomKeyProtector.cs.md) could successfully unprotect; `TryStoreFromEnvelope` returns false when the provided KEK fails to unwrap the envelope and will leave the cache unchanged. Storing or removing a key persists the corresponding `SavedServer.ChannelKeys` entry immediately (via the store's persistence path).