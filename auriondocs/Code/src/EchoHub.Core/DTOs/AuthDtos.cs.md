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


Represents the credentials payload for a login operation as an immutable data transfer object. It carries the two required fields, `Username` and `Password`, and is intended to be sent to the authentication boundary to perform sign-in. Use `LoginRequest` when you need to pass user credentials through service boundaries in a strongly-typed, single payload rather than as separate arguments.

## Remarks
Because `LoginRequest` is a `record`, it provides value-based equality and immutability, which makes it a natural data carrier across application layers. This abstraction helps decouple transport concerns from domain logic by centralizing credentials into a single, typed payload.

## Notes
- Do not log or serialize the `Password` value; treat `LoginRequest` as sensitive data and ensure transport uses TLS.

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


`LoginResponse` represents the result of a login attempt, carrying the [`Token`](../../EchoHub.Client/Services/ApiClient.cs.md), `RefreshToken`, `ExpiresAt`, and user identity data like `Username`, with optional `DisplayName` and `NicknameColor` for UI personalization. As a `record`, it is immutable and uses value-based equality, making it a convenient, transportable payload for authentication flows.

## Remarks
Immutability and value-based equality make `LoginResponse` easy to compare, cache, and pattern-match in authentication workflows. It groups all login-related data in one cohesive container, reducing the risk of mismatched fields across layers. The optional `DisplayName` and `NicknameColor` allow UI layers to present user-friendly details without forcing these values for every login.

## Example
```csharp
var response = new LoginResponse(
    Token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    RefreshToken: "defghijklmnopqrstuvwxyz",
    ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
    Username: "alice",
    DisplayName: "Alice",
    NicknameColor: "#1E90FF"
);
```

## Notes
- Token and RefreshToken are sensitive; avoid logging them or exposing them in UI or analytics outputs. Treat these values as secrets and secure any transport or storage paths that handle them.

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


An immutable data container representing the payload of a token refresh request. It exposes a single property, `RefreshToken`, which the authentication workflow uses to obtain new access tokens.

## Remarks
Because this is a `record` with a single value, it provides value-based equality and straightforward deconstruction, making it ideal as a data-transfer object (DTO) across API boundaries. It decouples transport concerns from token-issuance logic, enabling the controller to receive and forward the refresh token without embedding behavior.

## Notes
- `RefreshToken` is sensitive data; avoid logging it or exposing it in error payloads. 
- This type is a plain DTO with no validation or side effects; validation should occur in the service layer.

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


RegisterRequest is a data-transfer object that captures the input for a user registration operation. It encapsulates the required `Username` and `Password` and includes optional `DisplayName` and [`InviteCode`](../Models/InviteCode.cs.md) so callers can supply additional metadata in a single payload to the authentication endpoint.

## Remarks
As a simple DTO, `RegisterRequest` acts as a stable contract between the public API surface and the authentication logic. It isolates the registration input structure from internal domain models, enabling independent evolution and simpler testing while the underlying registration workflow evolves.

## Notes
- Do not log or serialize the `Password` field in logs or telemetry; treat it as sensitive data and rely on transport security.
- The optional fields `DisplayName` and [`InviteCode`](../Models/InviteCode.cs.md) may be `null`; downstream code should handle nulls gracefully and only include them when provided.

---