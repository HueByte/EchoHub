# ModerationController

> **File:** `src/EchoHub.Server/Controllers/ModerationController.cs`  
> **Kind:** class

*Figure: How ModerationController works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
A["ModerationController POST role receives AssignRoleRequest"]
A --> B["Call GetCallerAsync(ServerRole.Admin)"]
B -->|"error != null"|C["Return ErrorResponse"]
B -->|"caller authorized"|D["If request.Role == ServerRole.Owner -> BadRequest(ErrorResponse)"]
D -->|"true"|C
D -->|"false"|E["Query EchoHubDbContext.Users for target Username (toLower)"]
E -->|"not found"|F["Return NotFound(ErrorResponse)"]
E -->|"found"|G["If target.Role == ServerRole.Owner -> BadRequest(ErrorResponse)"]
G -->|"true"|C
G -->|"false"|H["If request.Role >= caller.Role -> BadRequest(ErrorResponse)"]
H -->|"true"|C
H -->|"false"|I["Set previousRole and assign target.Role = request.Role"]
I --> J["Call EchoHubDbContext.SaveChangesAsync()"]
J --> K["Log information about role change"]
K --> L["Return Ok with success message"]

M["ModerationController POST kick/{username} receives KickRequest?"]
M --> N["Call GetCallerAsync(ServerRole.Mod)"]
N -->|"error != null"|C
N -->|"caller authorized"|O["Query EchoHubDbContext.Users for target Username"]
O -->|"not found"|F
O -->|"found"|P["If target.Role >= caller.Role -> BadRequest(ErrorResponse)"]
P -->|"true"|C
P -->|"false"|Q["channels = PresenceTracker.GetChannelsForUser(target.Username)"]
Q --> R["For each Channel in channels: broadcast kick via IChatBroadcaster and clean presence"]
R --> S["Proceed to perform broadcast and cleanup (truncated)"]
```

```csharp
[ApiController]
[Route("api/moderation")]
[Authorize]
[EnableRateLimiting("general")]
public class ModerationController : ControllerBase
```


Provides HTTP endpoints under `api/moderation` for server moderation operations such as assigning roles, kicking and banning users. Use `ModerationController` when you need a centralized, authenticated API surface to perform privileged user-management actions that update persistent state and notify connected clients.

## Remarks
`ModerationController` centralizes moderation workflows: it validates the caller's privileges (via the controller's caller-checking helpers), performs database updates through [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), emits real-time notifications to connected clients through [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) implementations, and updates runtime state via [`PresenceTracker`](../Services/PresenceTracker.cs.md) and [`ServerStatsCollector`](../Services/Stats/ServerStatsCollector.cs.md). The class is decorated with `[Authorize]` and `[EnableRateLimiting("general")]`, so all endpoints require an authenticated caller and are subject to the configured rate limits. Actions that modify user connectivity (for example kicking a user) will both broadcast the event to affected channels and invoke the controller's disconnect/cleanup logic to remove presence and force client disconnects.

## Notes
- User lookup uses a lowercased username (e.g. `username.ToLowerInvariant()`), so callers should supply the canonical username form; mismatched casing can lead to `NotFound` responses.  
- Role hierarchy is enforced: the controller prevents assigning the `ServerRole.Owner`, prevents changing the server owner's role, and disallows assigning or acting on users with roles equal to or higher than the caller (see the `AssignRole` and `KickUser` checks).  
- Persistent changes are saved via `EchoHubDbContext.SaveChangesAsync()` and important actions are logged with the injected `ILogger<ModerationController>`, so moderation operations are durable and auditable.  
- Because the controller broadcasts moderation events using [`IChatBroadcaster`](../../EchoHub.Core/Contracts/IChatBroadcaster.cs.md) and may call `ForceDisconnectAndCleanupAsync`, clients connected to channels may be forcibly disconnected as part of an action — callers should expect immediate real-time side effects beyond the HTTP response.  
- The `[EnableRateLimiting("general")]` attribute can cause requests to be throttled under high load; plan client-side retry/backoff for operator tooling that calls these endpoints.