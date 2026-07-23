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


Represents a standard API outcome as a `record` with a `bool` `Success`, an optional `string?` [`Message`](../Models/Message.cs.md), and an optional `List<string>?` `Errors`. Use `ApiResponse` to package the result of API operations or service methods into a single, strongly-typed object for consistent client consumption instead of scattering boolean flags and messages across code.

## Remarks
By centralizing outcome data in `ApiResponse`, callers can handle success/failure logic in a uniform way and avoid ad-hoc boolean checks scattered through the code. The `Errors` collection is intended for granular, field-level validation messages that the client can display; the [`Message`](../Models/Message.cs.md) offers a concise summary, while `Success` drives flow control.

## Example

```csharp
var success = new ApiResponse(true);
var failure = new ApiResponse(false, "Validation failed", new List<string> { "Name is required", "Email is invalid" });
```

## Notes
- The `Errors` property is a mutable `List<string>`; external mutation is possible. If you need true immutability, consider using `IReadOnlyList<string>` or an immutable collection.
- When `Success` is true, you may omit [`Message`](../Models/Message.cs.md) and `Errors` or set them as appropriate; when `Success` is false, provide a meaningful [`Message`](../Models/Message.cs.md) and optionally populate `Errors` to detail issues.

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


A generic wrapper for operation results that standardizes API responses. It indicates success with `Success` and carries an optional [`Message`](../Models/Message.cs.md), a `List<string>` named `Errors` for validation or processing issues, and an optional `Data` payload of type `T`.

## Remarks

This abstraction decouples the shape of a successful response from the actual data, enabling consistent error handling and client-side parsing across services. By returning `ApiResponse<T>` from operations, you centralize how success, messages, and validation details are conveyed, which simplifies cross-cutting concerns like localization and error translation.

## Example

```csharp
// Successful response with data
var success = new ApiResponse<string>(true, "Operation completed", null, "payload-data");

// Failed response with errors
var failure = new ApiResponse<string>(false, "Validation failed", new List<string> { "Name is required", "Email is invalid" }, null);
```

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


ChannelOperationResult is an immutable wrapper that conveys the outcome of a channel-related operation. It either carries a [`ChannelDto`](ChatDtos.cs.md) when the operation succeeds or a `ChannelError` with an `ErrorMessage` when it fails; the static helpers `Success` and `Fail` make the intent explicit when constructing results.

## Remarks
ChannelOperationResult uses a C# `record` to express a simple, value-like outcome. It centralizes success/failure information for channel-oriented operations, enabling uniform error handling and reducing scattered null-checks. Consumers should inspect `IsSuccess` before accessing [`Channel`](../Models/Channel.cs.md); when `IsSuccess` is true, [`Channel`](../Models/Channel.cs.md) is non-null, and when false, `Error` and `ErrorMessage` describe the failure.

## Notes
- Prefer constructing via `ChannelOperationResult.Success(...)` or `ChannelOperationResult.Fail(...)` rather than the primary constructor to preserve the invariant that a successful result has a non-null [`Channel`](../Models/Channel.cs.md) and a failed result has non-null `Error`.

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


Encapsulates a standardized error payload with a mandatory `Error` code and an optional `Detail` string for extra context. As a `record`, it is immutable by design and supports value-based equality and deconstruction, which makes it ideal for returning a single, comparable error object from APIs or services. Use this type to produce consistent, serializable error information across the system.

## Remarks
Centralizes the error payload shape to ensure all error responses share a single contract. The optional `Detail` field provides human-friendly context without breaking clients that only inspect the `Error` code. Because it is a `record`, it naturally supports comparisons and pattern matching when handling error responses.

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


Represents a paged result set for a collection of items of type `T`. It bundles the current page of data (`Items`) with paging metadata: the total item count (`Total`), the starting offset (`Offset`), and the page size limit (`Limit`). This shape is used by APIs that support paging to convey both the data and how to fetch additional pages; the use of a `record` provides value-based equality and immutability for API responses.

## Remarks
Using a `record` for `PaginatedResponse<T>` gives value-based equality and an immutable data shape, which makes it natural for transporting paging results across boundaries. It centralizes both the data (`Items`) and its paging metadata (`Total`, `Offset`, `Limit`) in a single coherent DTO, reducing the risk of mismatch between data and paging state when consumed by clients or other services.

## Notes
- The `Items` collection is a `List<T>`, which is mutable. If you require immutability guarantees, wrap it in a read-only collection or clone the list before exposure.

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


An immutable result wrapper for user-related operations. It encapsulates either a [`UserProfileDto`](ProfileDtos.cs.md) payload via [`User`](../Models/User.cs.md) on success, or a `UserError` and a diagnostic `ErrorMessage` on failure. Use the static factories `Success` and `Fail` to construct consistent results, and check `IsSuccess` to decide how to proceed.

## Remarks
By encapsulating both success payload and failure details into a single value, this symbol standardizes how user-operation results are communicated. Callers check `IsSuccess` and then access either the [`User`](../Models/User.cs.md) payload or the `Error`/`ErrorMessage` to react. Because it is a `record`, equality is based on its contents, which helps tests and caching rely on value semantics.

## Notes
- Directly constructing with a mismatched state (for example, a non-null `Error` but a null or missing `ErrorMessage`) can create inconsistent results; prefer the provided factories to enforce the invariant that success results include a [`User`](../Models/User.cs.md) and no error, while failures include an `Error` and an `ErrorMessage`.

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


ChannelError is an enum that enumerates the standard error conditions that may arise when working with channels in the `EchoHub` domain. It provides a typed set of failure reasons—`ValidationFailed`, `AlreadyExists`, `NotFound`, `Forbidden`, and `Protected`—to be returned by channel-related operations, enabling callers to branch on the specific cause and handle it uniformly rather than parsing strings.

## Remarks

`ChannelError` centralizes the failure kinds that can occur during channel-related operations and is intended to be carried by DTOs that report operation results. It enables type-safe error handling, allowing callers to pattern-match on the exact failure (`ValidationFailed`, `AlreadyExists`, `NotFound`, `Forbidden`, `Protected`) and map them to appropriate responses without parsing human-generated messages. This separation of error kind from presentation keeps the API consistent as channel semantics evolve.

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


The `UserError` enum defines the canonical set of failure conditions related to user accounts that may be surfaced by operations in the core DTO layer. Members include `ValidationFailed`, `AlreadyExists`, `NotFound`, `InvalidCredentials`, and `Banned`, each representing a distinct error scenario that downstream code can pattern-match to drive error responses and user messaging.

## Remarks
This enum centralizes common user-domain errors so that authentication, registration, and profile-management flows can share a consistent error-handling strategy. By codifying these cases in a single type, callers can translate domain failures into uniform API responses and UI messages without depending on implementation details.

## Notes
- When mapping these errors to user-facing messages, avoid exposing sensitive internal details and rely on generic messaging driven by the enum value.

---