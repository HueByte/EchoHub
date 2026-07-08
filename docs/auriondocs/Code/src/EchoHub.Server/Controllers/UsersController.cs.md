# UsersController

> **File:** `src/EchoHub.Server/Controllers/UsersController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("general")]
public class UsersController : ControllerBase
```


UsersController is an ASP.NET Core API controller that handles user-related endpoints under api/users. It coordinates profile retrieval, profile updates, and avatar uploads by delegating to IUserService and ImageToAsciiService, applying authentication and rate limiting along the way.

## Remarks

By centralizing these endpoints, the controller enforces consistent authentication checks, error translation, and input validation while keeping the business logic in the services. It demonstrates how per-endpoint rate limiting (general vs. upload) is applied and how a complex operation—converting uploaded images to ASCII art— is composed through dedicated services.

## Notes

- Authentication is enforced by requiring a valid NameIdentifier claim for profile updates and avatar uploads; requests without authentication receive 401 Unauthorized.
- UploadAvatar validates the request as form content with at least one file, enforces a maximum size from HubConstants.MaxAvatarSizeBytes, and uses FileValidationHelper to ensure the file is a valid image (JPEG, PNG, GIF, WebP).
- Domain results from IUserService are translated into standard HTTP responses via MapUserError, ensuring consistent error semantics across GetProfile, UpdateProfile, and avatar operations.