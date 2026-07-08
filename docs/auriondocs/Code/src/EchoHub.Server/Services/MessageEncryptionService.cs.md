# MessageEncryptionService

> **File:** `src/EchoHub.Server/Services/MessageEncryptionService.cs`  
> **Kind:** class

```csharp
public class MessageEncryptionService : IMessageEncryptionService
```


MessageEncryptionService encapsulates AES-GCM-based encryption and decryption for message payloads, sourcing a 256-bit key from configuration and exposing a straightforward Encrypt/Decrypt API. It prefixes encrypted payloads with a stable marker, base64-encodes the nonce and the ciphertext+tag, and preserves legacy plaintext content that does not follow the encrypted format.

## Remarks
It acts as a centralised cryptographic service for EchoHub, tying configuration, logging, and crypto together to provide a consistent, secure encoding for messages. By enforcing a 256-bit key and a uniform storage format (EncryptionPrefix, nonce, and base64-encoded ciphertext+tag), it prevents ad-hoc encryption approaches and simplifies cross-component interoperability. The service also gracefully handles non-encrypted (legacy) content and decryption failures by returning safe sentinel values rather than throwing, which helps maintain robustness in mixed environments.

## Notes
- The encryption key must decode to exactly 32 bytes (256 bits); otherwise the constructor throws, catching configuration mistakes early.
- Decrypt distinguishes legacy plaintext (no prefix) from encrypted content and, on malformed input, logs a warning or error and returns a sentinel string to avoid leaking data.
- The encrypted payload format uses a fixed prefix, the nonce, and a base64-encoded blob of ciphertext concatenated with its authentication tag, separated from the nonce by a '$' delimiter.
