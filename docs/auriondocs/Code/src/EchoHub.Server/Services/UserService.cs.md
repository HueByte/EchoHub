# UserService

> **File:** `src/EchoHub.Server/Services/UserService.cs`  
> **Kind:** class

*Figure: How UserService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
start["Start RegisterUserAsync"]
start --> checkEmpty["Check username and password not empty"]
checkEmpty -->|"invalid"| emptyFail["Return UserOperationResult.Fail(UserError.ValidationFailed): Username and password required"]
checkEmpty -->|"valid"| regexCheck["Validate username with ValidationConstants.UsernameRegex()"]
regexCheck -->|"invalid"| regexFail["Return UserOperationResult.Fail(UserError.ValidationFailed): Username format invalid"]
regexCheck -->|"valid"| pwMinCheck["Check password length >= 6"]
pwMinCheck -->|"no"| pwMinFail["Return UserOperationResult.Fail(UserError.ValidationFailed): Password must be at least 6 characters"]
pwMinCheck -->|"yes"| pwMaxCheck["Check password length <= ValidationConstants.MaxPasswordLength"]
pwMaxCheck -->|"no"| pwMaxFail["Return UserOperationResult.Fail(UserError.ValidationFailed): Password exceeds max length"]
pwMaxCheck -->|"yes"| normalize["Normalize username (ToLowerInvariant().Trim())"]
normalize --> reservedCheck["If normalized == UsersController.DeletedUserName"]
reservedCheck -->|"yes"| reservedFail["Return UserOperationResult.Fail(UserError.ValidationFailed): This username is reserved"]
reservedCheck -->|"no"| dbScope["Create scope and get EchoHubDbContext"]
dbScope --> existsCheck["If EchoHubDbContext.Users.Any(u => u.Username == normalized)"]
existsCheck -->|"yes"| existsFail["Return UserOperationResult.Fail(UserError.AlreadyExists): Username is already taken"]
existsCheck -->|"no"| isFirstCheck["Determine isFirstUser = !EchoHubDbContext.Users.Any()"]
isFirstCheck -->|"true"| createUser["Create new User entity (new User { ... })"]
isFirstCheck -->|"false"| regMode["Check UserService.RegistrationMode (open, invite, closed)"]
regMode -->|"closed"| closedFail["Return UserOperationResult.Fail(UserError.ValidationFailed): Registration is closed on this server"]
regMode -->|"invite"| inviteTry["Call TryConsumeInviteAsync(EchoHubDbContext, inviteCode)"]
inviteTry -->|"error"| inviteFail["Return UserOperationResult.Fail(UserError.ValidationFailed): invite error returned"]
inviteTry -->|"ok"| createUser
regMode -->|"open"| createUser
createUser --> save["Save new User to EchoHubDbContext and assign roles (ServerRole may apply)"]
save --> success["Return UserOperationResult.Success(UserProfileDto)"]
```

```csharp
public class UserService : IUserService
```


Handles user registration and related user-management concerns for the server. Use `UserService` when you need a high-level operation that validates credentials, enforces server-wide registration policy, creates the initial server owner account, hashes passwords, and returns canonical [`UserOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) responses instead of interacting with [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) directly.

## Remarks
`UserService` encapsulates the rules and side effects required to create a new [`User`](../../EchoHub.Core/Models/User.cs.md) in the application: it validates the `username` with `ValidationConstants.UsernameRegex()`, enforces password length (minimum 6 characters and a maximum of `ValidationConstants.MaxPasswordLength`), normalizes the username to lowercase and trimmed form, prevents use of the reserved `Controllers.UsersController.DeletedUserName`, and checks uniqueness using [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md). The `RegistrationMode` property reads `Server:Registration` from `IConfiguration` and controls whether new sign-ups are allowed (`"open"`), require a valid invite (`"invite"`), or are disallowed (`"closed"`). The very first account created on an empty database is automatically assigned `ServerRole.Owner` to allow server bootstrap. For invite-based registration, `UserService` defers to the private `TryConsumeInviteAsync` routine which (per its comment) performs a guarded update so concurrent registrations cannot both consume the same invite.

## Example
```csharp
// Given an IUserService instance (e.g. resolved from DI):
var result = await userService.RegisterUserAsync("alice", "s3cret!", displayName: "Alice");
if (result.IsSuccess)
{
    var profile = result; // result carries the created [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) via `UserOperationResult.Success`
    // proceed with login or return profile to caller
}
else
{
    // handle failure: message and [`UserError`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) are available from the [`UserOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) returned
}
```

## Notes
- The `RegistrationMode` is computed on each access from `IConfiguration["Server:Registration"]`; changing that configuration at runtime affects subsequent calls to `RegisterUserAsync` immediately.
- The first created user bypasses invite/closed checks and is assigned `ServerRole.Owner`; this is intentional to allow initial server bootstrap and means the first successful registration must be protected in deployment scenarios.
- `RegisterUserAsync` normalizes usernames to lowercase and trims them before uniqueness checks, so the system enforces case-insensitive username uniqueness. Passwords are hashed with `BCrypt.Net.BCrypt.HashPassword` before being stored.