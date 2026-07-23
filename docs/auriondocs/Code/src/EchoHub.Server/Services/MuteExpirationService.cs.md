# MuteExpirationService

> **File:** `src/EchoHub.Server/Services/MuteExpirationService.cs`  
> **Kind:** class

```csharp
public sealed class MuteExpirationService : BackgroundService
```


Periodic background task that checks for users with an active timed mute and lifts the mute once the expiration time has passed. Implemented as a `BackgroundService`, it creates a short-lived scope to obtain an [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md), queries `Users` for those where `IsMuted` is true and `MutedUntil` has a value that is in the past, clears `IsMuted` and `MutedUntil`, and saves the changes. It logs each auto-unmute and continues running until the host is canceled; the check runs every 15 seconds to balance timely unmute with database load.

## Remarks
This symbol centralizes the timed-mute expiration lifecycle, decoupling unmute logic from controllers or scheduled jobs. It ensures mutes expire even if no user action occurs and uses a scoped [`EchoHubDbContext`](../Data/EchoHubDbContext.cs.md) to avoid long-lived contexts and to work with fresh data on each cycle. Updates are batched per cycle, with a per-user log entry (e.g., "Auto-unmuted user ... (timed mute expired)") to aid observability and troubleshooting.

## Notes
- The polling interval is fixed by `CheckInterval` (15 seconds); lowering or raising this value trades immediacy against database load. Adjust with awareness of your project’s performance characteristics.
- Only mutes with a non-null `MutedUntil` are expired by this service. If a user’s `MutedUntil` is null, that mute will not be auto-expanded by this path and will require manual intervention or a different expiration rule.
