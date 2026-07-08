# CommonDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/CommonDtos.cs`

## Contents

- [ApiResponse](#apiresponse)
- [ApiResponse](#apiresponse-1)
- [ChannelOperationResult](#channeloperationresult)
- [ErrorResponse](#errorresponse)
- [PaginatedResponse](#paginatedresponse)
- [UserOperationResult](#useroperationresult)
- [ChannelError](#channelerror)
- [UserError](#usererror)

---

## ApiResponse
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record ApiResponse(bool Success, string? Message = null, List<string>? Errors = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| [`Message`](../Models/Message.cs.md) | `string?` | `null` |
| `Errors` | `List<string>?` | `null` |


ApiResponse is a lightweight, immutable DTO used to standardize API results. It communicates whether an operation succeeded via the Success flag, and optionally carries a human-readable message and a collection of error details. Callers create an ApiResponse to wrap the outcome of a service or controller action, enabling consistent JSON payloads even when only partial information is available.

## Remarks
As a C# record, ApiResponse provides value-based equality and promotes immutability, helping prevent accidental mutation of response data after construction. The nullable Message and Errors properties let endpoints omit fields when they are not applicable, while still delivering a stable, predictable payload shape for clients. This pattern typically serves as a boundary-crossing envelope between domain logic and presentation, allowing controllers and services to return a single, transport-friendly object rather than ad-hoc tuples or multiple return values.

## Example
```csharp
var ok = new ApiResponse(true);
var validation = new ApiResponse(false, "Validation failed", new List<string> { "Email is invalid", "Password is too short" });
```


---

## ApiResponse
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record ApiResponse<T>(bool Success, string? Message = null, List<string>? Errors = null, T? Data = default)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Success` | `bool` | — |
| [`Message`](../Models/Message.cs.md) | `string?` | `null` |
| `Errors` | `List<string>?` | `null` |
| `Data` | `T?` | `default` |


`ApiResponse<T>` is a generic, immutable wrapper that standardizes how API results are returned. It exposes a boolean Success, an optional Message, an optional collection of Errors, and an optional Data payload of type T. The record nature provides value-based equality and convenient deconstruction, making it ideal for controllers and services to return a consistent contract while conveying both outcome and payload.

## Remarks
Using `ApiResponse<T>` avoids ad-hoc error signaling scattered across the API surface and centralizes the common success/error pattern. It decouples business logic from presentation details, enabling clients to rely on a predictable contract and render messages or error lists consistently.

## Example
```csharp
using System.Collections.Generic;

var ok = new ApiResponse<string>(true, "OK", null, "payload");
var error = new ApiResponse<string>(false, "Validation failed", new List<string> { "Field is required" }, null);
```

## Notes
- Data can be null even when Success is true; check Data before use.
- When Failure occurs, Data is often null; rely on Errors and Message for details.
- Use Errors to convey multiple validation issues; reserve Message for a concise summary.

---

## ChannelOperationResult
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record ChannelOperationResult(ChannelDto? Channel, ChannelError? Error, string? ErrorMessage)
{
    public bool IsSuccess => Error is null;

    public static ChannelOperationResult Success(ChannelDto channel) => new(channel, null, null);
    public static ChannelOperationResult Fail(ChannelError error, string message) => new(null, error, message);
}
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| [`Channel`](../Models/Channel.cs.md) | `ChannelDto?` | — |
| `Error` | `ChannelError?` | — |
| `ErrorMessage` | `string?` | — |


ChannelOperationResult is a lightweight outcome wrapper for channel-related operations, encapsulating either a ChannelDto when successful or an error code and message when failed. Use ChannelOperationResult.Success(...) to return a successful result or ChannelOperationResult.Fail(...) to represent a failure, and always check IsSuccess before accessing Channel.

## Remarks
ChannelOperationResult centralizes the common pattern of returning either a value or error information from channel-related operations, avoiding exceptions for expected failure conditions. The IsSuccess property acts as the discriminant, being true when Error is null. When successful, Channel holds the resulting ChannelDto and Error and ErrorMessage are null; when failed, Channel is null and Error/ErrorMessage describe the problem. This abstraction simplifies consumer code and keeps error handling centralized in a single type.

## Notes
- Direct construction of the record bypasses the intended factory invariants; prefer using Success(...) or Fail(...) to ensure that exactly one of Channel or Error is populated.

---

## ErrorResponse
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record ErrorResponse(string Error, string? Detail = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Error` | `string` | — |
| `Detail` | `string?` | `null` |


ErrorResponse is an immutable error payload represented as a C# record. It carries a required Error message and an optional Detail string, providing a consistent shape for error responses that can be serialized and transmitted across boundaries (for example, API responses or service outputs).

## Remarks
Records provide value-based equality, deconstruction, and concise initialization, which makes ErrorResponse a natural fit for error payloads. The Error property should contain a concise description or an error code suitable for display to users or clients; the optional Detail field can carry additional context (such as a correlation ID or debugging notes) when available. Since Detail can be null, consumers should handle its absence gracefully, and consider sanitizing or omitting internal details before exposing them publicly.

## Notes
- Detail is nullable; treat it as optional context rather than a guarantee.
- Avoid leaking sensitive details in Error or Detail when returning this to clients; log full details server-side instead.
- Serialization behavior may include null properties by default; configure your serializer to ignore nulls if you want to omit Detail when it's not provided.

---

## PaginatedResponse
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record PaginatedResponse<T>(List<T> Items, int Total, int Offset, int Limit)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Items` | `List<T>` | — |
| `Total` | `int` | — |
| `Offset` | `int` | — |
| `Limit` | `int` | — |


`PaginatedResponse<T>` is a generic record that carries a single page of items along with paging information. It standardizes paging results by returning the page's items together with the total item count, the current offset, and the page size (limit) used to fetch that page.

## Remarks
As a record, `PaginatedResponse<T>` provides value-based equality and a stable, immutable wrapper for paging data. Note that the Items property is a `List<T>`—the reference is immutable, but the list's contents can be mutated by callers; if true immutability is required, consider using `IReadOnlyList<T>` or `ImmutableList<T>` instead. Ensure Total represents the total number of items across all pages; if your data source cannot provide a total, surface a best-effort value or 0 and handle it on the client side. This type is intended as a DTO boundary for APIs and data access layers that implement paging.

## Example
```csharp
var page = new PaginatedResponse<string>(
    new List<string> { "alpha", "beta", "gamma" },
    total: 42,
    offset: 0,
    limit: 3);
```

## Notes
- Mutating the Items list after construction can surprise consumers; prefer exposing read-only access or using an immutable collection if you need to guarantee immutability.
- Ensure that Offset and Limit align with your paging strategy (e.g., 0-based offset and a positive limit).
- The generic type T should be serialization-friendly to ensure reliable transport across boundaries.

---

## UserOperationResult
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** record

```csharp
public record UserOperationResult(UserProfileDto? User, UserError? Error, string? ErrorMessage)
{
    public bool IsSuccess => Error is null;

    public static UserOperationResult Success(UserProfileDto user) => new(user, null, null);
    public static UserOperationResult Fail(UserError error, string message) => new(null, error, message);
}
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| [`User`](../Models/User.cs.md) | `UserProfileDto?` | — |
| `Error` | `UserError?` | — |
| `ErrorMessage` | `string?` | — |


Represents the outcome of a user-related operation by carrying either the resulting UserProfileDto payload on success or error information on failure. It exposes IsSuccess to indicate the outcome and provides two convenience factories, Success and Fail, to create well-formed results without throwing.

## Remarks

This type centralizes the contract for operations that obtain or transform user data, separating successful payload delivery from error handling at boundaries such as services or repositories. By modeling outcomes as a single, immutable value, callers can branch deterministically on IsSuccess rather than relying on exceptions or null checks. The presence of a User implies a successful result, while a non-null Error (and its corresponding ErrorMessage) represents a failure; use the provided factories to produce canonical shapes and keep usage intentions clear when propagating results.

## Notes

- IsSuccess is defined as Error == null. While the static factories encourage the common success/failure shapes, the record constructor technically allows other combinations; prefer using Success/Fail for invariant-friendly results.
- Consumers should access result.User only when IsSuccess is true and may read result.Error/ResultMessage on failure; the payload and the error are mutually exclusive in typical usage.


---

## ChannelError
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** enum

```csharp
public enum ChannelError
{
    ValidationFailed,
    AlreadyExists,
    NotFound,
    Forbidden,
    Protected
}
```


ChannelError enumerates the discrete failure modes that can occur when performing operations on channels within EchoHub. Callers use this enum to communicate why a channel operation failed (validation issues, existence checks, or permission constraints) rather than emitting generic exceptions, allowing higher layers to map failures to appropriate user-facing messages or HTTP status codes. Use it in channel-related service methods and API handlers to classify errors like invalid input, attempting to create a channel that already exists, referencing a missing channel, forbidden access, or operations on a protected channel.

## Remarks
By codifying these failure modes, the API can centralize error handling and provide consistent responses across channel operations. Each value communicates a distinct intent: ValidationFailed signals input problems, AlreadyExists a duplicate resource, NotFound a missing channel, Forbidden a permission issue, and Protected that the channel cannot be modified or deleted.

---

## UserError
> **File:** `src/EchoHub.Core/DTOs/CommonDtos.cs`  
> **Kind:** enum

```csharp
public enum UserError
{
    ValidationFailed,
    AlreadyExists,
    NotFound,
    InvalidCredentials,
    Banned
}
```


UserError defines a finite set of failure conditions that can arise during user-related operations (e.g., authentication, registration, or retrieval). It enumerates the specific reasons such operations may fail: ValidationFailed, AlreadyExists, NotFound, InvalidCredentials, and Banned. Use this enum to communicate a precise failure reason to callers and to drive consistent mapping to user-friendly messages and HTTP status codes rather than scattering string literals across the codebase.

## Remarks
By centralizing these error reasons, UserError provides a stable contract between services and the API layer. It enables consistent translation into user-facing messages and response statuses, and it simplifies testing by letting you assert on explicit error values rather than ad-hoc strings.

## Notes
- If new failure scenarios arise, extend the enum and propagate the update through any mapping logic to avoid inconsistent error reporting.
- Avoid leaking internal validation or business rules in the error values; keep the names meaningful to API clients and stable across versions.

---