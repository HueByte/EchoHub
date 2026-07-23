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


AssignRoleRequest is a lightweight, immutable data container (a positional `record`) that carries the target `Username` and the `Role` to be assigned. It serves as the payload for moderation workflows when granting a [`ServerRole`](../Models/ServerRole.cs.md) to a user, enabling consistent transport of this intent across API boundaries without embedding behavior. As a `record`, it uses value-based equality and can be copied with a `with` expression to create variations.

## Remarks
This symbol acts purely as a data carrier for the moderation flow, separating payload shape from the enforcement logic. It relies on the `Username` and `Role` values to identify the target user and the desired permission, enabling services to validate and enact the change consistently.

## Notes
- Ensure `Username` is a valid existing member; the DTO does not enforce existence.
- The `Role` must be a valid [`ServerRole`](../Models/ServerRole.cs.md) value; rely on server-side validation to handle invalid roles.

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


BanRequest is a simple data carrier used to submit a moderation ban action, optionally including a rationale. Its only member, `Reason`, is nullable and defaults to null, so callers may omit a reason when none is provided.

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


KickRequest is a minimal, immutable data carrier used to convey a moderation kick action. It carries an optional `Reason` explaining why the kick is issued. Callers instantiate a `KickRequest` when initiating a kick, providing a `Reason` if available; if no reason is supplied, the `Reason` property is `null`.

## Remarks
KickRequest being a `record` makes it a value object with structural equality and immutability, which is helpful when routing kick intents through handlers or messaging layers. It encapsulates the kick payload so that higher-level services can work with a single, consistent input type rather than ad-hoc parameters.

## Example
```csharp
var req = new KickRequest("Spamming in chat");
```

## Notes
- `Reason` is nullable; downstream code should handle `null` and decide whether a reason is required.
- Records provide value-based equality; two `KickRequest` instances with the same `Reason` compare equal.

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


MuteRequest is a lightweight data transfer object used to specify the parameters of a mute action in moderation flows. It includes two optional values: `Reason`, a `string?` describing why the mute is issued, and `DurationMinutes`, an `int?` indicating how long the mute should last; both default to `null` if not provided. This allows callers to mute with a default duration or provide additional context for auditing and user experience.

---