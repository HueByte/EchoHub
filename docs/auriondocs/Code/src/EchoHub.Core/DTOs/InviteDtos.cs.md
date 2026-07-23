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


This record serves as the payload for creating an invitation. It carries optional constraints that govern the invite: MaxUses limits how many times the invite can be redeemed, and ExpiresInHours determines how long the invite remains valid (in hours). When constructing the request, omit values you don’t want to constrain; null properties indicate the server should apply its defaults.

## Remarks
Because CreateInviteRequest is a C# record, it provides value-based equality and immutable semantics, making it a reliable DTO for API calls and caching. The nullable properties express optional constraints without introducing separate flags, keeping the surface area small and expressive.

## Example
```csharp
var request = new CreateInviteRequest(MaxUses: 5, ExpiresInHours: 24);
```

## Notes
- Null on a property means no constraint; the API defaults apply.
- Many serializers omit null fields; if the API requires an explicit indicator for "no constraint," ensure your serializer preserves the field or you configure it accordingly.
- If you need to convey zero constraints explicitly, pass 0 (not null) for the respective property; null is not the same as zero.


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


InviteDto is a small, transport-oriented representation of an invitation. It encapsulates the invitation code, the creator's username, the moment of creation, an optional expiry, and simple usage counters, making it suitable for API responses and inter-layer data transfers without revealing domain internals.

## Remarks
As a record, InviteDto is immutable and uses value-based equality, which makes caching and comparisons straightforward. It decouples transport concerns from domain logic by presenting only the data clients need. The fields map directly to invitation semantics: Code is the token, CreatedByUsername and CreatedAt capture provenance, ExpiresAt denotes expiry (nullable means no expiry), and MaxUses/UseCount express the usage limits and current consumption.

## Example
```csharp
var invite = new InviteDto(
    Code: "WELCOME-ABC123",
    CreatedByUsername: "admin",
    CreatedAt: DateTimeOffset.UtcNow,
    ExpiresAt: DateTimeOffset.UtcNow.AddDays(7),
    MaxUses: 5,
    UseCount: 0
);
```

## Notes
- Null ExpiresAt means the invitation does not expire; ensure your validation logic accounts for that.
- InviteDto is immutable; to reflect state changes (e.g., after a use), construct a new instance rather than mutating the existing one.
- Use UTC times for CreatedAt/ExpiresAt to avoid timezone ambiguity.

---