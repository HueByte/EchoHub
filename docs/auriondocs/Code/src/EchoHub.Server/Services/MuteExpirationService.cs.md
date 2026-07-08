# MuteExpirationService

> **File:** `src/EchoHub.Server/Services/MuteExpirationService.cs`  
> **Kind:** class

```csharp
public sealed class MuteExpirationService : BackgroundService
```


This background service periodically unmutes users whose timed mute has expired. It runs in the background, waking at a fixed interval to lift mutes automatically so moderation time-bound rules are enforced without manual intervention.

## Remarks
This abstraction centralizes the expiration logic for timed mutes, keeping moderation behavior consistent across the codebase. It relies on a scoped EchoHubDbContext to query and update user mute state and logs each auto-unmute for observability. The 15-second interval is a pragmatic balance between timely unmuting and minimizing database load; cancellation support ensures shutdown proceeds cleanly.

## Notes
- The 15-second CheckInterval means an expiry may trigger up to ~15 seconds after the MutedUntil timestamp. Adjust the interval if you require different latency guarantees.
- Ensure appropriate indexes on the Users table (e.g., on IsMuted and MutedUntil) to avoid full scans and improve performance.
- In multi-instance deployments, multiple nodes may attempt to unmute the same users concurrently; the operation is effectively idempotent, but consider coordination if you require strict single-writer semantics.