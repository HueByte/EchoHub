# UserService

> **File:** `src/EchoHub.Server/Services/UserService.cs`  
> **Kind:** class

*Figure: How UserService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
Start["Start RegisterUserAsync"]
Start --> CheckEmpty
CheckEmpty["Check username and password not empty"]
CheckEmpty -->|"missing"| FailMissing
CheckEmpty -->|"present"| CheckUsernameRegex

FailMissing["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;Username and password are required.#quot;)"]

CheckUsernameRegex["Validate username with ValidationConstants.UsernameRegex()"]
CheckUsernameRegex -->|"invalid"| FailUsernameRegex
CheckUsernameRegex -->|"valid"| CheckPwdMin

FailUsernameRegex["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;Username must be 3-50 characters and contain only letters, digits, underscores, or hyphens.#quot;)"]

CheckPwdMin["Check password length >= 6"]
CheckPwdMin -->|"too short"| FailPwdShort
CheckPwdMin -->|"ok"| CheckPwdMax

FailPwdShort["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;Password must be at least 6 characters.#quot;)"]

CheckPwdMax["Check password length <= ValidationConstants.MaxPasswordLength"]
CheckPwdMax -->|"too long"| FailPwdLong
CheckPwdMax -->|"ok"| Normalize

FailPwdLong["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;Password must not exceed ValidationConstants.MaxPasswordLength characters.#quot;)"]

Normalize["Normalize username (ToLowerInvariant and Trim)"]
Normalize --> CheckReserved

CheckReserved["Compare normalized username to UsersController.DeletedUserName"]
CheckReserved -->|"reserved"| FailReserved
CheckReserved -->|"not reserved"| CreateScope

FailReserved["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;This username is reserved.#quot;)"]

CreateScope["Create scope and get EchoHubDbContext from _scopeFactory"]
CreateScope --> CheckExists

CheckExists["Check if db.Users.AnyAsync(u => u.Username == normalizedUsername)"]
CheckExists -->|"exists"| FailAlreadyExists
CheckExists -->|"not exists"| CheckIsFirstUser

FailAlreadyExists["Return UserOperationResult.Fail(UserError.AlreadyExists, #quot;Username is already taken.#quot;)"]

CheckIsFirstUser["Determine isFirstUser = !await db.Users.AnyAsync()"]
CheckIsFirstUser -->|"first user"| CreateUser
CheckIsFirstUser -->|"not first"| RegistrationGate

RegistrationGate["Check RegistrationMode (open / invite / closed)"]
RegistrationGate -->|"closed"| FailRegistrationClosed
RegistrationGate -->|"invite"| TryInvite
RegistrationGate -->|"open"| CreateUser

FailRegistrationClosed["Return UserOperationResult.Fail(UserError.ValidationFailed, #quot;Registration is closed on this server.#quot;)"]

TryInvite["Call TryConsumeInviteAsync(db, inviteCode)"]
TryInvite -->|"invite error"| FailInviteError
TryInvite -->|"ok"| CreateUser

FailInviteError["Return UserOperationResult.Fail(UserError.ValidationFailed, inviteError)"]

CreateUser["Create new User instance (Id = Guid.NewGuid(), set fields)"]
```

```csharp
public class UserService : IUserService
```


Implements user-account operations for the server, most notably account registration. Use this concrete IUserService implementation when you need the server-backed behavior: configuration-driven registration modes (open / invite / closed), automatic owner bootstrap for the very first account, username/password validation, reserved-name checks, and password hashing before persisting users.

## Remarks
UserService is the server-side implementation of IUserService and is responsible for safe, policy-driven user creation. It reads the registration policy from IConfiguration (Server:Registration), uses an IServiceScopeFactory to create a scoped EchoHubDbContext per operation (so the service can be used from different DI lifetimes), and enforces validation rules from ValidationConstants. The very first account created on a fresh database is always promoted to ServerRole.Owner to allow bootstrapping an administration account. Invite consumption (when registration is in "invite" mode) is performed via an atomic/guarded update to avoid races when two registrations attempt to use the last invite simultaneously.

## Example
```csharp
// Typical usage from an async context where `userService` is resolved from DI
var result = await userService.RegisterUserAsync("alice", "s3cretP@ss", displayName: "Alice");
if (result.IsSuccess)
{
    var profile = result; // UserOperationResult.Success wraps the created UserProfileDto
    // proceed with signed-in flow
}
else
{
    // registration failed; map user-visible error to response
}
```

## Notes
- Username handling: the service normalizes usernames by trimming and lower-casing; a specific reserved name (UsersController.DeletedUserName) is rejected.
- The first user bypasses the registration gate and becomes ServerRole.Owner — this is intentional so a fresh server can be bootstrapped.
- Passwords are hashed using BCrypt.Net.BCrypt.HashPassword before being stored; there is no exposed mechanism here to change the hash algorithm.
- Invite consumption uses a guarded database update to prevent two concurrent registrations from both consuming the last available use of a code; if invite validation fails, RegisterUserAsync returns a validation failure with the invite error message.