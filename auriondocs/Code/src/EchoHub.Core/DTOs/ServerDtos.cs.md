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


EncryptionKeyResponse is a minimal, strongly-typed envelope used to return an encryption key from server-side DTOs. It is implemented as a C# `record` with a single property `string Key`, providing value-based equality and convenient deconstruction while keeping the surface area stable for serialization and future extension.

## Remarks
Using a one-property `record` as a DTO provides a stable, strongly-typed surface for returning the key, while enabling easy evolution (e.g., adding metadata like algorithm, expiration, or salt) without breaking client contracts. It also leverages `record` semantics to support value-based equality and clean deconstruction when used in responses.


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


Represents a lightweight, immutable snapshot of a server's status for transport between layers or to clients. It exposes the server's `Name`, optional `Description`, current `OnlineUsers`, total `TotalChannels`, and the `RegistrationMode` (defaulting to `open` when not provided).

## Remarks
Because this is a `record`, it uses value-based equality and immutable properties, making it ideal as a DTO boundary between internal domain models and external consumers. Construct this type from your server state when returning status information to clients, rather than leaking domain entities.

## Notes
- `Description` is nullable (`string?`). Guard against null or provide a fallback when presenting it to callers.
- To derive a modified copy (e.g., update `OnlineUsers`), use the `with` expression since `ServerStatusDto` is immutable.

---