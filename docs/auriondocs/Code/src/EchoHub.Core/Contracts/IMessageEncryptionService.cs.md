# IMessageEncryptionService

> **File:** `src/EchoHub.Core/Contracts/IMessageEncryptionService.cs`  
> **Kind:** interface

```csharp
public interface IMessageEncryptionService
```


IMessageEncryptionService defines a pluggable contract for encrypting and decrypting string data, using a distinctive prefix to mark encrypted content so callers can distinguish ciphertext from plain text and pass through non-encrypted values safely. It also exposes EncryptDatabaseEnabled to reflect the server setting for encrypting database content at rest, and provides nullable variants to handle optional fields without extra null checks.

## Remarks
This interface acts as a thin abstraction that isolates encryption concerns from business logic, enabling swap-in of different algorithms or key-management strategies without touching call sites. The public CiphertextPrefix and the Decrypt pass-through behavior for non-encrypted values provide a simple, deterministic convention for distinguishing encrypted payloads. The nullable variants help preserve nullability semantics in data-transfer surfaces while still enabling encryption when a value is present.

## Example
```csharp
// Given an IMessageEncryptionService implementation (injected or resolved via DI)
IMessageEncryptionService service = ...;

string plain = "customer-secret";
string cipher = service.Encrypt(plain);
string decrypted = service.Decrypt(cipher); // == plain

string? nullablePlain = null;
string? nullableCipher = service.EncryptNullable(nullablePlain); // null
string? nullableDecrypted = service.DecryptNullable(nullableCipher); // null

bool atRest = service.EncryptDatabaseEnabled;
```

## Notes
- Decrypt will pass through values that do not start with the CiphertextPrefix.  
- EncryptNullable/DecryptNullable gracefully handle nulls by returning null.  
- EncryptDatabaseEnabled indicates whether server-side encrypt-at-rest is active; use it to guide storage strategies.