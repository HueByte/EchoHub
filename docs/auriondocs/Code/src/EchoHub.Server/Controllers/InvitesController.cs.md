# InvitesController

> **File:** `src/EchoHub.Server/Controllers/InvitesController.cs`  
> **Kind:** class

```csharp
[ApiController]
[Route("api/invites")]
[Authorize]
[EnableRateLimiting("general")]
public class InvitesController : ControllerBase
```


InvitesController provides admin-only endpoints to manage invite codes used for invite-gated registration. It stores and governs the lifecycle of codes within this server's own database, rather than delegating to a central service, and should be used whenever an administrator needs to issue, review, or revoke invites.

## Remarks
Centralizing invite data in this controller creates a self-contained, auditable lifecycle for invites without relying on external services. It enforces admin ownership of creation, supports expiration and usage limits, and records actions for traceability via logs. By separating persistence (InviteCodes) from presentation (DTOs) and API responses, the design keeps concerns well-scoped and maintainable in this deployment.

## Notes
- There is a potential race around the MaxActiveInvites check with concurrent create requests; consider transactional safeguards if concurrent admins can issue invites.
- ExpiresInHours is optional; omitting it yields non-expiring invites; the code only applies an expiration when a value is provided.
- Generated codes use a reserved alphabet that excludes ambiguous characters and follow the XXXX-YYYY pattern, aiding readability and reducing mis-typing.
