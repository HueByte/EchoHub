# ChannelsController.cs

> **Source:** `src/EchoHub.Server/Controllers/ChannelsController.cs`

## Contents

- [ChannelsController](#channelscontroller)
  - [ChannelsController (constructor)](#channelscontroller-constructor)
  - [CreateChannel](#createchannel)
  - [DeleteChannel](#deletechannel)
  - [GetChannelCrypto](#getchannelcrypto)
  - [MapChannelError](#mapchannelerror)
  - [ParseKind](#parsekind)
- [GetChannelMeta](#getchannelmeta)
- [GetChannels](#getchannels)
- [RekeyChannel](#rekeychannel)
- [SendMessageWithAttachments](#sendmessagewithattachments)
- [SendUrl](#sendurl)
- [UpdateTopic](#updatetopic)

---

## ChannelsController
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/channels")]
[Authorize]
[EnableRateLimiting("general")]
public class ChannelsController : ControllerBase
```


Exposes the HTTP surface for channel-related operations under the route prefix api/channels. Authenticated clients use this controller to list and create channels, retrieve public crypto metadata and human-facing channel summaries, perform passphrase rewraps (rekey), update topics, delete channels, and post messages (including multipart uploads). Prefer calling these endpoints from client code or tests; use the underlying services (IChannelService, IMessageEncryptionService, etc.) directly only when you need to bypass HTTP semantics or perform server-side orchestration.

## Remarks
This controller is a thin HTTP façade that orchestrates several backend services rather than implementing business logic itself. It enforces [Authorize] and configurable rate-limiting (attributes show general and upload policy groups) and delegates persistence, file storage, ASCII preview generation, encryption operations, and chat routing to injected dependencies such as IChannelService, EchoHubDbContext, FileStorageService, ImageToAsciiService, IMessageEncryptionService, IChatService and UploadLimits. Upload size and multipart limits are applied at runtime using the UploadLimits configuration rather than compile-time attributes so the controller can honor configurable limits for large attachments.

## Notes
- GetChannelCrypto returns public crypto metadata and the PBKDF2 salt clients need to derive a join credential; it never returns the wrapped room key (that is issued only after a successful join).
- RekeyChannel re-wraps the room key to change the passphrase; historical messages are not re-encrypted (the room content key itself does not change).
- SendMessageWithAttachments applies upload limits at runtime from UploadLimits; the controller trusts clients for encrypted-channel attachments (clients must declare each file's kind and provide room-encrypted previews), while for non-encrypted channels the server may inspect files and generate ASCII previews for images.

---

### ChannelsController (constructor)
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** constructor

```csharp
public ChannelsController(
        IChannelService channelService,
        EchoHubDbContext db,
        FileStorageService fileStorage,
        ImageToAsciiService asciiService,
        IHttpClientFactory httpClientFactory,
        IChatService chatService,
        IMessageEncryptionService encryption,
        UploadLimits uploadLimits,
        ILogger<ChannelsController> logger)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelService` | [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) | — |
| `db` | [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) | — |
| `fileStorage` | [`FileStorageService`](../Services/FileStorageService.cs.md) | — |
| `asciiService` | [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md) | — |
| `httpClientFactory` | `IHttpClientFactory` | — |
| `chatService` | [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) | — |
| `encryption` | [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) | — |
| `uploadLimits` | [`UploadLimits`](../Config/UploadLimits.cs.md) | — |
| `logger` | `ILogger<ChannelsController>` | — |


The ChannelsController constructor wires up the controller by receiving its dependencies through dependency injection and assigning them to private fields. This pattern allows the controller to orchestrate channel-related functionality by delegating to dedicated services such as IChannelService, EchoHubDbContext, FileStorageService, ImageToAsciiService, IHttpClientFactory, IChatService, IMessageEncryptionService, UploadLimits, and `ILogger<ChannelsController>`. The framework supplies these collaborators at creation time, enabling a testable, loosely coupled design where concerns are separated and easily mockable for unit tests. This constructor is invoked by the ASP.NET Core runtime during request handling, not by consumer code directly.

## Remarks
The constructor centralizes the wiring of the controller's collaborators, which supports clean separation of concerns and testability. It enables the ChannelsController to delegate specialized tasks (e.g., data access, file handling, image processing, HTTP calls, chat interactions, and encryption) to dedicated services rather than embedding logic directly.

The lack of explicit null validation means misconfigured dependency injection (missing service registrations) could surface as NullReferenceExceptions later when members are used. Relying on the DI container to validate registrations is common, but tests should provide explicit mocks to ensure predictable behavior.

## Notes
- The constructor does not perform null checks; ensure all dependencies are registered in the DI container to avoid runtime null reference issues.
- When writing unit tests for ChannelsController, provide concrete or mock implementations for all injected services to exercise behavior reliably.

---

### CreateChannel
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `request` | [`CreateChannelRequest`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** `[HttpPost]
    public async `Task<IActionResult>``


Source Code
The CreateChannel action handles the HTTP POST to create a new channel for the authenticated user. It first verifies authentication by pulling the user ID from the current user’s claims; if the claim is missing, it responds with Unauthorized and an ErrorResponse indicating that authentication is required. It then delegates the actual creation to the channel service via CreateChannelAsync, passing the caller’s GUID along with the channel properties supplied in the request (Name, Topic, IsPublic, Password, EncryptionSalt, WrappedRoomKey). If the service reports a failure, the action returns a mapped error via MapChannelError. If a channel is successfully created and it is public, it broadcasts the updated channel through the chat service to notify connected clients. Finally, it returns a 201 Created response with the location of the new channel and the Channel data in the response body.

Dependencies
- IActionResult
- ErrorResponse
- User
- ClaimTypes
- Guid
- Channel

Dependency APIs (verified signatures)
- record [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) (`src/EchoHub.Core/DTOs/CommonDtos.cs`)
- property [`User`](../../EchoHub.Core/Models/User.cs.md) (`src/EchoHub.Core/Models/RefreshToken.cs`)
- class [`Channel`](../../EchoHub.Core/Models/Channel.cs.md) (`src/EchoHub.Core/Models/Channel.cs`)
  - `Guid Id`
  - `string Name`
  - `string? Topic`
  - `bool IsPublic`
  - `bool IsSystem`
  - `string? PasswordHash`
  - `string? EncryptionSalt`
  - `string? WrappedRoomKey`
  - `DateTimeOffset CreatedAt`
  - `Guid CreatedByUserId`
  - `List<Message> Messages`

Symbol To Document
- Name: CreateChannel
- Kind: method
- File: src/EchoHub.Server/Controllers/ChannelsController.cs
- Language: csharp
- ID: 373f63c8-2a87-460c-9821-46640d93a9fc

## Remarks
Creates a channel on behalf of the authenticated user and encapsulates the orchestration between the domain service and the HTTP response surface. It relies on _channelService to enforce business rules and persistence, and on _chatService to refresh client views when appropriate. This action adheres to RESTful semantics by returning 401 for unauthenticated requests, propagating domain errors via MapChannelError, broadcasting updates for public channels, and signaling successful creation with 201 and the new channel resource.

## Notes
- Be aware that Guid.Parse is used on the user ID claim. If the claim value is not a valid GUID, this will throw. Consider validating with Guid.TryParse at the call site if you anticipate non-GUID claim values.
- The publication check (IsPublic) gates whether a channel update is broadcast to clients; non-public channels skip broadcasting to peers.


---

### DeleteChannel
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpDelete("{channel}")]
    public async Task<IActionResult> DeleteChannel(string channel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}"` | — | — |


Deletes a channel for the authenticated user by handling an HTTP DELETE request to the channel route. It reads the user's ID from the authentication claims, delegates the deletion to the channel service using that ID and the channel name, and, on success, broadcasts the deletion to the chat service before returning HTTP 204 No Content. If authentication is missing, the method responds with 401 Unauthorized and an ErrorResponse.

## Remarks
This endpoint acts as a thin HTTP boundary that orchestrates authentication, domain deletion, and cross-service notification. It centralizes HTTP-level error handling (Unauthorized, error mapping) while delegating business rules to the channel service and the side-effect of notifying the chat service. The normalization of the channel name for the broadcast (lowercase and trimmed) helps ensure consumers react to a consistent channel identifier.

## Notes
- Authentication is required; requests without a valid NameIdentifier claim result in 401 Unauthorized with an ErrorResponse.
- The broadcast step uses channel.ToLowerInvariant().Trim(); differences between input casing and broadcast casing could affect downstream consumers.
- If the channel is deleted successfully but the broadcast fails, the method will surface a failure (no explicit retry here); consider compensating actions if eventual consistency is important.

---

### GetChannelCrypto
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpGet("{channel}/crypto")]
    public async Task<IActionResult> GetChannelCrypto(string channel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/crypto"` | — | — |


GetChannelCrypto is an HTTP GET action on ChannelsController that exposes essential cryptographic metadata for a channel. It indicates whether the channel is end-to-end encrypted and provides the PBKDF2 salt clients need to derive their join credential. The endpoint deliberately does not return the wrapped room key; that secret is only handed out after a successful join. The action delegates retrieval to the channel service and translates the result into standard HTTP responses: 200 OK with the crypto data when the channel exists, or 404 Not Found with an ErrorResponse if the channel does not exist.

## Remarks
By wrapping the service call behind a minimal HTTP surface, this symbol centralizes how cryptographic metadata is surfaced while keeping the actual cryptographic material protected. It demonstrates a clear separation of concerns: business logic lives in the ChannelService, while the controller handles HTTP semantics and error translation. The exposed salt enables client-side credential derivation, while the wrapped key remains strictly withheld until the proper join flow.

## Notes
- The action does not perform explicit authorization; ensure the surrounding middleware or route configuration enforces the intended access policy.
- It returns 404 with a generic ErrorResponse when the channel does not exist; clients should handle this scenario as an absence of channel crypto metadata.
- Do not rely on this endpoint to retrieve any sensitive material beyond allowed cryptographic metadata; the wrapped key must never be exposed through this action.

---

### MapChannelError
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
private IActionResult MapChannelError(ChannelOperationResult result) => result.Error switch
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `result` | [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) | — |

**Returns:** `IActionResult`


Converts a ChannelOperationResult into an API response by pattern-matching on result.Error and returning an appropriate HTTP result that wraps an ErrorResponse. It centralizes the translation from channel-domain errors to standard HTTP status codes (400, 403, 404, 409) so the rest of the controller does not duplicate error handling logic.

## Remarks
This encapsulates the error-handling policy for channel operations, ensuring clients see consistent HTTP semantics across all channel actions. It decouples domain error codes from HTTP choices, so updates to status codes or payload shapes can be made in one place rather than at every call site.

## Notes
- The branches pass result.ErrorMessage! into ErrorResponse; if ErrorMessage can be null for any mapped error, this will throw at runtime.
- New ChannelError values require extending this switch to preserve the API's error contract.


---

### ParseKind
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
private static AttachmentKind ParseKind(string? kind) => kind?.ToLowerInvariant() switch
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `kind` | `string?` | — |

**Returns:** [`AttachmentKind`](../../EchoHub.Core/Models/AttachmentKind.cs.md)


This private helper translates a nullable string that labels an attachment into a concrete AttachmentKind enum. It uses a case-insensitive comparison (ToLowerInvariant) to recognize 'image' and 'audio' and map them to AttachmentKind.Image and AttachmentKind.Audio, respectively; any other label (including null) falls back to AttachmentKind.File. Callers rely on this mapping when normalizing incoming attachment metadata before further processing in the channel/server pipeline.

## Remarks
Centralizes the normalization logic so all attachment-kind labels are interpreted consistently across the server. By funneling strings through this method, the rest of the attachment processing can operate on a well-defined enum, reducing branching and potential mismatches.

## Notes
- Unknown labels are treated as File by design; if a new kind is introduced, update this method or extend the enum.
- Because the method is private, it's exercised via the class's public APIs; ensure tests cover scenarios that exercise this mapping through those entry points.

---

## GetChannelMeta
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpGet("{channel}/meta")]
    public async Task<IActionResult> GetChannelMeta(string channel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/meta"` | — | — |


Retrieves the channel metadata for a given channel identifier via HTTP GET. It returns key overview details such as message count, the number of unique posters, an estimated size, the creation date, and the room id. These metadata are tracked by the server and are available even for encrypted channels, where the server cannot access the actual messages. If the channel does not exist, it responds with 404 and an ErrorResponse; otherwise it returns the metadata payload with a 200 OK.

## Remarks
This endpoint provides a read-only surface for obtaining channel overview information without exposing message contents. It enables clients to populate channel lists or dashboards while preserving message privacy, including for encrypted channels. By delegating the data retrieval to _channelService.GetChannelMetaAsync, the API keeps data access concerns centralized and allows the underlying storage/collection strategy to evolve without changing the surface contract.

## Notes
- The caller must handle a 404 NotFound with an ErrorResponse when the channel is missing. The error payload documents the failure reason.
- The endpoint exposes only metadata about a channel; actual messages remain inaccessible, preserving privacy for encrypted channels.
- The operation is asynchronous; consider service performance characteristics or potential caching strategies if metadata is requested frequently.

---

## GetChannels
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpGet]
    public async Task<IActionResult> GetChannels([FromQuery] int offset = 0, [FromQuery] int limit = 50)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `offset` | `int` | `0` |
| `limit` | `int` | `50` |

**Returns:** `[HttpGet]
    public async `Task<IActionResult>``


Gets a paged list of channels for the authenticated user. It enforces authentication by checking the user claims, reads the user's GUID from the claims, normalizes paging parameters (offset non-negative; limit clamped to 1–100), and delegates to the channel service to retrieve the channels, returning the result in an HTTP 200 response.

## Remarks

This action is intentionally thin: it performs authentication, input normalization, and orchestration between the API layer and the domain service. Centralizing paging bounds and user identification here provides consistent behavior and error handling for per-user channel retrieval across clients.

## Notes

- Be aware that if the NameIdentifier claim is present but is not a valid GUID, Guid.Parse will throw. Prefer Guid.TryParse or ensure identity claims are well-formed.
- The limit is clamped to the range 1–100; requests outside that range are adjusted to the nearest bound.

---

## RekeyChannel
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpPost("{channel}/rekey")]
    public async Task<IActionResult> RekeyChannel(string channel, [FromBody] RekeyChannelRequest request)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/rekey"` | — | — |


Changes an encrypted channel's passphrase by re-wrapping its room key. The caller proves knowledge of the existing passphrase via the old authentication key, and this operation preserves history by not changing the room content key.

## Remarks
RekeyChannel acts as a thin HTTP boundary that enforces authentication and delegates the cryptographic work to the channel service. By re-wrapping the existing room key instead of re-encrypting the historical content, it minimizes disruption while changing access controls. The controller handles authentication and error translation, while RekeyChannelAsync encapsulates the cryptographic policy in the domain layer.

## Notes
- Authentication is mandatory; if the user is not authenticated, the endpoint returns 401 Unauthorized with ErrorResponse("Authentication required.").
- The code uses Guid.Parse on the NameIdentifier claim; if the claim is present but not a valid GUID, a runtime exception may be thrown.

---

## SendMessageWithAttachments
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpPost("{channel}/messages")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> SendMessageWithAttachments(string channel, [FromQuery] string? size = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/messages"` | — | — |


Use SendMessageWithAttachments when you need to post a chat message to a channel that includes optional text and one or more attachments, while enforcing per-channel upload limits and channel permissions.

It supports both cleartext and end-to-end encrypted channels: in cleartext channels the server inspects file kinds to render ASCII previews and decrypts the content, while in encrypted channels the client uploads ciphertext with per-file kind and a pre-rendered encrypted preview and the server never inspects the ciphertext.

## Remarks

This endpoint centralizes the server-side orchestration for uploading messages with attachments, coordinating authentication via user claims, channel validation and mutability checks, multipart form handling, per-attachment processing, and interaction with the encryption and upload-limit subsystems. It relies on collaborators such as the channel service, the database context, and the encryption helper to enforce read-only channels, mute state, and maximum message length in a consistent manner. By encapsulating these concerns, it ensures secure, policy-compliant message delivery and prevents plaintext exposure of encrypted payloads. In short, it is the single integration point for sending rich messages with attachments in EchoHub.Server.

## Notes

- The request size is governed at runtime by UploadLimits; configure this to control maximum allowed payloads.
- For encrypted channels, ensure that per-file previews are provided in the ciphertext workflow and that file order remains aligned with the declared previews to avoid misrendering on the client.

---

## SendUrl
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpPost("{channel}/send-url")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> SendUrl(string channel, [FromBody] SendUrlRequest request, [FromQuery] string? size = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/send-url"` | — | — |


SendUrl is an HTTP POST endpoint on ChannelsController that accepts a channel name, a request body containing an image URL, and an optional size parameter. It authenticates the caller, validates the channel, enforces channel policies (rejects system/read-only and end-to-end encrypted channels), validates the URL, downloads the image server-side, enforces size limits, validates the image format, saves the file, and generates an ASCII preview for display in the channel.

## Remarks

Centralizes remote image ingestion with strict, server-side validation to prevent improper content, inconsistent client behavior, or abuse. The endpoint relies on the application's security and storage abstractions: it checks user claims, ensures channel permissions, uses FileStorage to persist the file, and uses ImageToAsciiService to produce a lightweight ASCII representation for previews. The EnableRateLimiting("upload") attribute signals this is a potentially resource-intensive operation and should be throttled to guard against abuse.

## Notes

- Requires authentication; missing user claims yield Unauthorized responses with a helpful error.
- Validates channel state: if the channel does not exist, is system (read-only), or is encrypted, it responds with NotFound/403/400 and an ErrorResponse explaining the reason.
- Validates the supplied URL and only accepts http/https URLs; invalid URLs or unsupported schemes produce a BadRequest with a descriptive message.
- Downloads the image server-side using an HttpClient named "ImageDownload". It handles timeouts and HTTP errors by returning BadRequest with a clear message.
- Enforces file size limits via _uploadLimits.MaxImageSizeBytes before and after downloading the content.
- Validates that the downloaded content is a real image (JPEG, PNG, GIF, WebP) before persisting.
- Determines a filename from the URL or Content-Type; if missing, it falls back to a generated name with an appropriate extension.
- Persists the file and creates an ASCII representation (via ImageToAsciiService) for downstream use.


---

## UpdateTopic
> **File:** `src/EchoHub.Server/Controllers/ChannelsController.cs`  
> **Kind:** method

```csharp
[HttpPut("{channel}/topic")]
    public async Task<IActionResult> UpdateTopic(string channel, [FromBody] UpdateTopicRequest request)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `"{channel}/topic"` | — | — |


Updates a channel's topic for the currently authenticated user via HTTP PUT. It verifies authentication, calls ChannelService.UpdateTopicAsync with the user's ID, the channel, and the new topic, and on success broadcasts the channel update before returning the updated channel; on failure or missing authentication, it yields an HTTP error.

## Remarks

Acts as the HTTP API boundary for updating a channel topic, delegating the actual update to the domain service and handling authentication. It centralizes error translation via MapChannelError and ensures clients are informed of changes in real time by broadcasting after a successful update.

## Notes

- Be aware that Guid.Parse could throw if the user claim is not a valid GUID; consider Guid.TryParse to avoid runtime exceptions.
- The broadcast channel is normalized by lowercasing and trimming the channel name; this affects how subscribers perceive channel identifiers in updates.

---