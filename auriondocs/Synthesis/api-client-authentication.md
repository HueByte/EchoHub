# API client and authentication

> How the EchoHub client authenticates with the server, handles tokens, and defines authentication DTOs.

This guide explains how the EchoHub client performs HTTP operations and manages authentication tokens, and it documents the DTOs the client uses when talking to the server. It focuses on the client-side [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) as the central point for login/refresh/logout and common API operations, and the small set of DTOs and client callback interface the ApiClient consumes and produces. Read this when you need to understand which types carry credentials and tokens, how attachments and avatar uploads are represented, and where server-initiated events are delivered on the client.

## ApiClient.cs
Implements token management and API calls to the EchoHub server.

The [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) class is a high-level HTTP client that centralizes authentication lifecycle (LoginAsync, LoginWithRefreshTokenAsync, RefreshTokenAsync, LogoutAsync, SetTokens) and exposes token state via properties like Token, RefreshToken, and ExpiresAt. It provides helper methods for authenticated requests (AuthenticatedRequestAsync, AuthenticatedGetAsync, EnsureAuthenticated, GetValidTokenAsync) and common server operations surfaced to callers: channel and message management (CreateChannelAsync, DeleteChannelAsync, SendMessageWithAttachmentsAsync, DeleteMessageAsync, RekeyChannelAsync, NukeChannelAsync), moderation actions (AssignRoleAsync, BanUserAsync, KickUserAsync, MuteUserAsync, UnbanUserAsync, UnmuteUserAsync), profile and upload flows (UploadAvatarAsync, DownloadFileToTempAsync, UpdateProfileAsync, ExportMyDataAsync, DeleteMyAccountAsync), and utilities for handling file content types (GetContentType). The ApiClient implements IDisposable (Dispose) and contains response handling helpers (EnsureSuccessAsync) so callers get a single, managed surface for HTTP/authorization concerns. According to its relationships it depends on the DTO definitions in [AuthDtos](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md), [ChatDtos](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md), [ModerationDtos](../Code/src/EchoHub.Core/DTOs/ModerationDtos.cs.md), and [ProfileDtos](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md); in practice the ApiClient serializes and deserializes instances of those DTOs when calling corresponding endpoints and when returning structured results to its callers.

## AuthDtos.cs
Defines login request data structure used to authenticate.

The [AuthDtos](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) file defines the transport types used by the authentication endpoints: the immutable positional [LoginRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) record carrying Username and Password, the [LoginResponse](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) record that bundles Token, RefreshToken, ExpiresAt and basic user identity fields, plus a [RefreshRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) and [RegisterRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md). These DTOs are pure data containers (no business logic) intended to be serialized over HTTP; the documentation calls out that Password is sensitive and that LoginResponse is what clients consume to establish an authenticated session. The ApiClient uses these DTOs when performing login and token-refresh flows (see relationships: used by ApiClient.cs).

## AuthDtos.cs (LoginResponse)
Represents server response after authentication including tokens.

The [LoginResponse](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) record is the structured server reply to a successful authentication, containing the short-lived Token, the RefreshToken, an ExpiresAt timestamp, and identifying fields like Username with optional display personalization. Clients (like the [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md)) consume LoginResponse to populate their in-memory token state and to drive expiration/refresh logic; because it contains the expiry moment, consumers can decide when to call RefreshTokenAsync or LoginWithRefreshTokenAsync instead of issuing unauthenticated requests.

## AuthDtos.cs (RefreshRequest)
Represents refresh token request for renewing authentication.

The [RefreshRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) is the DTO used to request new authentication tokens from the server using a refresh token. It is the lightweight, immutable payload the ApiClient will serialize when it invokes its refresh endpoint (RefreshTokenAsync / LoginWithRefreshTokenAsync) so the server can validate the refresh token and return a new [LoginResponse](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md).

## IEchoHubClient.cs
Interface for EchoHub client surface used by ApiClient to perform operations.

The [IEchoHubClient](../Code/src/EchoHub.Core/Contracts/IEchoHubClient.cs.md) interface defines the callback surface that a client implementing the real-time hub must provide: methods such as ReceiveMessage(MessageDto), UserJoined(channelName, username, UserPresenceDto?), UserLeft, ChannelUpdated(ChannelDto), UserStatusChanged, UserKicked, UserBanned, MessageDeleted, ChannelDeleted, ChannelNuked, ForceDisconnect, and Error. The doc shows example minimal implementations that log or handle these events quickly and non-blockingly. While the ApiClient handles HTTP and token management, this interface is the typed contract used by any hub/transport layer to deliver server-initiated events to client code; the relationship shows IEchoHubClient depends on DTO types like those in [ChatDtos](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) and [ProfileDtos](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md), which are delivered through these callbacks.

## ChatDtos.cs
`AttachmentDto` collaborates directly with `ApiClient` and other members of this topic (10 dependency links).

The [ChatDtos](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) file defines message and channel payloads used across both HTTP API and hub callbacks. In particular, the [AttachmentDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) record carries Kind (AttachmentKind), Url, FileName, FileSize, and an optional AsciiPreview; it represents a message attachment's metadata and is the shape ApiClient sends or receives when uploading, downloading, or rendering attachments. Other DTOs in the same file (MessageDto, ChannelDto, ChannelMetaDto, SendMessageRequest, SendUrlRequest, ReplyRefDto, etc.) are the structured inputs and outputs ApiClient uses for channel operations and that appear on the [IEchoHubClient](../Code/src/EchoHub.Core/Contracts/IEchoHubClient.cs.md) callbacks. The docs note an important detail: in end-to-end encrypted channels the content behind the Url (and previews) may be ciphertext opaque to the server, which affects how clients process the Url returned in AttachmentDto.

## ModerationDtos.cs
`AssignRoleRequest` collaborates directly with `ApiClient` and other members of this topic (4 dependency links).

The [ModerationDtos](../Code/src/EchoHub.Core/DTOs/ModerationDtos.cs.md) file provides small, immutable payloads for moderation actions; the [AssignRoleRequest](../Code/src/EchoHub.Core/DTOs/ModerationDtos.cs.md) record carries a Username and a ServerRole value and is intended to be sent to moderation endpoints to request a role change. The file also contains BanRequest, KickRequest, and MuteRequest records used for banning, kicking, and muting operations. The ApiClient serializes these DTOs when invoking its moderation methods (AssignRoleAsync, BanUserAsync, KickUserAsync, MuteUserAsync), so moderation actions are expressed as data objects across the HTTP boundary.

## ProfileDtos.cs
`AvatarUploadResponse` collaborates directly with `ApiClient` and other members of this topic (4 dependency links).

The [ProfileDtos](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) file defines small user-profile payloads used by profile/update and avatar upload endpoints. The [AvatarUploadResponse](../Code/src/EchoHub.Core/DTOs/ProfileDtos.cs.md) record holds AvatarAscii, the ASCII-art representation returned after an avatar upload; ApiClient's UploadAvatarAsync returns or deserializes this DTO so callers can display or store the ASCII preview. UpdateProfileRequest and UpdateStatusRequest are optional-field records used for partial profile updates and are the payloads ApiClient will send via UpdateProfileAsync.

How the pieces fit

The ApiClient is the HTTP façade: it consumes and produces the DTOs in AuthDtos, ChatDtos, ModerationDtos, and ProfileDtos when calling server endpoints and populating client state. Authentication flows center on the LoginRequest/LoginResponse/RefreshRequest DTOs and ApiClient methods that set and refresh Token/RefreshToken and expose helpers like GetValidTokenAsync and EnsureAuthenticated. Separately, real-time server-to-client events are delivered through the [IEchoHubClient](../Code/src/EchoHub.Core/Contracts/IEchoHubClient.cs.md) callback interface using the same Chat and Profile DTOs, keeping transport and event handling decoupled while the ApiClient handles request/response semantics and token lifecycle.

---
*Covers 8 of 8 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-23 05:50:51 UTC*
