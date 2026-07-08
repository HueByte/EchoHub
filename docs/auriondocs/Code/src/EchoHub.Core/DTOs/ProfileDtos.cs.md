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


AvatarUploadResponse is an immutable data transfer object that represents the outcome of an avatar upload operation. It carries a single string, AvatarAscii, containing the ASCII representation of the uploaded avatar, which is returned so clients can render or store the textual avatar without handling binary image data.

## Remarks
Using a record provides value-based equality and built-in immutability, which makes AvatarUploadResponse an ideal DTO for cross-layer communication. The single-property shape keeps serialization stable and makes it easy to deconstruct or pattern-match responses.

## Example
```csharp
// Example usage
var ascii = "  ___  \n / _ \\ \n| | | |\n|_| |_|";
var resp = new AvatarUploadResponse(ascii);
```

## Notes
- The AvatarAscii string may contain newline characters; ensure transport/consumers handle escaping (e.g., JSON) correctly.
- Because AvatarUploadResponse is immutable, any modification requires constructing a new instance instead of mutating the existing one.

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


UpdateProfileRequest is a lightweight, immutable data transfer object used to convey updates to a user's profile. It exposes three optional properties: DisplayName, Bio, and NicknameColor. All properties are nullable strings with default null, enabling callers to include only the fields they want to modify. As a C# record, it benefits from value-based equality and a concise, immutable model that works well across boundaries such as API requests or service layers.

## Remarks
Being a record provides a natural representation of a data-bearing payload and supports ergonomic mutation via 'with' expressions, which allows creating a new payload from an existing one with just the changed fields. This reduces boilerplate when building update payloads and makes intent explicit at the call site.

## Example
```csharp
// Minimal usage
var req = new UpdateProfileRequest(DisplayName: "Alice");

// Derive a patch from an existing payload
var patch = existing with { Bio = "Updated bio" };
```

## Notes
- Nullable properties are intended for partial updates; verify how your endpoint interprets omitted vs explicit null values during serialization.

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


UpdateStatusRequest is a small, immutable data transfer object used to request a user's status update by supplying the new Status and an optional StatusMessage. A developer would construct this record when submitting a status change through profile-update flows rather than mutating user objects directly.

## Remarks
Because UpdateStatusRequest is defined as a record, it benefits from value-based equality and deconstruction, making it ideal for transport across API boundaries or service layers. The Status field enforces a concrete user status, while StatusMessage offers optional, human-readable context. StatusMessage is nullable (string?), so callers may omit it when no extra context is needed. This abstraction isolates the update contract from implementation details, enabling predictable handling by controllers or handlers that consume this payload.

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
    ServerRole Role)
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


A value object that conveys a user's presence information for transfer across system boundaries. It groups the user's identity (Username), optional presentation details (DisplayName, NicknameColor), current presence state (Status, StatusMessage), and their server role (Role) into a single immutable record. This DTO is intended for APIs and UI layers that need a consistent, serializable snapshot of a user's presence.

## Remarks
Being a C# record, UserPresenceDto provides value-based equality and immutability, which makes it ideal for presence snapshots, caches, and payloads that are compared or deduplicated. Nullable fields (DisplayName, NicknameColor, StatusMessage) reflect optional data and allow partial updates without creating placeholder values. The strictly non-null Username, Status, and Role establish a stable identity and state contract for each user across the system.

## Notes
- No runtime validation is performed. Ensure that Status and Role values come from your domain enums/types and that optional strings are handled by the caller.
- If DisplayName is null, you should fallback to Username for user-facing displays.
- NicknameColor is a free-form string; interpret consistently downstream (e.g., hex color codes).

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


Represents the data contract for a user's public profile as it travels across boundaries (for example, API responses or inter-service communication). It aggregates identity (Id, Username), optional display information (DisplayName, Bio, NicknameColor, AvatarAscii), presence/status details (Status, StatusMessage), and authorization context (Role), along with lifecycle timestamps (CreatedAt, LastSeenAt) into a single immutable value object. Use this DTO when you need a stable, client-facing snapshot of a user profile without exposing the full domain model.

## Remarks
Because UserProfileDto is a C# record, it benefits from value-based equality and immutability, which makes it a reliable, side-effect-free carrier across layers and in caches. The nullable fields reflect optional user-provided metadata; consumers should gracefully handle nulls in UI and serialization scenarios. The combination of status information (Status, StatusMessage) with role (Role) and timestamps (CreatedAt, LastSeenAt) provides a complete profile view suitable for rendering presence, permissions, and recency without additional lookups.

## Notes
- Nullable fields (DisplayName, Bio, NicknameColor, AvatarAscii) may be absent or null in the payload; callers should account for missing values in UI rendering.
- DateTimeOffset fields imply timestamps with an explicit offset; ensure consistent time-zone handling (prefer consistent normalization, e.g., UTC) when populating and consuming these values.

---