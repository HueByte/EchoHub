# RoomKeyProtector

> **File:** `src/EchoHub.Client/Services/RoomKeyProtector.cs`  
> **Kind:** class

```csharp
public sealed class RoomKeyProtector
```


Encrypts cached room content keys at rest so the client config never holds them as plain base64. Windows uses DPAPI (current-user scope, format prefix `dp1:`). On other platforms the keys are AES-GCM encrypted with a per-user master key file stored next to the config with permissions 0600 (prefix `k1:`) — without an OS keychain that is file-permission-level protection, not zero-knowledge: anyone who can read both the config and the key file can recover the room keys. Values with no recognized prefix are legacy plain-base64 keys from older clients; they load once and are re-encrypted. The room passphrase itself is never stored in any form.

The primary public surface consists of:
- `Protect(byte[] roomKey)`: encrypts a room key for storage in the config.
- `TryUnprotect(string stored, out byte[] roomKey, out bool wasLegacy)`: decrypts a stored value back into a room key.
The class caches the per-user master key and selects the protection mechanism based on the platform (DPAPI on Windows when enabled, otherwise the per-user master-key path). It also handles migration of legacy entries by re-encrypting them using the active scheme on subsequent saves. The constants `DpapiPrefix` and `KeyFilePrefix` label the on-disk formats, ensuring callers remain agnostic to the underlying storage strategy.