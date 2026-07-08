# IMessageEncryptionService

> **File:** `src/EchoHub.Core/Contracts/IMessageEncryptionService.cs`  
> **Kind:** interface

```csharp
public interface IMessageEncryptionService
```


IMessageEncryptionService defines encryption and decryption operations for string data and exposes a flag that indicates whether database-at-rest encryption is enabled by the server. Implementations are used by components that must securely persist or recover text, with Encrypt producing a ciphertext from plaintext and Decrypt restoring the original content. The nullable variants EncryptNullable and DecryptNullable provide safe handling for values that may be null, allowing callers to avoid extra null checks while preserving nullability semantics.

## Remarks
By abstracting the encryption strategy behind this interface, the rest of the system remains decoupled from the specifics of the algorithm, key management, or storage. This makes it easier to swap out implementations (for example, to change the encryption provider or to mock it during testing) without touching business logic. The EncryptDatabaseEnabled property reports the server-side decision about at-rest encryption and can guide decisions at runtime or in configuration code.

## Notes
- EncryptNullable(string? value) and DecryptNullable(string? value) gracefully handle null inputs; if the input is null, the corresponding result is null.
- Do not rely on a specific deterministic result across different implementations; keys, salts, or algorithms may vary between providers.
- EncryptDatabaseEnabled is informational and reflects server configuration rather than guaranteeing that every piece of data will be encrypted in all contexts.