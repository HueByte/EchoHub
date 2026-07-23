# ChannelsController.cs

> **Source:** `src/EchoHub.Server/Controllers/ChannelsController.cs`

## Contents

- [ChannelsController](#channelscontroller)
  - [CreateChannel](#createchannel)
  - [DeleteChannel](#deletechannel)
  - [GetChannelCrypto](#getchannelcrypto)
  - [GetChannelMeta](#getchannelmeta)
  - [GetChannels](#getchannels)
  - [MapChannelError](#mapchannelerror)
  - [ParseKind](#parsekind)
  - [RekeyChannel](#rekeychannel)
  - [SendMessageWithAttachments](#sendmessagewithattachments)
  - [SendUrl](#sendurl)
  - [UpdateTopic](#updatetopic)
- [ChannelsController (constructor)](#channelscontroller-constructor)

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


Exposes the channel-oriented HTTP API beneath `api/channels` for listing, creating, updating and deleting channels, for retrieving channel metadata and public crypto parameters, for changing an encrypted channel's passphrase, and for posting messages (including attachments). Reach for `ChannelsController` when implementing server-side channel management or wiring client HTTP calls: it is the main HTTP surface that enforces authentication, rate limits and upload policies for channel operations.

## Remarks
`ChannelsController` is a thin HTTP façade that delegates domain work to services such as [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md), [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) and [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) while persisting metadata via [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md). It centralizes cross-cutting concerns: request authorization (`[Authorize]`), rate limiting (the controller is annotated with `EnableRateLimiting("general")` and the attachment upload endpoint uses `EnableRateLimiting("upload")`), runtime-configured upload limits via the injected [`UploadLimits`](../Config/UploadLimits.cs.md), file handling via [`FileStorageService`](../Services/FileStorageService.cs.md), and image preview generation via [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md). The controller intentionally keeps cryptographic secrets off the public endpoints — for example, `GetChannelCrypto` returns only public metadata (including the PBKDF2 salt) and never hands out the wrapped room key; `RekeyChannel` re-wraps a channel's room key without re-encrypting historical messages.

## Notes
- The server does not attempt to decrypt or inspect message contents for end-to-end encrypted channels; encrypted attachments must be uploaded as ciphertext and the client must provide the declared `kind` and the room-encrypted `preview` aligned with attachment order. The controller treats those blobs as opaque.
- Request body and multipart limits are applied at runtime from the injected [`UploadLimits`](../Config/UploadLimits.cs.md) rather than using compile-time attributes like `[RequestSizeLimit]`. The implementation raises the request body ceiling from [`UploadLimits`](../Config/UploadLimits.cs.md) before the body is read to support configurable upload maxima.
- `RekeyChannel` changes how the room key is wrapped (the passphrase) but does not re-encrypt existing history — the underlying room content key remains the same, so historical ciphertext is not rewritten.

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


Creates a new channel for the authenticated user via HTTP POST. It first authenticates by reading the `NameIdentifier` from `ClaimTypes.NameIdentifier` in [`User`](../../EchoHub.Core/Models/User.cs.md); if missing, it returns `Unauthorized` with an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md). On success, it calls `_channelService.CreateChannelAsync` with the parsed GUID from the `NameIdentifier` claim and the fields from `request` (`Name`, `Topic`, `IsPublic`, `Password`, `EncryptionSalt`, `WrappedRoomKey`). If the result indicates failure, it returns the mapped error via `MapChannelError`. If the created channel is public, it notifies clients by calling `_chatService.BroadcastChannelUpdatedAsync`. Finally it returns `Created` with the new channel at `/api/channels/{result.Channel.Name}`.

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


Deletes a channel for the currently authenticated user. It first validates authentication by reading the `NameIdentifier` claim from [`User`](../../EchoHub.Core/Models/User.cs.md) and returns `Unauthorized` with an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) if missing; otherwise it calls `_channelService.DeleteChannelAsync` with the parsed `Guid` user id and the provided `channel` name. If the deletion succeeds it broadcasts the channel deletion to the chat subsystem via `_chatService.BroadcastChannelDeletedAsync` (the channel name lowercased and trimmed) and returns `NoContent`; if it fails, it returns the mapped error using `MapChannelError`.

## Remarks

This method acts as an orchestration boundary, ensuring only authenticated users can delete their channels and coordinating the domain operation with cross-service notification to keep clients in sync.

## Notes

- Potential exception if the `NameIdentifier` claim isn't a valid GUID; consider using `Guid.TryParse` or additional validation.

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


GetChannelCrypto is an HTTP GET endpoint that returns the public crypto metadata for a given channel, including whether the channel is end-to-end encrypted and the PBKDF2 salt used to derive the join credential. It never returns the wrapped room key; if the channel doesn't exist, the endpoint responds with `NotFound` and an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md); otherwise it returns the metadata with an `Ok(crypto)` result.

## Remarks
This endpoint centralizes crypto-configuration retrieval for a channel, keeping actual keys out of reach and clarifying that the response is metadata only. It delegates to `_channelService.GetChannelCryptoAsync(channel)` to obtain the data and uses the 404/not-found path to signal missing channels or missing crypto metadata. It sits in the `ChannelsController` and complements the security model by exposing minimal, auditable information required by clients to participate in encrypted joins.

## Notes
- If `_channelService.GetChannelCryptoAsync(channel)` returns null, the API responds with 404 via the same messaging, conflating a missing channel with missing crypto metadata.
- The endpoint does not expose any cryptographic material beyond the publicly exposable metadata; actual keys are never returned.

---

### GetChannelMeta
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


GetChannelMeta exposes channel metadata for a given `channel` via HTTP GET to `"{channel}/meta"`. It delegates to `_channelService.GetChannelMetaAsync(channel)` to assemble metadata such as message count, unique posters, estimated size, creation date, and room id. This remains available for encrypted channels as well since the server tracks this metadata independent of the messages. If the channel does not exist, the endpoint returns `NotFound(new ErrorResponse($"Channel '{channel}' does not exist."))`; otherwise it returns the metadata payload with `Ok(meta)`.

---

### GetChannels
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


GetChannels is an HTTP GET endpoint on the `ChannelsController` that returns a paged list of channels for the currently authenticated user. It reads the query parameters `offset` and `limit`, clamps them to sane bounds, parses the user GUID from the `NameIdentifier` claim, delegates to `_channelService.GetChannelsAsync(Guid.Parse(userIdClaim), offset, limit)`, and returns the data in an `Ok` response.

## Remarks
As an HTTP boundary, this method coordinates authentication and paging concerns, keeping the controller thin by delegating data retrieval to `_channelService.GetChannelsAsync(...)`. It relies on [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) to signal authentication failures and on the service to fetch domain data, forming a simple, testable conduit between the HTTP layer and business logic.

## Notes
- It assumes the `NameIdentifier` claim contains a valid GUID; if not, `Guid.Parse` will throw. Consider using `Guid.TryParse` or stricter claim validation to avoid runtime exceptions.

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


MapChannelError is a private helper in `ChannelsController` that translates a [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) into an HTTP response by switching on `result.Error`. It centralizes the mapping from domain channel errors (the [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) enum) to HTTP status results, covering common cases: `ChannelError.ValidationFailed` yields a `BadRequest` with an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) payload containing the error message, `ChannelError.AlreadyExists` yields `Conflict`, `ChannelError.NotFound` yields `NotFound`, `ChannelError.Forbidden` yields a 403 via `StatusCode(403, ...)`, and `ChannelError.Protected` yields `BadRequest`; any unlisted error falls back to a `BadRequest` with either the provided `ErrorMessage` or the string `"Unknown error."`. All branches construct the error payload with `new ErrorResponse(result.ErrorMessage!)` (except the fallback) to deliver structured error information to the client.

## Remarks
This helper encapsulates the error-to-HTTP translation for channel operations, ensuring consistent client-facing semantics across the controller. By funneling all channel-related errors through a single switch, changes to HTTP status mappings or payload shape can be made in one place. The method returns an `IActionResult` and always uses an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) payload to provide a predictable error contract to clients; callers do not need to repeat boilerplate error handling.

## Notes
- The code uses the null-forgiving operator on `ErrorMessage` in most branches; ensure `ErrorMessage` is populated for those [`ChannelError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) values, or risk a runtime null allocation.
- The default branch returns a `BadRequest` with either the provided message or a fallback of `"Unknown error."`, which avoids leaking a null payload but may obscure the underlying error if messages are not consistently set.

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


Converts an optional string describing an attachment into the corresponding [`AttachmentKind`](../../EchoHub.Core/Models/AttachmentKind.cs.md) enum value. It normalizes the input with `ToLowerInvariant()` and returns `AttachmentKind.Image` for `image`, `AttachmentKind.Audio` for `audio`, or `AttachmentKind.File` for any other value (including when the input is `null`).

## Remarks
By centralizing this mapping in a private helper, the server ensures consistent classification of attachments across callers and makes future changes to the mapping straightforward. The use of `ToLowerInvariant()` guarantees predictable behavior regardless of the runtime culture.

## Notes
- If a new attachment kind is introduced, this method must be updated; otherwise unknown values default to `AttachmentKind.File`.

---

### RekeyChannel
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


RekeyChannel rotates an encrypted channel's passphrase by re-wrapping its room key. It authenticates the caller via the `ClaimTypes.NameIdentifier` claim and requires knowledge of the old password (provided as `OldPassword` in the request) to authorize the change; the operation preserves history by not re-encrypting the room content key.

## Remarks
RekeyChannel delegates the actual rotation to `_channelService.RekeyChannelAsync`, which performs the rewrapping logic and returns a result. If the operation succeeds, the updated channel is returned with `Ok`, otherwise `MapChannelError` translates failures into the appropriate HTTP error response. The endpoint is exposed at the route `"{channel}/rekey"`, enforcing authentication at the boundary via the user identity claim.

## Notes
- Authentication relies on the presence of the `NameIdentifier` claim in [`User`](../../EchoHub.Core/Models/User.cs.md); if it is missing, the method responds with `Unauthorized(new ErrorResponse("Authentication required."))`.
- The code calls `Guid.Parse(userIdClaim)` on the claim value; if the `NameIdentifier` claim is present but not a valid GUID, an exception could be thrown at runtime.
- This operation re-wraps the room key to rotate the channel's passphrase without altering the underlying room content key, preserving historical data while changing access material.


---

### SendMessageWithAttachments
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


SendMessageWithAttachments posts a single chat message to a named channel, optionally including plaintext `content` and zero or more attachments delivered as multipart form data.

For non-encrypted channels the server may inspect attachments and render ASCII previews; for room-encrypted channels the client supplies ciphertext with per-file `kind` and a pre-rendered `preview`, and the server never inspects the ciphertext.

The action enforces runtime upload limits, validates authentication and channel state, requires multipart content with at least one attachment, and observes per-channel constraints such as maximum attachments per message and maximum message length for non-encrypted content.

## Remarks
`SendMessageWithAttachments` is a boundary between the chat surface and the attachment pipeline. It coordinates authentication, channel resolution, and per-channel policy (read-only channels, allowed attachment counts, and length limits), then delegates the heavier lifting of encryption handling and persistence to the underlying services (`_encryption`, `_channelService`, and the database context). By centralizing multipart request handling and per-file metadata (such as `Attachment.Kind` and `Attachment.AsciiPreview`), it provides a single, secure entry point for composing rich messages that may include both plaintext and encrypted payloads, while ensuring that encrypted channels never disclose raw attachment data to the server.

## Notes
- If the channel is encrypted, the endpoint relies on the client-provided per-file metadata (e.g., `kind` and `preview`) and does not perform server-side inspection of the ciphertext blobs; ensure consistency between client-provided metadata and channel state to avoid mismatches. 

---

### SendUrl
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


SendUrl is an HTTP POST action that enables an authenticated user to attach an image to a channel by URL. It coordinates authentication, channel validation, image download, format and size validation, storage, and ASCII preview generation, applying channel policies (such as read-only `IsSystem` channels and end-to-end encrypted `IsEncrypted` channels) before persisting the asset.

## Remarks
This action centralizes URL-based image delivery, delegating channel lookup to `` `_channelService` ``, remote download/validation to the HTTP client path, and persistence to ``_fileStorage``. It ensures that content is only added to writable channels and that encrypted channels disallow URL-based image sending, thereby reducing risk and keeping concerns isolated. The composition makes testing and reuse consistent with other upload flows in the codebase, leveraging collaborators such as [`FileValidationHelper`](../../EchoHub.Core/Services/FileValidationHelper.cs.md) for image validation and [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md) for the ASCII preview.

## Example
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var payload = new { Url = "https://example.com/image.png" };
var json = JsonSerializer.Serialize(payload);
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var client = new HttpClient(); // configure base address and authentication as needed
var response = await client.PostAsync("/channels/general/send-url?size=1024", content);
```

## Notes
- Requires authentication; requests without credentials yield `Unauthorized` with an [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md).
- Validates channel name via `ValidationConstants.ChannelNameRegex` and checks channel existence (`NotFound`) and state (`IsSystem` / `IsEncrypted`).
- Downloads the image using the named HttpClient `"ImageDownload"`, enforces the maximum size via `_uploadLimits.MaxImageSizeBytes`, and validates the actual image content with `FileValidationHelper.IsValidImage`.
- If the downloaded data cannot be interpreted as a supported image, returns a `BadRequest` with an explanatory message.

---

### UpdateTopic
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


UpdateTopic handles PUT requests to update a channel's topic for the authenticated user. It reads the `NameIdentifier` claim and, if missing, returns `Unauthorized(new ErrorResponse("Authentication required."))`; otherwise it calls `_channelService.UpdateTopicAsync(Guid.Parse(userIdClaim), channel, request.Topic)`, maps errors via `MapChannelError` on failure, and on success broadcasts the update with `_chatService.BroadcastChannelUpdatedAsync(result.Channel!, channel.ToLowerInvariant().Trim())` before returning `Ok(result.Channel)`.

## Remarks
This endpoint centralizes authentication checks and cross-service coordination for topic changes. It ensures only authenticated users can modify a channel topic and that updates are propagated to connected clients via the `_chatService.BroadcastChannelUpdatedAsync` call.

---

## ChannelsController (constructor)
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


The `ChannelsController` constructor wires the controller to its collaborators by accepting all required services via dependency injection and storing them for use in action methods. It is invoked by the ASP.NET Core DI container when handling channel-related requests, meaning developers should avoid manual instantiation and instead provide mocks or fakes for its dependencies in tests.

## Remarks
By composing [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md), [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), [`FileStorageService`](../Services/FileStorageService.cs.md), [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md), `IHttpClientFactory`, [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md), [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md), [`UploadLimits`](../Config/UploadLimits.cs.md), and `ILogger<ChannelsController>` in a single place, the constructor positions `ChannelsController` as a coordinator that delegates work to specialized services. This composition reflects a separation of concerns across persistence, media processing, HTTP communication, chat orchestration, encryption, and logging.

## Notes
- Do not instantiate `ChannelsController` yourself; rely on the DI container so tests can provide mocks or fakes.
- A constructor with many dependencies can indicate the controller has multiple responsibilities; consider extracting a higher-level service if you find yourself needing to mock many collaborators in tests.

---