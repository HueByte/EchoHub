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


EncryptionKeyResponse is a tiny, immutable data carrier used to convey an encryption key as part of a response payload. Implemented as a C# record with a single Key property, it provides a stable contract for services returning keys and benefits from value-based equality and concise construction. Use EncryptionKeyResponse when you want to make the presence of a Key explicit in a response rather than returning a raw string.

## Remarks
Because it is a record, EncryptionKeyResponse emphasizes a value-like payload rather than behavior. This makes comparisons deterministic and supports effortless copying via with-expressions when you need a slight variation of the key without mutating the original instance. The wrapper enables evolution of the response contract by adding new fields in the future without breaking existing consumers.

## Example
```csharp
var response = new EncryptionKeyResponse("c2VjcmV0LWtleQ==");
Console.WriteLine(response.Key);
```

## Notes
- Treat the Key as sensitive data; avoid logging or exposing it in UI or logs.
- Ensure that the key is transmitted over secure channels and not persisted insecurely on the client side.

---

## ServerStatusDto
> **File:** `src/EchoHub.Core/DTOs/ServerDtos.cs`  
> **Kind:** record

```csharp
public record ServerStatusDto(string Name, string? Description, int OnlineUsers, int TotalChannels)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Description` | `string?` | — |
| `OnlineUsers` | `int` | — |
| `TotalChannels` | `int` | — |


ServerStatusDto is an immutable data transfer object that encapsulates the current status of a server. It carries the server's Name, an optional Description, the number of OnlineUsers, and the TotalChannels. Use it when you need a lightweight, serializable payload to convey server state across boundaries (for example from the service layer to clients or API responses) without exposing domain behavior or navigation properties.

## Remarks
ServerStatusDto acts as a boundary object between the domain and presentation layers. Being a C# record, it provides value-based equality and convenient deconstruction, making it ideal for comparisons and patterns in client code. Its immutable nature helps prevent unintended mutations as data flows from server logic to its consumers, reinforcing a clear separation between read-only status data and domain behavior.

## Notes
- The Description property is nullable; callers should handle the possibility that no description is provided.  
- Properties are init-only due to the record positional syntax; you cannot mutate them after creation.  
- Keep this DTO as a pure data carrier—avoid embedding domain behavior or navigational properties.

---