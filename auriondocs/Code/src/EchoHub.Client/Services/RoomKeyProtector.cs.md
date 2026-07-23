# RoomKeyProtector

> **File:** `src/EchoHub.Client/Services/RoomKeyProtector.cs`  
> **Kind:** class

```csharp
public sealed class RoomKeyProtector
```


Encrypts cached room content keys at rest so the client config never holds them as plain base64. Windows uses DPAPI (current-user scope, format prefix "dp1:"). On other platforms the keys are AES-GCM encrypted with a per-user master key file stored next to the config with 0600 permissions (prefix "k1:") — without an OS keychain that is file-permission-level protection, not zero-knowledge: anyone who can read both the config and the key file can recover the room keys. Values with no recognized prefix are legacy plain-base64 keys from older clients; they load once and are re-encrypted. The room passphrase itself is never stored in any form.

The RoomKeyProtector class provides a single API surface to protect and unprotect per-user room keys across platforms. The Protect method returns a string suitable for storage in the config, automatically selecting the appropriate protection mechanism for the current OS (DPAPI on Windows, file-based AES-GCM on others). TryUnprotect decodes a stored value back into a room key, reporting whether the value was a legacy (unencrypted) entry and whether the decryption succeeded. The implementation intentionally hides platform differences behind a consistent interface, so callers can persist and reload keys without worrying about the underlying cryptosystem.

The constructor accepts a directory that holds the master key file and an optional flag to override the OS-provided protection path (useful for tests). The key file path is derived from the directory by appending the fixed file name roomkeys.key. Key loading is guarded by a small lock and the master key is cached after the first read. The Protect path prefixes the output to indicate how the data is protected ("dp1:" or "k1:").

The class ensures the room passphrase itself is never persisted, and it gracefully tolerates missing or unreadable key material by returning false from TryUnprotect (leaving the caller to prompt the user for action).

````csharp
// Typical usage
var protector = new RoomKeyProtector("/config");
byte[] roomKey = new byte[32]; // obtain from a secure source
string stored = protector.Protect(roomKey);

if (protector.TryUnprotect(stored, out var recovered, out bool wasLegacy))
{
    // recovered contains the room key if the value was decryptable
    // wasLegacy is true only if the input was a legacy base64 key without a prefix
}
````
