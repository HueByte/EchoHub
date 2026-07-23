# MessageEncryptionService

> **File:** `src/EchoHub.Server/Services/MessageEncryptionService.cs`  
> **Kind:** class

```csharp
public class MessageEncryptionService : IMessageEncryptionService
```


MessageEncryptionService is a server-side component that encrypts and decrypts text using AES-GCM with a 256-bit key sourced from configuration. It implements [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) and exposes `Encrypt`, `Decrypt`, `EncryptNullable`, and `DecryptNullable`. Use it when you need to store or transmit sensitive strings (for example, in a database) without exposing plaintext. Each encrypted value is prefixed with the configured `CiphertextPrefix` and serialized as a base64-encoded nonce followed by a base64-encoded payload containing the ciphertext and authentication tag, enabling safe storage and later decryption with the same key. If a value supplied to `Decrypt` does not begin with the encryption prefix, the service treats it as legacy plaintext and returns it unchanged. When decryption fails for any reason, the service logs the issue and returns the placeholder string `[encrypted message — decryption failed]` to avoid leaking cryptographic details.

## Remarks
MessageEncryptionService centralizes cryptographic logic to isolate security concerns from business code. It provides a single, testable path for encryption and decryption and ensures consistent storage format for encrypted data, which simplifies auditing and data integrity checks. The class reads a 256-bit key at startup from `Encryption:Key` (as Base64) and validates its length, making key management explicit and failure-revealing at boot time; the `EncryptDatabaseEnabled` flag controls whether database encryption should be active, enabling or disabling encryption behavior without code changes.

## Notes
- Do not rotate the encryption key at runtime; the key is loaded once during construction and would render previously encrypted data unreadable.
- The class is thread-safe for concurrent use since it creates a new `AesGcm` instance per operation and does not share mutable state.
- Non-prefixed content is treated as legacy plaintext, ensuring backward compatibility with data that predates server-side encryption.