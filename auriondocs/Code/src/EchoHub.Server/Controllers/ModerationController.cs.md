# ModerationController

> **File:** `src/EchoHub.Server/Controllers/ModerationController.cs`  
> **Kind:** class

*Figure: How ModerationController works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
Start["POST api/moderation/role - AssignRoleRequest"]
GetCaller["Call GetCallerAsync(ServerRole.Admin)"]
CallerError{"GetCaller returned error?"}
ReturnError["Return ErrorResponse and stop"]

CheckOwnerReq{"request.Role == ServerRole.Owner?"}
BadRequestOwner["Return BadRequest(ErrorResponse: Cannot assign the Owner role.)"]

FindTarget["Query EchoHubDbContext.Users for request.Username.ToLower()"]
TargetNotFound{"target is null?"}
ReturnNotFound["Return NotFound(ErrorResponse: user not found)"]

TargetIsOwner{"target.Role == ServerRole.Owner?"}
BadRequestOwner2["Return BadRequest(ErrorResponse: Cannot change the server owner role.)"]

RoleTooHigh{"request.Role >= caller.Role?"}
BadRequestRole["Return BadRequest(ErrorResponse: Cannot assign a role equal to or above your own.)"]

ApplyChange["Set previousRole, assign request.Role to target, call EchoHubDbContext.SaveChangesAsync()"]
ReturnOk["Return Ok(message: user is now role)"]

Start --> GetCaller
GetCaller --> CallerError
CallerError -->|Yes| ReturnError
CallerError -->|No| CheckOwnerReq

CheckOwnerReq -->|Yes| BadRequestOwner
CheckOwnerReq -->|No| FindTarget

FindTarget --> TargetNotFound
TargetNotFound -->|Yes| ReturnNotFound
TargetNotFound -->|No| TargetIsOwner

TargetIsOwner -->|Yes| BadRequestOwner2
TargetIsOwner -->|No| RoleTooHigh

RoleTooHigh -->|Yes| BadRequestRole
RoleTooHigh -->|No| ApplyChange

ApplyChange --> ReturnOk
```

```csharp
[ApiController]
[Route("api/moderation")]
[Authorize]
[EnableRateLimiting("general")]
public class ModerationController : ControllerBase
```


Exposes HTTP endpoints under api/moderation for server moderation operations such as assigning roles, kicking users, and banning users. Reach for this controller when implementing administrative or moderation features (web UI, automated moderation tools, or internal scripts) that must enforce role hierarchy, persist changes to the user store, notify connected clients, and clean up presence/connection state.

## Remarks
This controller centralizes server-side moderation logic and enforces policy at the API boundary: callers must be authenticated and possess the appropriate ServerRole before actions are performed. It coordinates several responsibilities through injected services — persisting role changes via the DbContext, enumerating and notifying affected channels via the PresenceTracker and IChatBroadcaster implementations, forcing connection teardown and cleanup, and recording moderation metrics with ServerStatsCollector. The design keeps authorization and business rules (for example, preventing Owner reassignment and preventing actors from assigning or acting on users with equal or higher roles) inside the controller so callers cannot bypass them.

## Notes
- Usernames are normalized (lowercased) before lookup; callers should supply usernames case-insensitively.
- Role comparisons rely on the numeric ordering of ServerRole (the controller rejects assigning or acting on roles that are equal to or higher than the caller).
- Methods have observable side effects: database updates, broadcasts to connected clients, forced disconnects and presence cleanup, and server-stat increments — consumers should treat these endpoints as state-changing and potentially long-running operations.
- The controller logs moderation actions (role changes, kicks, etc.); ensure logging and monitoring are configured appropriately for audit purposes.