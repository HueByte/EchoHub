# ClientEncryptionService

> **File:** `src/EchoHub.Client/Services/ClientEncryptionService.cs`  
> **Kind:** class

```csharp
public sealed class ClientEncryptionService : IMessageEncryptionService
```


ClientEncryptionService provides client-side encryption for messages by applying AES-256-GCM using a key supplied by the server. It mirrors the server’s encryption format so messages are encrypted end-to-end between client and server. When no key has been set, Encrypt is a no-op and returns the plaintext to preserve compatibility with unauthenticated flows; once initialized, Encrypt produces a prefixed, base64-encoded payload containing the nonce and ciphertext+tag, and Decrypt reverses this process. If decryption fails due to a missing or mismatched key or corrupted data, a sentinel message is returned to indicate the failure and prompt re-authentication to refresh the key.

## Remarks
This abstraction isolates cryptography behind a single, testable service that can be swapped or disabled without changing business logic. It enforces a clear security boundary: encryption only happens after a server-provided key is loaded, reducing the risk of leaking plaintext. The pre-key pass-through behavior preserves compatibility with existing flows during login or in environments where the key has not yet been fetched.

## Example
```csharp
// Example: encrypt and decrypt with a server-provided key
var client = new ClientEncryptionService();

// Create a 32-byte key for demonstration (replace with real server-provided key)
var keyBytes = new byte[32];
var base64Key = Convert.ToBase64String(keyBytes);
client.SetKey(base64Key);

string plaintext = "Secret message";
string encrypted = client.Encrypt(plaintext);
string decrypted = client.Decrypt(encrypted);
// decrypted should equal plaintext
```

## Notes
- Encrypt and Decrypt only work after a 32-byte key has been provided via SetKey; otherwise Encrypt returns plaintext and Decrypt returns content unchanged.
- If the encrypted content is tampered with, the key is wrong, or the payload is malformed, Decrypt returns the special placeholder: "[encrypted message — decryption failed, try re-logging to fetch the latest key]".
- The key is held in memory and is not rotated automatically; ensure proper key management and re-fetch after key rotation on the server.