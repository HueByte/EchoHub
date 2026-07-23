# AuthDtos.cs

> **Source:** `src/EchoHub.Core/DTOs/AuthDtos.cs`

## Contents

- [LoginRequest](#loginrequest)
- [LoginResponse](#loginresponse)
- [RefreshRequest](#refreshrequest)
- [RegisterRequest](#registerrequest)

---

## LoginRequest
> **File:** `src/EchoHub.Core/DTOs/AuthDtos.cs`  
> **Kind:** record

```csharp
public record LoginRequest(string Username, string Password)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Username` | `string` | — |
| `Password` | `string` | — |


Represents the credentials needed to log a user in. This immutable record carries a Username and Password and is intended to be used as a data transfer object when submitting login data to authentication endpoints.

## Remarks

As a positional record, LoginRequest provides value-based equality and deconstruction. It is immutable, with init-only properties, which helps prevent accidental mutation of credential data as it travels across system boundaries. Treat Password as sensitive data: avoid logging or displaying it, and ensure transport security when sending this DTO.

## Example

```csharp
var request = new LoginRequest("alice", "P@ssw0rd!");
```

## Notes

- Password is sensitive data; avoid logging or displaying it; mask when emitted in logs or error messages.
- This is a simple data-transfer object; it contains no business logic.

---

## LoginResponse
> **File:** `src/EchoHub.Core/DTOs/AuthDtos.cs`  
> **Kind:** record

```csharp
public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string? DisplayName,
    string? NicknameColor)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| [`Token`](../../EchoHub.Client/Services/ApiClient.cs.md) | `string` | — |
| `RefreshToken` | `string` | — |
| `ExpiresAt` | `DateTimeOffset` | — |
| `Username` | `string` | — |
| `DisplayName` | `string?` | — |
| `NicknameColor` | `string?` | — |


LoginResponse is a data transfer object that represents the server's response to a successful login. It bundles the authentication tokens (Token and RefreshToken), the token expiration moment (ExpiresAt), and the authenticated user's identity (Username), along with optional personalization fields (DisplayName and NicknameColor). This object is intended for consumption by clients to establish authenticated sessions, attach the access token to requests, refresh tokens when needed, and present user information in the UI.

## Remarks
LoginResponse is an immutable value object (a record) whose identity is defined by its content. It cleanly separates transport concerns from domain logic, acting as a simple contract that different layers can rely on without side effects. The optional DisplayName and NicknameColor fields model user-facing personalization; callers must handle potential nulls when those fields are not provided.

## Example
```csharp
var response = new LoginResponse(
    Token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    RefreshToken: "def123-refresh",
    ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
    Username: "alex",
    DisplayName: "Alex Doe",
    NicknameColor: "#FF6A00"
);
```

## Notes
- DisplayName and NicknameColor may be null if the server omits them.
- Treat this type as data-only; avoid adding behavior such as validation or mutation.
- Token values are sensitive; avoid logging them and consider secure storage/handling in the client.

---

## RefreshRequest
> **File:** `src/EchoHub.Core/DTOs/AuthDtos.cs`  
> **Kind:** record

```csharp
public record RefreshRequest(string RefreshToken)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `RefreshToken` | `string` | — |


RefreshRequest is a small, immutable data transfer object (a C# record) that carries a single value: the RefreshToken. It is used when a client requests a new access token from the authentication service, typically by posting this payload to the refresh endpoint.

## Remarks
By representing the refresh payload as a dedicated type, the API boundary gains a clear, strongly-typed contract that can be validated and logged consistently. The use of a record ensures value-based equality and immutable semantics, which helps prevent accidental mutation during transport or handling and makes it straightforward to pattern-match or deconstruct if needed in higher layers. In the overall authentication flow, this DTO sits alongside other EchoHub authentication DTOs and forms the low-level transport shape for refresh token exchanges.

## Example
```csharp
var request = new RefreshRequest("sample-refresh-token");
```

## Notes
- Do not log or expose the RefreshToken; avoid writing it to logs or UI.
- Ensure the token is transmitted over HTTPS and handled only in the request body, not in URLs.
- Validate that the token is non-empty before sending to the refresh endpoint; handle nulls gracefully.


---

## RegisterRequest
> **File:** `src/EchoHub.Core/DTOs/AuthDtos.cs`  
> **Kind:** record

```csharp
public record RegisterRequest(string Username, string Password, string? DisplayName = null, string? InviteCode = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Username` | `string` | — |
| `Password` | `string` | — |
| `DisplayName` | `string?` | `null` |
| [`InviteCode`](../Models/InviteCode.cs.md) | `string?` | `null` |


RegisterRequest is a compact data-transfer object used to convey the information necessary to register a new user. It requires a Username and Password, and optionally accepts a DisplayName and an InviteCode. Implemented as a C# positional record, it is immutable and uses value-based equality, making it ideal for transport across API boundaries and for straightforward comparisons in tests. This DTO is typically produced by a client during registration and consumed by server-side authentication logic. The DisplayName parameter is nullable with a default of null, allowing clients to omit it; InviteCode is also nullable and used only when the onboarding flow supports invitation codes.

## Remarks
This symbol acts as a stable contract for the registration flow: it encapsulates the required credentials and optional metadata in a single, immutable object. By using a record, equality and deconstruction align with value semantics, making it easy to compare requests and to pass them through layers without mutation. Because DisplayName and InviteCode are optional, validation often happens elsewhere, enabling flexible client behavior while preserving a clear API boundary.

## Example
```csharp
// Typical usage with all fields
var full = new RegisterRequest("jdoe", "P@ssw0rd", "John Doe", "INVITE-42");

// Minimal usage: only required fields
var minimal = new RegisterRequest("jdoe", "P@ssw0rd");
```

## Notes
- Do not log or leak the Password value; treat it as sensitive data and rely on secure transport and proper logging practices.
- Optional fields may be null; server-side validation should enforce any business rules regarding DisplayName or InviteCode as appropriate.

---