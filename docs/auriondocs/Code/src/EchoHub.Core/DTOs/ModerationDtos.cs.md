# ModerationDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/ModerationDtos.cs`

## Contents

- [AssignRoleRequest](#assignrolerequest)
- [BanRequest](#banrequest)
- [KickRequest](#kickrequest)
- [MuteRequest](#muterequest)

---

## AssignRoleRequest
> **File:** `src/EchoHub.Core/DTOs/ModerationDtos.cs`  
> **Kind:** record

```csharp
public record AssignRoleRequest(string Username, ServerRole Role)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Username` | `string` | — |
| `Role` | [`ServerRole`](../Models/ServerRole.cs.md) | — |


Represents a request to assign a ServerRole to a user, identified by Username. This immutable data container is used in moderation workflows when submitting a role-change action to a service or API.

## Remarks

Because this symbol is a C# record, it provides value-based equality and immutability, making it ideal as a transport DTO across boundaries. It decouples the API contract from domain logic by modeling the action as a simple payload containing only the target Username and the Role to assign. This promotes clean separation of concerns in moderation features that manage user permissions.

## Notes

- Ensure Username is non-empty; the record itself does not enforce validation. Callers should validate input before transmission.
- Validate that the provided ServerRole is permitted and that the receiving side enforces authorization checks.
- As a positional-record, the properties are Username and Role; maintain constructor parameter order when deserializing payloads to avoid binding issues.

---

## BanRequest
> **File:** `src/EchoHub.Core/DTOs/ModerationDtos.cs`  
> **Kind:** record

```csharp
public record BanRequest(string? Reason = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Reason` | `string?` | `null` |


BanRequest is a minimal, immutable data carrier used to convey a moderation action to ban a user, optionally including a reason. Implemented as a C# record with a single nullable string property Reason, it supports concise construction and value-based equality. Use BanRequest when you need to pass ban details across boundaries (services, APIs, or events) in a typed, self-describing way rather than using loose dictionaries or scattered parameters.

## Remarks
Because BanRequest is a record, it benefits from value-based equality, deconstruction, and built-in immutability, which improves reliability in tests and when broadcasting ban decisions across system boundaries. The single-property shape keeps the contract small, while still allowing future extension (e.g., duration, scope) without changing callers.

## Example
```csharp
// Ban with a reason
var withReason = new BanRequest("Spamming in chat");

// Ban without a reason
var withoutReason = new BanRequest();
```

## Notes
- Be mindful that Reason is nullable; some serializers may omit null values by default. If your API contract requires the field to be present, configure the serializer to include nulls or provide a non-null default.

---

## KickRequest
> **File:** `src/EchoHub.Core/DTOs/ModerationDtos.cs`  
> **Kind:** record

```csharp
public record KickRequest(string? Reason = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Reason` | `string?` | `null` |


KickRequest is a small, immutable data carrier used to represent a moderation kick; it carries an optional reason to provide context for the action. Use it when issuing a kick so downstream handlers, logs, and UIs can display the justification if present.

## Remarks
As a record, KickRequest affords value-based equality and concise construction, enabling straightforward comparisons, serialization, and testing. It separates the payload of a kick from the execution logic, making it easier to transport across boundaries (e.g., messaging or API layers). The optional Reason supports auditing and user-facing messaging, while a null value signals that no justification was provided.

## Example
```csharp
var reqWithReason = new KickRequest("Spamming in chat");
var reqWithoutReason = new KickRequest();
```

## Notes
- Reason is nullable; callers should handle a possible null value. To create a modified copy with a different reason, use the with expression (e.g., `var updated = req with { Reason = "New reason" };`).

---

## MuteRequest
> **File:** `src/EchoHub.Core/DTOs/ModerationDtos.cs`  
> **Kind:** record

```csharp
public record MuteRequest(string? Reason = null, int? DurationMinutes = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Reason` | `string?` | `null` |
| `DurationMinutes` | `int?` | `null` |


MuteRequest is a lightweight data carrier that encapsulates a moderation action to mute a user. It exposes two optional fields: Reason, which provides context for the mute, and DurationMinutes, which specifies how long the mute should last when a duration is given. Because both fields are nullable, callers can omit one or both values to express "no specific reason" or "no explicit duration". This DTO is designed to be consumed by moderation services or APIs to apply a mute without enforcing defaults at the data layer.

## Remarks
As a record, MuteRequest benefits from value-based equality and immutability, which makes it reliable for comparing requests in tests and when passing around as a data transfer object. It serves as a clear contract for conveying a mute intent across layers of the moderation subsystem, decoupling the entry point from the enforcement logic.

## Notes
- Null fields indicate that the caller did not specify a value; downstream components should interpret null as "unspecified".
- Validation (e.g., non-negative DurationMinutes) should occur at the business-logic layer; the DTO itself does not enforce these constraints.
- Be aware of serializer behavior for null-valued properties; ensure the API accepts either explicit nulls or omitted fields as appropriate for your transport.


---