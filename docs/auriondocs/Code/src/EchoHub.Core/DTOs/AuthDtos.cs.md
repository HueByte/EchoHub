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


LoginRequest is an immutable data carrier (record) used to transport a user's credentials from the client to the authentication endpoint. It encapsulates a Username and Password as a single payload, leveraging the value-based semantics and deconstruction features provided by C# records for straightforward comparison and pattern matching in tests and middleware.

## Remarks
Because LoginRequest is a record, it models a value object focused on data rather than behavior. It serves as a boundary for the login workflow, allowing reliable equality checks and straightforward deconstruction in pipelines and tests. Its immutable, positional construction ensures the credentials are captured as provided, without subsequent mutation.

## Notes
- Do not log or reveal the Password value; be mindful of telemetry, debugging, and exception messages.
- Ensure the login payload is transmitted over TLS and avoid including the credentials in logs or transient caches.

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


LoginResponse is an immutable data transfer object that captures the essential results of a successful login. It bundles the access token, refresh token, token expiration, and basic user identifiers into a single, strongly-typed payload, so callers can pass authentication data around without juggling multiple separate values. Its definition as a record enables concise construction and value-based equality, which is convenient for testing and handling in client code.

## Remarks
LoginResponse acts as a boundary between the authentication service and its consumers. Centralizing token data and user identifiers in one object reduces coupling and simplifies API evolution by containing login-related fields in a single type. Because this is a record, it is immutable and compared by value, which helps detect differences in login results across layers and in tests.

## Example
```csharp
// Example 1: all fields provided
var login = new LoginResponse(
    token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    refreshToken: "def50200...",
    expiresAt: DateTimeOffset.UtcNow.AddHours(1),
    username: "alice",
    displayName: "Alice",
    nicknameColor: "#1E90FF"
);

// Example 2: optional fields omitted (null)
var loginWithMinimal = new LoginResponse(
    token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    refreshToken: "def50200...",
    expiresAt: DateTimeOffset.UtcNow.AddHours(1),
    username: "bob",
    displayName: null,
    nicknameColor: null
);
```

## Notes
- The DisplayName and NicknameColor properties are nullable; consumers should handle nulls where appropriate.
- LoginResponse is a record, so it benefits from immutability and value-based equality, and you can easily create modified copies with the with-expression when needed.

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


RefreshRequest is a small data transfer object used to carry a refresh token as part of the token refresh workflow. It is typically sent to an authentication endpoint to exchange a valid refresh token for a new access token.

## Remarks
RefreshRequest uses a record to provide immutability and value-based equality for the refresh token payload. This makes the token a first-class, easily comparable object across layers. It clearly communicates the intent: this type represents the data required to perform a token refresh.

## Example
```csharp
// Example: create a refresh request payload
var request = new RefreshRequest("sample-refresh-token");
```

## Notes
- Avoid logging the RefreshToken; remember that a record's default ToString shows the token, which could leak sensitive information.
- Ensure the token is transmitted securely (over HTTPS) and not exposed in query strings or headers that could be logged or cached.

---

## RegisterRequest
> **File:** `src/EchoHub.Core/DTOs/AuthDtos.cs`  
> **Kind:** record

```csharp
public record RegisterRequest(string Username, string Password, string? DisplayName = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Username` | `string` | — |
| `Password` | `string` | — |
| `DisplayName` | `string?` | `null` |


RegisterRequest models the data required to register a new user: a username, a password, and an optional display name. Use this record as the registration payload (for example, in an API controller) rather than constructing a dictionary or anonymous object, as the record provides immutability and value-based equality that simplify testing and comparison.

## Remarks
RegisterRequest is a C# record, which provides immutable, value-based equality and clean data-oriented semantics suitable for transport across API boundaries. Located in EchoHub.Core/DTOs/AuthDtos.cs, it defines the shape of the registration payload and helps decouple transport details from domain logic. The optional DisplayName enables clients to supply a preferred display name; when omitted, the server can apply a default or prompt the user later.

## Example
```csharp
using System.Text.Json;

var request = new RegisterRequest("alice", "P@ssw0rd!", "Alice Smith");
string json = JsonSerializer.Serialize(request);
```

## Notes
- Logging or exposing the RegisterRequest object can reveal sensitive data (such as Password) via ToString() or serialization; avoid logging the entire payload or redact sensitive fields when necessary.
- Treat Password as sensitive data: ensure it's transmitted over secure channels (e.g., HTTPS) and not stored in logs or client-side storage beyond what is strictly needed for authentication.

---