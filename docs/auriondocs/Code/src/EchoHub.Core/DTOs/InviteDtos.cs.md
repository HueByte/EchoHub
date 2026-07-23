# InviteDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/InviteDtos.cs`

## Contents

- [CreateInviteRequest](#createinviterequest)
- [InviteDto](#invitedto)

---

## CreateInviteRequest
> **File:** `src/EchoHub.Core/DTOs/InviteDtos.cs`  
> **Kind:** record

```csharp
public record CreateInviteRequest(int? MaxUses = null, int? ExpiresInHours = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `MaxUses` | `int?` | `null` |
| `ExpiresInHours` | `int?` | `null` |


Represents the request payload for creating an invite, carrying optional constraints for the invite. The nullable `MaxUses` and `ExpiresInHours` allow callers to omit constraints. As a `record`, it provides value-based equality and immutability, making it a convenient, typed carrier for API calls.

## Remarks
This type centralizes the concept of invite constraints and cleanly separates client request construction from business logic. It interoperates with the invite-creation pathway by encoding optional parameters as nullable properties, allowing the API to apply defaults when a field is null.

## Notes
- Null values indicate 'not specified' and will be treated as absent by the invite-creation endpoint; set only the fields you intend to constrain.

---

## InviteDto
> **File:** `src/EchoHub.Core/DTOs/InviteDtos.cs`  
> **Kind:** record

```csharp
public record InviteDto(
    string Code,
    string CreatedByUsername,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int MaxUses,
    int UseCount)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Code` | `string` | — |
| `CreatedByUsername` | `string` | — |
| `CreatedAt` | `DateTimeOffset` | — |
| `ExpiresAt` | `DateTimeOffset?` | — |
| `MaxUses` | `int` | — |
| `UseCount` | `int` | — |


InviteDto is an immutable data transfer object that carries the metadata for an invitation: the `Code`, the creator's username (`CreatedByUsername`), the creation time (`CreatedAt`), an optional expiration (`ExpiresAt`), and usage counters (`MaxUses` and `UseCount`). It is designed for transporting invitation data across application boundaries without behavior, making it easy to serialize, deserialize, and compare by value.

## Remarks
Because it is defined as a `record`, `InviteDto` benefits from value-based equality and structural immutability, ensuring that two invitations with the same data compare equal and that the payload remains unchanged after construction. The nullable `ExpiresAt` conveys that an invitation might have no expiration; consumers must treat a null as no expiry. The `MaxUses` together with `UseCount` enables the system to enforce limits at the boundary without embedding logic here. This symbol sits at the boundary between persistence, API contracts, and business logic, keeping the shape of invitation data consistent across layers.

---