# MuteExpirationService

> **File:** `src/EchoHub.Server/Services/MuteExpirationService.cs`  
> **Kind:** class

```csharp
public sealed class MuteExpirationService : BackgroundService
```


Automatically unmutes users when their timed mute period has expired.

This is a background hosted service that periodically scans for users who are currently muted and whose MutedUntil timestamp has passed, then clears the mute state and logs the action. It uses a scoped DbContext instance per iteration (via IServiceScopeFactory) to perform a safe, isolated database update, and it runs on a fixed cadence (15 seconds) until the host is stopped. The service catches non-cancellation exceptions to avoid leaking the loop and continues monitoring uninterrupted.

## Remarks
The MuteExpirationService centralizes the expiry-based state transition for user mutes, decoupling this concern from user actions or other services. By resolving EchoHubDbContext within a scope for each cycle, it ensures proper disposal of the context and its resources while keeping the background loop lightweight. This pattern keeps mute state consistent across the system and reduces the chance of missed expirations if a user’s timed mute expires while the application is running.

## Notes
- The query materializes expired mutes with ToListAsync before processing; for environments with a very large backlog of expirations, consider batching to reduce memory usage.
- The cadence (CheckInterval) is 15 seconds; adjust if you need tighter or looser alignment with mute expiration semantics.
- Time comparisons use UTC (DateTimeOffset.UtcNow) to avoid timezone-related drift; ensure MutedUntil is stored as a UTC timestamp to preserve correctness.