# ClientEncryptionService

> **File:** `src/EchoHub.Client/Services/ClientEncryptionService.cs`  
> **Kind:** class

```csharp
public sealed class ClientEncryptionService : IMessageEncryptionService
```


ClientEncryptionService provides client-side AES-256-GCM encryption, aligning with the server's format so messages are encrypted end-to-end between client and server. Use it after obtaining the server's 32-byte key via SetKey; if the key isn't set, Encrypt passes through unmodified.

## Remarks
This abstraction encapsulates cryptography details and payload formatting, shielding callers from the exact AES-GCM layout and the prefix. It ensures that all outgoing messages follow a single, server-compatible scheme and can be decrypted by the server with the same key.

## Example
```csharp
// After obtaining the server-provided key
var encryptionService = new ClientEncryptionService();
encryptionService.SetKey(serverBase64Key);

string plaintext = "Secret message";
string encrypted = encryptionService.Encrypt(plaintext);
string decrypted = encryptionService.Decrypt(encrypted);
// decrypted should equal plaintext
```

## Notes
- Encrypt before SetKey is called acts as a no-op; the plaintext is returned unchanged.
- If decryption fails at runtime, the method returns a sentinel string: "[encrypted message — decryption failed, try re-logging to fetch the latest key]".
- SetKey will throw InvalidOperationException if the key is not exactly 32 bytes (256 bits).