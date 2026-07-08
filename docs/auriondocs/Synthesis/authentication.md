# Authentication and token-based security

> How tokens are issued, validated, and used by clients to access protected resources across client and server.

*Figure: How Authentication and token-based security works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
sequenceDiagram
Client->>ApiClient_overview: "Login(LoginRequest)"
ApiClient_overview->>AuthController.cs: "POST /auth/login (LoginRequest) [uses AuthDtos.cs]"
AuthController.cs->>JwtTokenService: "GenerateToken(LoginRequest)"
JwtTokenService-->>AuthController.cs: "JWT"
AuthController.cs-->>ApiClient_overview: "200 OK + JWT (AuthDtos.cs)"
ApiClient_overview-->>Client: "Return token; store locally"
Client->>ApiClient_overview: "Request \"GET /channels\""
ApiClient_overview->>AuthController.cs: "GET /channels Authorization: Bearer <JWT>"
AuthController.cs->>JwtTokenService: "ValidateToken(JWT)"
JwtTokenService-->>AuthController.cs: "Validation OK"
AuthController.cs-->>ApiClient_overview: "200 OK [ChannelDto[]] (uses ChatDtos.cs, ChannelDto)"
ApiClient_overview-->>Client: "Return ChannelDto[]"
```

This guide explains how the server issues and validates JWT access tokens, how refresh-token rotation is performed and persisted, and how the client stores and uses tokens when calling protected REST endpoints.

## JwtTokenService.cs
Generates and validates JWT tokens for authentication.

The [JwtTokenService](../Code/src/EchoHub.Server/Auth/JwtTokenService.cs.md) class is the single place that builds signed access tokens and refresh tokens. It exposes two overloads of GenerateAccessToken (one taking a User, one taking a UserProfileDto) that emit JWTs containing standard claims (sub as the user id, username, display_name, role) plus a unique jti; these tokens are signed with a symmetric key read from configuration and expire after a fixed 15-minute lifetime. The service also provides GenerateRefreshToken (64 cryptographically secure random bytes, Base64-encoded) and HashToken (SHA-256) for persisting only the hashed representation of a refresh token. Note the constructor validates the presence of Jwt:Secret, Jwt:Issuer, and Jwt:Audience and will throw InvalidOperationException when those keys are missing.

## AuthController.cs
Exposes login/registration endpoints for clients.

The [AuthController](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) is the HTTP boundary at api/auth and orchestrates registration, login, refresh-token rotation, and logout. It delegates user management to an IUserService, token issuance to the [JwtTokenService](../Code/src/EchoHub.Server/Auth/JwtTokenService.cs.md), and persists refresh-token state via the EchoHubDbContext; refresh tokens are stored only as hashed values and the controller revokes the old refresh token on a successful refresh to implement rotation. The controller is rate-limited under the "auth" policy and returns the token payloads described by the auth DTOs to clients after successful operations.

## ApiClient.cs
Handles token storage and authenticated HTTP calls from the client.

The [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) wraps an HttpClient and centralizes access/refresh token lifecycle on the client side: it exposes Token and RefreshToken properties, keeps expiry information, and fires an OnTokensRefreshed event when tokens change. It implements higher-level methods that drive the auth flows—LoginAsync, LoginWithRefreshTokenAsync, RefreshTokenAsync, LogoutAsync—as well as helpers to ensure an access token is valid (GetValidTokenAsync/EnsureAuthenticated) and to perform authenticated requests (AuthenticatedRequestAsync/AuthenticatedGetAsync). ApiClient relies on the DTO shapes in the shared DTO files when sending and receiving payloads and delegates storage and rotation semantics to the server-side endpoints it calls.

## AuthDtos.cs
`LoginRequest` collaborates directly with `ApiClient` and other members of this topic (8 dependency links).

The [LoginRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) record is an immutable carrier for Username and Password used when the client calls login endpoints; its companion [LoginResponse](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) bundles Token, RefreshToken, ExpiresAt, Username, and optional display fields returned after a successful login. The file also contains the RefreshRequest and RegisterRequest DTOs used by the login/refresh/registration endpoints so that both the client ([ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md)) and server ([AuthController](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md)) agree on payload shapes. The docs emphasize security practices: do not log passwords, transmit credentials only over TLS, and persist only hashed refresh tokens on the server.

## ChatDtos.cs
`ChannelDto` collaborates directly with `ApiClient` and other members of this topic (5 dependency links).

The [ChannelDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) record is a lightweight immutable transport type describing a chat channel (Id, Name, nullable Topic, IsPublic, MessageCount, CreatedAt) and is used by [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) for channel-related operations such as CreateChannelAsync and GetChannelsAsync. Because ChannelDto is a pure data carrier with value-based equality, the client and server use it to exchange channel metadata without embedding domain behavior; callers should account for nullable Topic and the DateTimeOffset semantics when rendering or sorting.

How the pieces fit

The server-side [JwtTokenService](../Code/src/EchoHub.Server/Auth/JwtTokenService.cs.md) is the authority for token format, signing, and lifetimes; [AuthController](../Code/src/EchoHub.Server/Controllers/AuthController.cs.md) uses it to issue access and refresh token pairs and persists only hashed refresh tokens in the database while performing rotation on refresh. The client-side [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) uses the DTO contracts from [AuthDtos](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) and [ChatDtos](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) to call the controller endpoints, store the raw tokens in memory (exposing Token and RefreshToken), and automatically attach or refresh tokens when making authenticated HTTP requests. Together they implement a request flow where the controller delegates token creation to JwtTokenService, persists hashed refresh tokens, and the client manages token usage and rotation by calling the controller's endpoints using the shared DTO shapes.

---
*Covers 5 of 5 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-08 17:06:04 UTC*
