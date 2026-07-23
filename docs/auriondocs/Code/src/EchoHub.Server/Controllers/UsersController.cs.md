# UsersController

> **File:** `src/EchoHub.Server/Controllers/UsersController.cs`  
> **Kind:** class

*Figure: How UsersController works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
UsersController["UsersController: receives HTTP request"]
UserService["UserService: GetUserProfileAsync(username)"]
ErrorResponse["ErrorResponse: create error response (message)"]
User["User: profile DTO / updated user"]
UpdateProfileRequest["UpdateProfileRequest: request body"]
IUserService["IUserService: UpdateProfileAsync(userId, displayName, bio, nicknameColor)"]
UserOperationResult["UserOperationResult: IsSuccess + User"]
UserError["UserError: domain error result"]
UploadLimits["UploadLimits: MaxAvatarSizeBytes"]

UsersController -->|"GET {username}/profile"| UserService
UserService -->|"returns null"| ErrorResponse
ErrorResponse -->|"404 NotFound (User not found)"| UsersController
UserService -->|"returns profile"| User
User -->|"200 OK (profile)"| UsersController

UsersController -->|"PUT /profile with UpdateProfileRequest"| UpdateProfileRequest
UpdateProfileRequest -->|"no userId claim"| ErrorResponse
UpdateProfileRequest -->|"has userId claim -> call UpdateProfileAsync"| IUserService
IUserService -->|"returns UserOperationResult"| UserOperationResult
UserOperationResult -->|"IsSuccess == false"| UserError
UserError -->|"MapUserError -> ErrorResponse"| ErrorResponse
UserOperationResult -->|"IsSuccess == true"| User
User -->|"200 OK (updated user)"| UsersController

UsersController -->|"POST /avatar"| UsersController
UsersController -->|"no userId claim"| ErrorResponse
UsersController -->|"no form content or files"| ErrorResponse
UsersController -->|"file obtained from Request.Form.Files[0] -> check length"| UploadLimits
UploadLimits -->|"file length > MaxAvatarSizeBytes -> BadRequest"| ErrorResponse
```

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("general")]
public class UsersController : ControllerBase
```


Handles HTTP endpoints rooted at /api/users for authenticated user operations such as retrieving a user's public profile, updating the caller's profile, and uploading an avatar. The controller delegates business logic to IUserService and ImageToAsciiService, applies rate limiting and upload-size checks, and returns standard DTOs like ErrorResponse and AvatarUploadResponse.

## Remarks
This controller is the HTTP adapter for user-focused features: it validates requests and authorization, enforces upload and rate limits, converts uploaded images to ASCII art via ImageToAsciiService, and forwards profile and avatar changes to IUserService. It centralizes request-level concerns (model binding, auth, error translation) so the underlying services can remain framework-agnostic.

## Notes
- UploadAvatar requires a multipart/form POST (Request.HasFormContentType) and will reject requests with no files or files exceeding the configured UploadLimits.MaxAvatarSizeBytes.
- The controller reads the caller's user id from ClaimTypes.NameIdentifier and uses Guid.Parse; if the claim is present but malformed the parse will throw. The implementation assumes authenticated tokens supply a well-formed GUID.
- Take care when extending or changing image validation: FileValidationHelper.IsValidImage is called on the uploaded stream before ImageToAsciiService.ConvertToAscii is invoked. If validation reads the stream to its end without rewinding, the conversion will receive an empty stream — ensure the validation either rewinds the stream or operates on a buffered/copy of the data.