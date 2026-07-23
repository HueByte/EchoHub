# ServerDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/ServerDtos.cs`

## Contents

- [EncryptionKeyResponse](#encryptionkeyresponse)
- [ServerStatusDto](#serverstatusdto)

---

## EncryptionKeyResponse
> **File:** `src/EchoHub.Core/DTOs/ServerDtos.cs`  
> **Kind:** record

```csharp
public record EncryptionKeyResponse(string Key)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Key` | `string` | — |


`EncryptionKeyResponse` is a concise data-transfer `record` that carries a single string property named `Key`, representing an encryption key. Use this type when an API response or internal boundary needs to convey the key as a structured envelope rather than a raw string, benefiting from the immutability and value-based equality of a `record`.

## Remarks
By modeling the payload as its own type, this symbol helps keep key handling explicit and self-describing across boundaries. It pairs with other server DTOs to form a consistent contract for encryption-related data, and it can evolve to carry extra metadata (expiry, algorithm) without breaking existing clients.

## Notes
- Treat the `Key` as sensitive data; avoid logging it or exposing it in traces. Ensure it is transmitted only over secure channels and managed according to your security policy.

---

## ServerStatusDto
> **File:** `src/EchoHub.Core/DTOs/ServerDtos.cs`  
> **Kind:** record

```csharp
public record ServerStatusDto(
    string Name,
    string? Description,
    int OnlineUsers,
    int TotalChannels,
    string RegistrationMode = "open",
    string Version = "0.0.0")
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Description` | `string?` | — |
| `OnlineUsers` | `int` | — |
| `TotalChannels` | `int` | — |
| `RegistrationMode` | `string` | `"open"` |
| `Version` | `string` | `"0.0.0"` |


Represents a compact, immutable data transfer object that conveys a server's identity and current activity. It exposes the server's `Name`, optional `Description`, the `OnlineUsers` count, the `TotalChannels`, and optional `RegistrationMode` and `Version` (defaulting to `"open"` and `"0.0.0"` when omitted). Use this DTO in API responses or status endpoints to deliver a stable snapshot of server state.

## Remarks
Because it is a `record`, `ServerStatusDto` benefits from value-based equality and deconstruction semantics, making it convenient to compare status payloads in tests or across clients. The trailing `RegistrationMode` and `Version` parameters are optional in construction, allowing callers to supply just the core metrics while still producing a complete payload. This DTO isolates status representation from internal domain entities and keeps the shape stable for clients and tooling.

## Example
```csharp
// Minimal construction: Description omitted (use null)
var status = new ServerStatusDto("EchoHub", null, 12, 3);

// Full construction with explicit values
var statusFull = new ServerStatusDto("EchoHub", "Main gateway", 12, 3, "open", "1.2.0");
```


---