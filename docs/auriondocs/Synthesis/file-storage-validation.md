# File uploads, storage, and validation

> Client and server file handling: validation, storage, and upload flows for media and avatars.

*Figure: How File uploads, storage, and validation works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
sequenceDiagram
participant Client
participant ApiClient.cs
participant UploadAvatarAsync
participant FileValidationHelper
participant FileStorageService
participant AuthDtos.cs
participant LoginRequest
participant ChatDtos.cs
participant ChannelDto

Client->>ApiClient.cs: Request UploadAvatar
activate ApiClient.cs
ApiClient.cs->>UploadAvatarAsync: Invoke UploadAvatarAsync
activate UploadAvatarAsync

UploadAvatarAsync->>FileValidationHelper: Validate file
activate FileValidationHelper
FileValidationHelper-->>UploadAvatarAsync: Validation result OK
deactivate FileValidationHelper

alt Invalid file
    UploadAvatarAsync-->>ApiClient.cs: Return validation error
    deactivate UploadAvatarAsync
    ApiClient.cs-->>Client: Error response
else Valid file
    UploadAvatarAsync->>FileStorageService: Store file and obtain URL
    activate FileStorageService
    FileStorageService-->>UploadAvatarAsync: Return storage URL
    deactivate FileStorageService

    UploadAvatarAsync->>LoginRequest: Create LoginRequest(Username, Password)
    LoginRequest-->>UploadAvatarAsync: LoginRequest instance

    UploadAvatarAsync->>AuthDtos.cs: Use auth DTOs to prepare auth header
    AuthDtos.cs-->>UploadAvatarAsync: Auth token

    UploadAvatarAsync->>ChatDtos.cs: Build ChannelDto with avatar URL
    ChatDtos.cs-->>ChannelDto: ChannelDto instance

    UploadAvatarAsync->>ApiClient.cs: Send update with ChannelDto and auth
    ApiClient.cs-->>Client: Success response with avatar URL
    deactivate UploadAvatarAsync
end

deactivate ApiClient.cs
```

# File uploads, storage, and validation

This guide explains how client-side upload calls, lightweight file validation, and a simple disk-backed storage implementation cooperate to accept and persist user avatars and other media. It highlights the small, focused responsibilities: the client call surface that carries credentials and files, a validator that performs non-destructive header checks, and a filesystem service that owns GUID-based identities and disk I/O. Together they form a straightforward request flow from the client into a single-host storage surface.

## FileStorageService.cs
Handles server-side file storage and retrieval.

The [FileStorageService](../Code/src/EchoHub.Server/Services/FileStorageService.cs.md) class is a minimal filesystem-backed persister that reads a storage path from configuration (Storage:Path), ensures the directory exists, and exposes a small API to save, lookup, and delete files. Its SaveFileAsync method writes an uploaded stream to disk using a GUID-based filename while preserving the original extension and returns the generated fileId plus the concrete path; GetFilePath looks up the first matching file path for a given id (or null if none); DeleteFile attempts a best-effort removal. The implementation intentionally isolates disk I/O from higher layers so other server components can call into this API or swap a different storage backend without changing callers; it is optimized for single-host deployments and will surface underlying IO exceptions if deletion or writes fail.

## FileValidationHelper.cs
Validates files against allowed types and constraints before storage.

The [FileValidationHelper](../Code/src/EchoHub.Server/Services/FileValidationHelper.cs.md) static class centralizes quick, non-destructive checks to determine whether a stream contains a common image format and offers an extension-based IsAudioFile helper. The image checks examine leading signature (magic) bytes for JPEG, PNG, GIF, and WebP while preserving the incoming stream's position so callers don't have to reposition or buffer; IsValidImage requires a seekable stream and will return false for non-seekable or too-short inputs (WebP needs a 12-byte header). IsAudioFile only inspects file name extensions, so it is a lightweight, complementary test rather than a full MIME or content validation routine.

## ApiClient.cs
Uploads avatar images from the client to the server.

The [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) is a sealed, higher-level HTTP client that centralizes authentication token management (Token, RefreshToken, expiry) and exposes the EchoHub REST surface, including file operations like UploadAvatarAsync and UploadFileAsync and download helpers such as DownloadFileToTempAsync. It wraps an HttpClient, provides helpers for authenticated requests (EnsureAuthenticated, AuthenticatedRequestAsync) and token lifecycle (OnTokensRefreshed, SetTokens, RefreshTokenAsync), and ensures UploadAvatarAsync participates in that authenticated request flow so uploads carry valid credentials. Within this topic the client depends on the DTO types defined in the core project (see the relationships to [ChatDtos](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) and [AuthDtos](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md)), using those records for payloads and responses while leaving server-side validation and persistence to the server components.

## ChatDtos.cs
`ChannelDto` collaborates directly with `ApiClient` and other members of this topic (5 dependency links).

The [ChannelDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) is a compact, immutable record carrying channel metadata (Id, Name, nullable Topic, IsPublic, MessageCount, CreatedAt). As a pure data transfer object it has no behavior; the ApiClient and other layers use it to surface channel information safely across process boundaries. Its nullable Topic and DateTimeOffset CreatedAt are explicit design choices callers must handle when rendering or sorting channel information in the client.

## AuthDtos.cs
`LoginRequest` collaborates directly with `ApiClient` and other members of this topic (4 dependency links).

The [LoginRequest](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md) is a positional record carrying Username and Password for the login workflow. It is intended as an immutable payload used by [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) when calling authentication endpoints; the docs stress treating the password carefully (do not log it) and transmitting credentials only over TLS. The surrounding DTOs (e.g., LoginResponse) bundle tokens and expiry information that the ApiClient persists and uses to authorize subsequent file upload requests.

How the pieces fit

Client code uses [ApiClient](../Code/src/EchoHub.Client/Services/ApiClient.cs.md) to perform authenticated upload requests (for avatars or other files) and to receive token-bearing responses described by [AuthDtos](../Code/src/EchoHub.Core/DTOs/AuthDtos.cs.md). On the server side, incoming streams can be subjected to quick, non-destructive checks using [FileValidationHelper](../Code/src/EchoHub.Server/Services/FileValidationHelper.cs.md) to assert common image formats or to reject unsupported inputs, and successful uploads are persisted by [FileStorageService](../Code/src/EchoHub.Server/Services/FileStorageService.cs.md) which assigns GUID-based ids and writes files to the configured storage path. DTO types such as [ChannelDto](../Code/src/EchoHub.Core/DTOs/ChatDtos.cs.md) remain separate value carriers used by the ApiClient and not involved in storage mechanics, keeping responsibilities clearly separated across layers.

---
*Covers 5 of 5 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-08 17:07:16 UTC*
