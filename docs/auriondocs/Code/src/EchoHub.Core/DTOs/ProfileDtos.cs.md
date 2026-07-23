# ProfileDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`

## Contents

- [AvatarUploadResponse](#avataruploadresponse)
- [UpdateProfileRequest](#updateprofilerequest)
- [UpdateStatusRequest](#updatestatusrequest)
- [UserPresenceDto](#userpresencedto)
- [UserProfileDto](#userprofiledto)

---

## AvatarUploadResponse
> **File:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`  
> **Kind:** record

```csharp
public record AvatarUploadResponse(string AvatarAscii)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `AvatarAscii` | `string` | — |


AvatarUploadResponse is a lightweight data container that carries the ASCII representation of a user-uploaded avatar. Its sole payload is the `AvatarAscii` string, which downstream clients can render to display the avatar in text form after an upload.

---

## UpdateProfileRequest
> **File:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`  
> **Kind:** record

```csharp
public record UpdateProfileRequest(
    string? DisplayName = null,
    string? Bio = null,
    string? NicknameColor = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `DisplayName` | `string?` | `null` |
| `Bio` | `string?` | `null` |
| `NicknameColor` | `string?` | `null` |


This `UpdateProfileRequest` is a `record` that carries a partial update payload for a user's profile. By supplying only non-null properties (e.g. `DisplayName`, `Bio`, or `NicknameColor`), callers express which fields should be updated; fields left as `null` indicate no change for that field.

## Remarks

Using a `record` provides value-based equality and inherent immutability, which makes it ideal for data-carrying DTOs. The ability to set properties to `null` gives a clean contract for partial updates; consumers should treat nulls as 'do not modify' for that field and pass through only the intended changes to the update operation.

## Example

```csharp
var request = new UpdateProfileRequest(DisplayName: "Nova", NicknameColor: "#FFAA00");
```

## Notes

- Ensure the update handler interprets nulls as "no change" to avoid overwriting existing values.


---

## UpdateStatusRequest
> **File:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`  
> **Kind:** record

```csharp
public record UpdateStatusRequest(
    UserStatus Status,
    string? StatusMessage = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Status` | [`UserStatus`](../Models/UserStatus.cs.md) | — |
| `StatusMessage` | `string?` | `null` |


This `UpdateStatusRequest` record encapsulates the payload required to update a user's profile status. It carries the new [`UserStatus`](../Models/UserStatus.cs.md) and an optional `StatusMessage`, and is intended to be used when issuing a status update to APIs or command handlers where a consistent update payload is expected.

## Remarks

By modeling the input as a dedicated value object, this abstraction centralizes validation and transport concerns at the boundaries between the domain and application layers, ensuring a stable contract for status updates. It also isolates update-related concerns from the rest of the profile payload, making it easier to evolve serialization, auditing, or routing rules without touching domain entities.

## Notes

- The `StatusMessage` property is nullable. Callers must handle the possibility of a missing message when consuming this payload.

---

## UserPresenceDto
> **File:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`  
> **Kind:** record

```csharp
public record UserPresenceDto(
    string Username,
    string? DisplayName,
    string? NicknameColor,
    UserStatus Status,
    string? StatusMessage,
    ServerRole Role,
    bool IsIrc = false)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Username` | `string` | — |
| `DisplayName` | `string?` | — |
| `NicknameColor` | `string?` | — |
| `Status` | [`UserStatus`](../Models/UserStatus.cs.md) | — |
| `StatusMessage` | `string?` | — |
| `Role` | [`ServerRole`](../Models/ServerRole.cs.md) | — |
| `IsIrc` | `bool` | `false` |


Represents the presence-related data for a user in profile contexts, bundling the `Username`, optional `DisplayName`, optional `NicknameColor`, current `Status`, optional `StatusMessage`, `Role`, and the `IsIrc` flag into a single immutable DTO (with `IsIrc` defaulting to `false`). It is intended to be created and transported as a coherent unit when rendering user cards or updating presence in the UI or API responses, rather than scattering these fields across multiple structures.

## Remarks
Acts as a stable boundary for presence data used by profile-related UI and API surfaces, consolidating identity, status, and role information into one payload. The [`UserStatus`](../Models/UserStatus.cs.md) and [`ServerRole`](../Models/ServerRole.cs.md) collaborators encode the allowed presence states and roles, while `NicknameColor` provides a UI cue without forcing a separate domain type. Being a `record`, it relies on value equality to simplify change detection and caching as presence updates propagate.

---

## UserProfileDto
> **File:** `src/EchoHub.Core/DTOs/ProfileDtos.cs`  
> **Kind:** record

```csharp
public record UserProfileDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? NicknameColor,
    string? AvatarAscii,
    UserStatus Status,
    string? StatusMessage,
    ServerRole Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Id` | `Guid` | — |
| `Username` | `string` | — |
| `DisplayName` | `string?` | — |
| `Bio` | `string?` | — |
| `NicknameColor` | `string?` | — |
| `AvatarAscii` | `string?` | — |
| `Status` | [`UserStatus`](../Models/UserStatus.cs.md) | — |
| `StatusMessage` | `string?` | — |
| `Role` | [`ServerRole`](../Models/ServerRole.cs.md) | — |
| `CreatedAt` | `DateTimeOffset` | — |
| `LastSeenAt` | `DateTimeOffset` | — |


UserProfileDto is an immutable data transfer object that represents a snapshot of a user's profile for API responses and inter-layer communication. Implemented as a `record`, it carries a stable payload including the user's identity (`Id` of type `Guid`, `Username`), optional display details (`DisplayName`, `Bio`, `NicknameColor`, `AvatarAscii`), presence (`Status` of type [`UserStatus`](../Models/UserStatus.cs.md), `StatusMessage`), role (`Role` of type [`ServerRole`](../Models/ServerRole.cs.md)), and timestamps (`CreatedAt`, `LastSeenAt` of type `DateTimeOffset`).

## Remarks
By modelling the payload as a `record`, `UserProfileDto` benefits from value-based equality and straightforward serialization for API clients. It serves as a transport contract that decouples external API surfaces from the internal domain model, allowing optional fields to convey partial profile information without mutating server state.

---