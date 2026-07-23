# API client authentication

> How the client authenticates with the server, including login, token refresh, and token usage across API calls.

A short, focused orientation to how the client authenticates to the server and then uses those credentials when making API calls. The three files described below show a single HTTP façade that owns token state and many API operations ([ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md)), plus the small immutable DTOs that carry credentials and message/attachment metadata between the client and server. Read these together to understand the runtime flow: sign in -> store tokens -> refresh when needed -> attach tokens to requests; and how message attachments are represented when uploaded or downloaded.

## ApiClient.cs
Performs login, token refresh, and authenticated API calls.

The [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) is a sealed, disposable HTTP façade that centralizes authentication state (access token, refresh token, and expiration) and exposes the concrete operations the UI or other client code calls. The doc lists properties and members such as `BaseUrl`, `Token`, `RefreshToken`, `SetTokens`, and lifecycle helpers like `Dispose`, plus auth-focused methods `LoginAsync`, `LoginWithRefreshTokenAsync`, `RefreshTokenAsync`, and `GetValidTokenAsync` — these are the explicit entry points for establishing and renewing credentials. For making requests it provides `AuthenticatedRequestAsync` and `AuthenticatedGetAsync` (and `EnsureAuthenticated` / `EnsureSuccessAsync`) to attach the current token and validate responses; higher-level API operations are implemented as methods like `SendMessageWithAttachmentsAsync`, `DownloadFileToTempAsync`, `UploadAvatarAsync`, and many channel/user management calls (e.g., `CreateChannelAsync`, `BanUserAsync`, `AssignRoleAsync`). Within this topic the `ApiClient` depends on the DTO types defined in the other files to marshal request and response payloads (see relationships: depends on ChatDtos.cs, AuthDtos.cs) and therefore hands off typed payloads like `LoginRequest`/`LoginResponse` and `AttachmentDto` when calling the server.

## AuthDtos.cs
Defines the login response DTO used by the API client.

This file contains small immutable records that model the authentication payloads the client sends and receives. Notably, `LoginRequest(string Username, string Password)` packages credentials for `LoginAsync` calls, and `LoginResponse(string Token, string RefreshToken, DateTimeOffset ExpiresAt, string Username, string? DisplayName, string? NicknameColor)` is the typed response carrying the `Token`, `RefreshToken`, and `ExpiresAt` values that the [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) stores and uses to authorize subsequent requests. There are also `RefreshRequest` and `RegisterRequest` records for refresh and registration flows; these DTOs are value objects (records) intended for transport only and are the direct inputs/outputs used by ApiClient methods like `LoginAsync`, `RefreshTokenAsync`, and `LoginWithRefreshTokenAsync` as the source of truth for token state.

## ChatDtos.cs
`AttachmentDto` collaborates directly with `ApiClient` and other members of this topic (8 dependency links).

`ChatDtos.cs` defines the message- and channel-related transport shapes that the [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) consumes and returns. The `AttachmentDto(AttachmentKind Kind, string Url, string FileName, long FileSize, string? AsciiPreview = null)` record encapsulates an attachment's metadata: a retrieval `Url`, `FileName`, `FileSize`, and optional `AsciiPreview`. The file also contains `SendMessageRequest`, `SendUrlRequest`, `ChannelDto`, `ChannelMetaDto`, `MessageDto`, `UserDto`, and `ChannelCryptoDto` among others; these records are the concrete payloads `ApiClient` methods accept and return for operations such as `SendMessageWithAttachmentsAsync`, `SendUrlAsync`, `GetChannelMetaAsync`, and `DownloadFileToTempAsync`. In practice the `AttachmentDto.Url` is the link the client will follow (via `DownloadFileToTempAsync`) to retrieve an attachment and the structured send requests are the bodies used by the ApiClient when posting messages or creating channels.

How the pieces fit

The runtime collaboration is straightforward: the [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) is the orchestrator that holds token state emitted by the auth DTOs (e.g., [LoginResponse](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md)). Callers invoke `LoginAsync`/`LoginWithRefreshTokenAsync` to obtain or restore that state, `GetValidTokenAsync`/`RefreshTokenAsync` to keep it current, and the client then uses `AuthenticatedRequestAsync`/`AuthenticatedGetAsync` to attach the access token to calls. For message and file operations the ApiClient sends and receives the chat records from [ChatDtos.cs](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) — for example, `AttachmentDto` conveys the `Url` and metadata that `DownloadFileToTempAsync` and `SendMessageWithAttachmentsAsync` operate on — so DTOs remain passive carriers while ApiClient implements the network and auth behavior that uses them.

---
*Covers 3 of 3 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:30:19 UTC*
