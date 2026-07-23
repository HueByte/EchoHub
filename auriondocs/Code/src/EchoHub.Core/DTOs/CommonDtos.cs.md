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


ApiResponse is a lightweight data transfer object used to convey the outcome of an operation. It carries a required Success flag and optional Message and Errors to provide feedback and diagnostics to callers.

## Remarks
Used as a common response shape across service boundaries to avoid ad-hoc return types. The primary purpose is to separate control flow (success/failure) from payload, facilitating simple success messaging and error propagation. Be mindful that Errors is a `List<string>`, which remains mutable if the same instance is shared; convert to a read-only collection or copy before returning to external consumers.

## Notes
- The Errors property is a mutable `List<string>`—wrap or copy it if you intend to preserve a fixed snapshot when returning to consumers.
- Message may be null; supply a default user-friendly message or handle nulls in UI/logging.

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


`ApiResponse<T>` is a generic wrapper you return from API methods to convey a successful outcome, an optional human-friendly message, and a payload of type T, along with any per-call errors. Use this pattern when you want a consistent contract for success, messaging, and data across endpoints rather than returning raw data alone.

## Remarks
`ApiResponse<T>` is an immutable value type (a record with a primary constructor) that standardizes how results are communicated. It separates the data payload from status information, allowing clients to inspect Success, Message, and Errors independently from Data. Because Message and Errors are optional, responses can remain concise for successful operations while still providing rich error detail when needed.

## Example
```csharp
using System.Collections.Generic;

// success with data
var result = new ApiResponse<string>(true, "Operation completed", null, "payload");

// error with details
var failure = new ApiResponse<string>(false, "Validation failed", new List<string> { "Email is invalid" }, null);
```

## Notes
- Message and Errors are nullable; always check Success before relying on these fields, and provide defaults if you need non-null output. 
- `ApiResponse<T>` is immutable; to modify it, use a with-expression to create a copy (e.g., var updated = result with { Data = newData };).


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


ChannelOperationResult is a lightweight result wrapper used by channel-creation/lookup operations to return either a ChannelDto on success or an error descriptor on failure. Callers typically inspect IsSuccess and then access Channel or Error/ErrorMessage, using the static factories to produce a well-formed result rather than constructing it directly.

## Remarks
It captures the outcome of channel-oriented operations in a single, immutable value, reducing the need for exception-based control flow. By pairing either a Channel with no error or an Error with a message, it forces consumers to handle both success and failure paths in a uniform way. It complements the ChannelDto and ChannelError types by providing a minimal, self-describing container that can be passed through layers without leaking implementation details.

## Notes
- Prefer the static factories to create instances to preserve the intended invariant that a result carries either a Channel or an error. The public constructor can produce degenerate states if misused.
- The ErrorMessage is optional; provide a descriptive message to aid debugging when using Fail.

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


ErrorResponse is a small, immutable data transfer object used to convey error information from the server to API clients. Implemented as a C# record with two positional properties, Error and Detail, it carries a concise error identifier or message and optional supplemental details. Use it when standardizing error payloads across API endpoints or error-handling middleware that wants to provide a consistent error shape.

## Remarks
Using a record provides value-based equality and immutability, making ErrorResponse a stable payload that is easy to compare in tests and to clone with modifications via with-expressions. The Error field represents a short error code or message, while Detail offers optional, human-friendly context. This type is intended to be reused across API boundaries, ensuring clients receive a uniform error shape.

## Notes
- Avoid leaking sensitive internals in Error; prefer stable, client-friendly codes or messages.
- Detail is nullable; when null, serialization may omit the property depending on serializer settings.
- As a DTO, this record should be produced by a dedicated error-handling path rather than constructed manually in business logic.

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


Represents a paginated result set for a collection of items of type T. It bundles the items for the current page together with paging metadata (Total, Offset, and Limit), enabling consumers to render pages and request subsequent pages without fetching the entire dataset. Use `PaginatedResponse<T>` when an API or service returns a slice of a larger collection and you need to convey both the page content and the overall size.

## Remarks
This generic DTO unifies paging across different endpoints by pairing a page of items with metadata describing the total size of the set and the paging window (Offset and Limit). Consumers can derive the total number of pages and navigate accordingly, without duplicating paging logic.

## Example
```csharp
var page = new PaginatedResponse<int>(
    Items: new List<int> { 1, 2, 3 },
    Total: 10,
    Offset: 0,
    Limit: 3
);
```

## Notes
- The Items property is a `List<T>`, which is mutable. Mutating the list after construction will affect the PaginatedResponse instance. If you require immutability of the collection, consider exposing `ReadOnlyCollection<T>` or `IReadOnlyList<T>` instead of `List<T>`, or wrap the list before returning.
- Because `PaginatedResponse<T>` is a record, the wrapper itself uses value-based equality, but the `List<T>` contained in Items is compared by reference. Two instances with equal contents but different `List<T>` instances will not compare equal.

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


Represents the outcome of a user-related operation: it either carries a UserProfileDto for success or a UserError and an ErrorMessage for failure. Use IsSuccess to branch on the result and create instances via Success(user) for success or Fail(error, message) for failure.

## Remarks
This abstraction uses a record with nullable payload fields to model a simple Result pattern without introducing a separate discriminated union. It provides a single return type across methods that can either yield a user profile or fail with details, enabling concise consumer code that checks IsSuccess first. Because User is nullable when the result is a failure, and because Error and ErrorMessage are null on success, callers should guard access to User unless IsSuccess is true. The helper methods ensure the invariant that a successful result always carries a user while a failure carries an error and message.

## Notes
- Read result.User only after confirming IsSuccess; otherwise the value may be null.
- On failure, User will be null; consult Error and ErrorMessage for details.

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


ChannelError enumerates the discrete failure cases that can arise when managing channels in EchoHub. It provides a finite set of error codes so callers can distinguish invalid input, duplicates, missing resources, permission issues, and protected resources without resorting to free-form strings.

## Remarks
This enum lives in the DTO layer to convey precise failure reasons from service or repository operations to API clients. By centralizing channel-related errors, it enables consistent error handling, mapping to user-friendly responses, and easier client-side interpretation across create, update, and lookup workflows. The member names align with common REST/DTO conventions, reducing ambiguity when serializing and documenting API contracts.

## Notes
- Changing the enum's members or their order can impact clients that serialize/deserialize error codes; treat it as a public contract.
- If you enable numeric JSON serialization for enums, ensure the API contract documents the expected codes to avoid confusion.

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


Represents the set of user-related errors that can occur during authentication, registration, lookup, or other user-identity operations in the EchoHub DTO layer. This enum provides a typed, contract-friendly way to communicate failure modes from server to client, enabling centralized handling and consistent feedback without scattering string literals across the codebase.

Values include:
- ValidationFailed: input data failed validation.
- AlreadyExists: a resource with the given identifier already exists.
- NotFound: the requested user or resource could not be found.
- InvalidCredentials: credentials were invalid during authentication.
- Banned: the user is banned from the system.

## Remarks
By consolidating these common errors into a single enum, this abstraction decouples transport contracts from domain logic and supports uniform error mapping on the client. It simplifies UI messaging, and it allows the server to evolve its error vocabulary without changing method signatures.

## Notes
- Be mindful of how the enum is serialized in API responses (numeric vs string); consider standardizing on string representations to avoid client breakage when new values are added.
- Adding new values is a contract change; document and version the API accordingly, and ensure clients handle unknown values gracefully.
- This enum is a DTO-level error vocabulary; do not encode domain exceptions here.

---