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


EncryptionKeyResponse is a tiny, immutable data transfer object that carries a single encryption key via its Key property. Use it whenever a caller must receive an encryption key in a strongly-typed envelope (instead of returning a plain string) to improve clarity and compatibility with serialization and tooling.

## Remarks
By leveraging a C# record, EncryptionKeyResponse benefits from value-based equality, structural deconstruction, and concise construction. It serves as a semantic wrapper around the raw key, making intent explicit in APIs that issue or relay keys, and aligns with other DTOs in the EchoHub.Core DTOs layer.

## Notes
- The Key contains sensitive material; avoid logging or exposing it in request traces. Ensure transport channels are secure (TLS) and that only authorized callers can obtain the key.
- Because it is a simple wrapper, use it when a typed envelope adds value (e.g., API contracts or structured responses) and avoid over-modeling plain, ephemeral keys.


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
    string RegistrationMode = "open")
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Description` | `string?` | — |
| `OnlineUsers` | `int` | — |
| `TotalChannels` | `int` | — |
| `RegistrationMode` | `string` | `"open"` |


ServerStatusDto is an immutable data-transfer object that represents the current status of a server in EchoHub. It exposes the server name, an optional description, the number of online users, the total number of channels, and a registration mode (defaulting to open). As a C# record with a primary constructor, it benefits from value-based equality and convenient deconstruction, making it a natural payload for API responses that describe the server's state.

## Remarks
A record provides value-based equality and immutability for a simple data carrier, which is exactly what a status payload is. The Description field is optional, so consumers must be prepared to handle null. The shape is designed to be serialized to JSON for API responses and easily deconstructed when mapping to other domain models.

## Notes
- Nullable Description means clients must handle nulls.
- RegistrationMode defaults to "open" when not supplied, preserving backward compatibility.
- As a record, two instances with identical property values compare equal (value equality).

---