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


AssignRoleRequest is a lightweight, immutable data transfer object that carries the intent to assign a specific server role to a user. It encapsulates just two pieces of information—the target Username and the desired Role—and is intended to be serialized and sent to moderation or authorization services that perform the actual role assignment.

## Remarks
The record type provides value-based equality and immutability, making it a reliable payload for messaging boundaries between UI, services, and backend handlers. By expressing the action as data rather than behavior, it supports clean separation of concerns and straightforward routing in moderation workflows.

## Notes
- Ensure Username conforms to identity rules at the boundary before processing the request.
- Because this is an immutable record, callers should create a new instance for every distinct request; do not modify an existing instance.

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


BanRequest is a lightweight, immutable data container used when issuing moderation bans. It carries an optional Reason and is designed to be passed as a single object through the moderation pipeline instead of a group of disparate parameters. This structure makes future extension straightforward (e.g., adding additional ban metadata) without changing call sites.

## Remarks
BanRequest acts as a boundary between the transport/presentation layer and the moderation domain. Using a record provides value-based equality and predictable serialization, which aids testing, logging, and caching. The optional Reason supports both silent bans and bans accompanied by rationale, with policy decisions about requiring a reason typically enforced at higher layers.

## Notes
- Reason is nullable; handle nulls gracefully when displaying or persisting data, and apply any policy about requiring a reason at the appropriate layer.

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


KickRequest is a lightweight, immutable payload used when performing a moderation kick. It carries an optional Reason describing why the kick occurred. Callers construct this record when issuing a kick action and attach the reason if one is known; if no reason is provided, Reason remains null. The record shape ensures value-based equality and easy serialization across boundaries, making it a convenient transport object for moderation workflows.

## Remarks
KickRequest isolates the transport of a kick action from its core moderation logic. This abstraction makes it easy to extend later with additional fields (for example, moderatorId, timestamp, or kick ban duration) without changing the public contract. It also supports consistent logging and audit trails by treating the kick reason as optional metadata.

## Notes
- Reason is optional; validate as needed at the API boundary if your scenario requires a non-null reason.
- When serializing, null Reason might be omitted depending on serializer configuration; be explicit if you need to communicate 'no reason'.
- This is a simple DTO; do not conflate it with the domain entity for a kick; use it to transport data.

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


MuteRequest is a compact, immutable data transfer object used to initiate a moderation mute. It carries two optional fields: Reason and DurationMinutes, allowing you to specify a rationale and a duration when issuing a mute; omitting either field leaves that detail to the receiver's policy.

## Remarks
By grouping the fields into a single record, this abstraction reduces API surface area and provides a consistent payload for mute-related actions across the moderation layer. The record semantics also enable value-based equality and straightforward testing and transport.

## Example
```csharp
// Mute for 30 minutes with a reason
var request = new MuteRequest("Spamming in chat", 30);

// Mute without specifying details
var request2 = new MuteRequest();
```

## Notes
- Reason may contain user-provided content; avoid including it in logs or telemetry unless explicitly permitted.
- Because the type is a record with nullable fields, ensure boundary validation and handle nulls gracefully at the call site or in the receiving layer.

---