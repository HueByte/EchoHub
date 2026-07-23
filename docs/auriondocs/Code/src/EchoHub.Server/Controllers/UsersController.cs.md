# UsersController

> **File:** `src/EchoHub.Server/Controllers/UsersController.cs`  
> **Kind:** class

*Figure: How UsersController works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
    start["UsersController receives HTTP request"]
    route{"Route: which endpoint?"}

    start --> route

    %% GET profile flow
    route -->|"GET {username}/profile"| gpCall["Call IUserService.GetUserProfileAsync(username)"]
    gpCall --> profileNull{"Profile is null?"}
    profileNull -- Yes --> notFound["Return 404 NotFound(ErrorResponse)"]
    profileNull -- No --> okProfile["Return 200 Ok(profile)"]

    %% Update profile flow
    route -->|"PUT profile"| updAuth["Extract userIdClaim from User"]
    updAuth --> updAuthNull{"userIdClaim is null?"}
    updAuthNull -- Yes --> updUnauthorized["Return 401 Unauthorized(ErrorResponse)"]
    updAuthNull -- No --> updCall["Call IUserService.UpdateProfileAsync(Guid.Parse(userIdClaim), UpdateProfileRequest fields)"]
    updCall --> updResult{"result.IsSuccess?"}
    updResult -- No --> updError["Return mapped UserError response (UserError)"]
    updResult -- Yes --> updOk["Return 200 Ok(result.User)"]

    %% Upload avatar flow (truncated)
    route -->|"POST avatar"| uploadAuth["Extract userIdClaim from User"]
    uploadAuth --> uploadAuthNull{"userIdClaim is null?"}
    uploadAuthNull -- Yes --> uploadUnauthorized["Return 401 Unauthorized(ErrorResponse)"]
    uploadAuthNull -- No --> formCheck{"Request.HasFormContentType && Request.Form.Files.Count > 0?"}
    formCheck -- No --> noFile["Return 400 BadRequest(ErrorResponse: No file uploaded.)"]
    formCheck -- Yes --> fileAssign["Select first file from Request.Form.Files (file)"]
    fileAssign --> sizeCheck{"file.Length > UploadLimits.MaxAvatarSizeBytes?"}
    sizeCheck -- Yes --> tooLarge["Return 400 BadRequest(ErrorResponse: File size exceeds maximum)"]
    sizeCheck -- No --> continue["Proceed with avatar processing (truncated)"]
```

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("general")]
public class UsersController : ControllerBase
```


Controller that exposes the HTTP surface for user profile and account-related operations under `api/users`, including profile retrieval, profile updates and avatar upload. Reach for `UsersController` when you need to translate authenticated HTTP requests into calls to the user, storage and broadcasting services (for example, calling [`IUserService`](../../EchoHub.Core/Contracts/IUserService.cs.md) to update a profile or [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md) to convert an uploaded avatar).

## Remarks
`UsersController` is an orchestration layer: it validates and normalizes incoming HTTP requests, enforces authentication and rate-limiting policies, performs lightweight validation (for example file size and image format checks), and delegates the domain work to collaborators such as [`IUserService`](../../EchoHub.Core/Contracts/IUserService.cs.md), [`ImageToAsciiService`](../../EchoHub.Core/Services/ImageToAsciiService.cs.md), [`FileStorageService`](../Services/FileStorageService.cs.md), [`PresenceTracker`](../Services/PresenceTracker.cs.md) and the collection of [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) implementations. The controller centralizes common web concerns (claim extraction via `User.FindFirstValue`, mapping service results to HTTP responses with `MapUserError`, and producing [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md)/[`AvatarUploadResponse`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) payloads) so the underlying services can remain focused on business logic. The `DeletedUserName` constant is a reserved tombstone username: [`UserService`](../Services/UserService.cs.md) will refuse to register it, and it is used when re-attributing messages after account deletion (messages re-attributed to `DeletedUserName`). Note also that exported account data includes stored messages but that any end-to-end encrypted room content remains ciphertext on the server (the controller preserves what the server stores, it does not decrypt client-side E2E content).

## Notes
- The controller is annotated with `Authorize`, so every action requires authentication by default. An action must be decorated with `AllowAnonymous` to be reachable without credentials.
- `UploadAvatar` expects a multipart/form-data request and will return `BadRequest` when `Request.Form.Files` is empty. It also enforces size limits using `_uploadLimits.MaxAvatarSizeBytes` and reports the limit in MB in the error text.
- `FileValidationHelper.IsValidImage` is used to allow only images (it recognizes JPEG, PNG, GIF and WebP). Because both validation and ASCII conversion operate on the same `Stream` (`file.OpenReadStream()`), ensure the validation method does not consume the stream or that the stream position is reset before calling `ImageToAsciiService.ConvertToAscii` — otherwise the conversion may see an empty stream.
- The controller extracts the caller identity using `User.FindFirstValue(ClaimTypes.NameIdentifier)` and then calls `Guid.Parse(...)`. If the claim is present but not a valid GUID this will throw; callers should ensure the claim is a GUID or the parsing should be hardened (for example with `Guid.TryParse`).
- Upload endpoints have a more specific rate limit: the controller-level `[EnableRateLimiting("general")]` applies broadly while `UploadAvatar` additionally uses `[EnableRateLimiting("upload")]`, so be aware of which policy will throttle a client.
- Many methods rely on `MapUserError` to convert domain errors into HTTP responses; consumers of these endpoints should expect standardized [`ErrorResponse`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) payloads for error cases.