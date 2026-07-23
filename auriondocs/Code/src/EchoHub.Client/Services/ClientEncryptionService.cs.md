# ClientEncryptionService

> **File:** `src/EchoHub.Client/Services/ClientEncryptionService.cs`  
> **Kind:** class

```csharp
public sealed class ClientEncryptionService : IMessageEncryptionService
```


ClientEncryptionService implements client-side encryption using AES-256-GCM to protect messages before sending them to the server, aligning with the server's ciphertext format so decryption occurs only with the shared key. After you provide a base64-encoded key via `SetKey`, it encrypts plaintext by generating a fresh 12-byte nonce and a 16-byte authentication tag, returning a string that starts with the `EncryptionPrefix` and includes base64-encoded nonce and payload; if no key has been set (`_key` is null), `Encrypt` returns the plaintext unchanged.

## Remarks
This class hides cryptography behind the [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) contract, offering a simple, predictable API for encryption and decryption while keeping key material private. It ensures that only a server-provisioned key enables encryption, and it produces self-contained ciphertext that carries its nonce and tag so the server can decrypt it reliably. The design also provides nullable-friendly helpers (`EncryptNullable`, `DecryptNullable`) to gracefully handle missing values.

## Example
```csharp
// Example usage of client-side encryption
var encryption = new ClientEncryptionService();
string base64Key = "<32-byte-base64-key>";
encryption.SetKey(base64Key);
string plaintext = "Secret message";
string ciphertext = encryption.Encrypt(plaintext);
string decrypted = encryption.Decrypt(ciphertext);
```

## Notes
- Encrypt before calling `SetKey` is a no-op: the input plaintext is returned unchanged when `_key` is null.
- Decrypt returns the original content if `_key` is null or the input does not start with the expected `EncryptionPrefix`.
- `SetKey` enforces a 32-byte (256-bit) key length and throws `InvalidOperationException` if the length is not exactly 32 bytes.
- Decryption errors are handled gracefully; if decryption fails for any reason, a sentinel message is returned: "[encrypted message — decryption failed, try re-logging to fetch the latest key]".