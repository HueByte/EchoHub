# IMessageEncryptionService

> **File:** `src/EchoHub.Core/Contracts/IMessageEncryptionService.cs`  
> **Kind:** interface

```csharp
public interface IMessageEncryptionService
```


The `IMessageEncryptionService` interface defines a centralized contract for encrypting and decrypting messages used in transit and at rest. It exposes a straightforward API to convert plaintext into ciphertext and back, while the `CiphertextPrefix` marks encrypted payloads so the implementation can transparently pass through values that are not encrypted. The `EncryptDatabaseEnabled` flag surfaces the server-side setting that indicates whether data stored in the database should be encrypted at rest, enabling callers to adapt their behavior to policy.

## Remarks

This abstraction minimizes scattered crypto logic by presenting a single, testable surface for encryption decisions. The pass-through behavior for content that does not begin with the `CiphertextPrefix` helps prevent double-encrypting and keeps compatibility with data already in plaintext. By providing nullable-aware methods (`EncryptNullable` and `DecryptNullable`), it cleanly handles optional values without forcing callers to perform boilerplate null checks at call sites.

## Example

```csharp
// Assume you have an instance of IMessageEncryptionService named `service`
string ciphertext = service.Encrypt("TopSecret");
string plaintext = service.Decrypt(ciphertext); // "TopSecret"

// Decrypting non-encrypted content yields the original value (pass-through)
string passthrough = service.Decrypt("plain-text"); // "plain-text"

string? nullableValue = null;
string? encNullable = service.EncryptNullable(nullableValue); // null
string? decNullable = service.DecryptNullable(encNullable); // null
```

## Notes
- The `CiphertextPrefix` ("$ENC$v1$") is a marker used to identify encrypted data. Decrypt will return the input unchanged if it does not start with this prefix.
- `EncryptDatabaseEnabled` reflects a server policy. It indicates whether data should be encrypted at rest, but callers must still invoke `Encrypt`/`EncryptNullable` before storage to ensure encryption occurs per policy.
