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


AvatarUploadResponse is a tiny, immutable data container that represents the server’s response to an avatar-upload operation. It carries a single payload, AvatarAscii, which holds the ASCII-art representation of the uploaded avatar. Use this type as a typed contract when returning avatar data from a service or API endpoint, rather than returning a raw string scattered through your responses.

## Remarks
This abstracted DTO isolates the avatar representation behind a named contract, making it easier to evolve the API (e.g., by adding metadata) without breaking call sites. The record semantics ensure value-based equality and straightforward deconstruction, which pairs well with serialization and testing.

## Example
```csharp
var resp = new AvatarUploadResponse("ASCII_ART");
Console.WriteLine(resp.AvatarAscii);
```

## Notes
- AvatarAscii may contain newline characters; ensure your JSON/HTTP layer preserves them.
- Keep the payload size reasonable; extremely large ASCII art can inflate responses.
- This type is a pure DTO with no behavior; avoid placing business logic here.

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


UpdateProfileRequest is a data transfer object used when updating a user's profile. All fields are optional, enabling partial updates by supplying only the fields you want to change (DisplayName, Bio, or NicknameColor). This object is typically sent to a profile update endpoint or service, where the provided values are applied while unspecified fields remain unchanged.

## Remarks
By modeling the payload as a record with nullable properties, this abstraction communicates intent clearly: you're patching specific aspects of a profile rather than replacing it wholesale. It decouples API contract from the underlying domain model and reinforces immutability semantics for the request object. The combination of a concise DTO and nullable members makes it straightforward for clients to express partial updates without constructing separate patch types.

## Example
```csharp
// Update only the display name
var request1 = new UpdateProfileRequest(DisplayName: "Nova");

// Update multiple fields
var request2 = new UpdateProfileRequest(DisplayName: "Nova", Bio: "Software engineer", NicknameColor: "#1E90FF");
```

## Notes
- Omitted properties are treated as "no update" by the receiver; a null value may be interpreted differently depending on backend semantics.
- If you need to clear a value, verify the server's rules: null may not clear a field unless explicitly supported; you may need to provide an empty string or use a dedicated API path to clear a value.

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


UpdateStatusRequest is a small, immutable data transfer object used to submit a user's status update. It bundles the new Status and, optionally, an accompanying StatusMessage to be processed by a profile update operation.

## Remarks
Being a C# 9 record, UpdateStatusRequest is immutable and supports value-based equality, which makes it reliable to pass across process boundaries and into tests. The Status is a required field that identifies the new user state via UserStatus, while StatusMessage provides optional context. This DTO participates in the profile update workflow and is typically serialized as part of requests to the profile service.

## Notes
- StatusMessage is nullable; if the receiver accepts no message, null can be sent and should be handled gracefully.
- Because UpdateStatusRequest is a record, you can create modified copies using the with expression, e.g. existing with { Status = newStatus } to preserve other fields.

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


Represents a single snapshot of a user's presence in EchoHub. This record aggregates the user's identity (Username and optional DisplayName), their current presence state (Status and optional StatusMessage), and their server role (Role). It also carries UI-related hints such as NicknameColor and an IsIrc flag indicating whether the presence originated from IRC. The type is a C# record with positional parameters, making it an immutable, value-based data object that is ideal for transport across API boundaries and for equality comparisons of presence data.

## Remarks
Consolidating identity, status, and role into one DTO reduces the number of cross-cutting data transfers required to render a user in a presence list or chat UI. The NicknameColor provides a presentation cue without forcing consumers to derive display styling; the IsIrc flag lets calling code distinguish between sources. As a record, instances compare by their values, enabling straightforward caching, deduplication, and change detection.

## Notes
- Nullable fields (DisplayName, NicknameColor, and StatusMessage) may be null; callers should handle nulls gracefully.
- IsIrc defaults to false; set to true when constructing from IRC-origin data.
- This is a positional-parameter record; properties are init-only and the object is immutable after construction; create a new instance to represent a changed presence.

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


Represents a compact, transport-friendly snapshot of a user's profile used across boundaries (e.g., API responses, UI layers). As a C# record, it provides value-based equality and immutability, ensuring a stable contract when serializing user data. It collects identity (Id, Username), optional display attributes (DisplayName, Bio, NicknameColor, AvatarAscii), current status (Status, StatusMessage), role (Role), and timestamp metadata (CreatedAt, LastSeenAt).

## Remarks
This DTO exists to decouple internal domain models from the data contract exposed to clients. By using a dedicated record, changes to the underlying domain models won't automatically ripple into API payloads. The explicit nullable fields model optional user attributes, and the timestamp fields communicate when the profile was created and last observed; consumers must handle time values robustly across time zones.

## Notes
- Nullable properties (DisplayName, Bio, NicknameColor, AvatarAscii, StatusMessage) may be null; handle accordingly in consumers.
- CreatedAt and LastSeenAt are DateTimeOffset values; when displaying, convert to a user-friendly timezone or use UTC representation as defined by the API contract.

---