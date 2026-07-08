# UserService

> **File:** `src/EchoHub.Server/Services/UserService.cs`  
> **Kind:** class

*Figure: How UserService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
  Start["Start"]
  Op{"Choose operation Register or Authenticate"}

  Start --> Op
  Op --> Reg["RegisterUserAsync"]
  Op --> Auth["AuthenticateUserAsync"]

  %% Register flow
  Reg --> RegV{"Username and password provided"}
  RegV -- "no" --> ReturnValidation["return UserOperationResult.Fail(UserError.ValidationFailed)"]
  RegV -- "yes" --> UsernameRegex{"Username matches ValidationConstants UsernameRegex"}
  UsernameRegex -- "no" --> ReturnValidation
  UsernameRegex -- "yes" --> PwMin{"Password length >= 6"}
  PwMin -- "no" --> ReturnValidation
  PwMin -- "yes" --> PwMax{"Password length <= ValidationConstants MaxPasswordLength"}
  PwMax -- "no" --> ReturnValidation
  PwMax -- "yes" --> Normalize["normalizedUsername = username.ToLowerInvariant().Trim()"]
  Normalize --> DbScopeReg["Create scope and get EchoHubDbContext"]
  DbScopeReg --> Exists{"db.Users.Any(u => u.Username == normalizedUsername)?"}
  Exists -- "yes" --> ReturnExists["return UserOperationResult.Fail(UserError.AlreadyExists)"]
  Exists -- "no" --> IsFirst{"isFirstUser = !db.Users.Any()"}
  IsFirst -- "yes" --> RoleOwner["role = ServerRole.Owner"]
  IsFirst -- "no" --> RoleMember["role = ServerRole.Member"]
  RoleOwner --> CreateUser["Create User with Id Username PasswordHash DisplayName and Role"]
  RoleMember --> CreateUser
  CreateUser --> AddSave["db.Users.Add(user) and db.SaveChangesAsync()"]
  AddSave --> ReturnSuccessReg["return UserOperationResult.Success(UserProfileDto)"]

  %% Authenticate flow
  Auth --> AuthV{"Username and password provided"}
  AuthV -- "no" --> ReturnValidation
  AuthV -- "yes" --> NormalizeAuth["normalizedUsername = username.ToLowerInvariant().Trim()"]
  NormalizeAuth --> DbScopeAuth["Create scope and get EchoHubDbContext"]
  DbScopeAuth --> FindUser{"user = db.Users.FirstOrDefault(u => u.Username == normalizedUsername)"}
  FindUser --> Verify{"user exists and BCrypt.Verify(password, user.PasswordHash)?"}
  Verify -- "no" --> ReturnInvalidCred["return UserOperationResult.Fail(UserError.InvalidCredentials)"]
  Verify -- "yes" --> Banned{"user.IsBanned?"}
  Banned -- "yes" --> ReturnBanned["return UserOperationResult.Fail(UserError.Banned)"]
  Banned -- "no" --> UpdateLastSeen["user.LastSeenAt = DateTimeOffset.UtcNow and db.SaveChangesAsync()"]
  UpdateLastSeen --> ReturnSuccessAuth["return UserOperationResult.Success(UserProfileDto)"]
```

```csharp
public class UserService : IUserService
```


Handles user lifecycle operations: registration, authentication, and profile retrieval/updates, using a scoped EchoHubDbContext for each operation. Reach for this service when you need application-level user management (validation, normalization, password hashing, role assignment) rather than manipulating the database or hashing logic directly.

## Remarks
This class centralizes user-related policies so callers don't have to repeat validation, normalization, hashing, or role-assignment logic. It obtains an IServiceScope from the injected IServiceScopeFactory for every operation and resolves EchoHubDbContext from that scope — this keeps DbContext usage scoped to each method call and avoids lifetime mismatches when the service is registered with a longer lifetime. Passwords are hashed and verified with BCrypt, usernames are normalized (trimmed and lower-cased) and validated with ValidationConstants.UsernameRegex(), and the first user created is granted the Owner server role.

## Example
```csharp
// Assuming userService is an IUserService resolved from DI
var registerResult = await userService.RegisterUserAsync("alice", "P@ssw0rd", "Alice");
if (registerResult.IsSuccess)
{
    // registration succeeded
}
else
{
    // handle validation / duplicate / other failures
}

var authResult = await userService.AuthenticateUserAsync("alice", "P@ssw0rd");
if (authResult.IsSuccess)
{
    // authentication succeeded
}
else
{
    // handle invalid credentials or banned account
}
```

## Notes
- Usernames are normalized to lower-case and trimmed before checks and storage; uniqueness is enforced at the application level here, so consider also enforcing a unique index in the database to avoid race conditions.
- The first successfully registered user is assigned ServerRole.Owner; concurrent registrations may race — if that distinction matters, guard it at the database/transaction level.
- AuthenticateUserAsync updates the user's LastSeenAt and checks the IsBanned flag; password hashing/verification uses BCrypt under the hood.