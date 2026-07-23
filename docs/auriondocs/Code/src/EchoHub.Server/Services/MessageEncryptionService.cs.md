# MessageEncryptionService

> **File:** `src/EchoHub.Server/Services/MessageEncryptionService.cs`  
> **Kind:** class

```csharp
public class MessageEncryptionService : IMessageEncryptionService
```


MessageEncryptionService provides AES-GCM-based encryption and decryption for strings using a 256-bit key loaded from configuration, returning ciphertexts in a standardized prefixed, base64-encoded format. Callers use it when they need authenticated encryption with a consistent storage format and null-safety helpers.

## Remarks
By centralizing the encryption logic, this class ensures all encrypted messages share the same nonce handling, tag size, and output format, which simplifies storage and auditing across clients and servers. It also enforces key validation upfront and uses dependency-injected logging to surface decryption problems and protect the caller from exceptions. The EncryptNullable/DecryptNullable helpers make it convenient to encode optional values without duplicating boilerplate.

## Notes
- Key retrieval and validation: the constructor reads Encryption:Key from configuration as a Base64 string and requires exactly 32 bytes; otherwise it throws InvalidOperationException.
- Decryption safety and error handling: if content doesn't start with the CiphertextPrefix, it is treated as legacy plaintext; malformed payloads log a warning and yield "[encrypted message — decryption failed]"; any exception results in a logged error and the same sentinel output.
- Null handling convenience: EncryptNullable and DecryptNullable gracefully handle null inputs without throwing.