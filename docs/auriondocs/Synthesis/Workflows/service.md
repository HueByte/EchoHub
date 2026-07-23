# Adding a new service

> *Workflow template auto-derived from 8 existing exemplar(s).*

When you need to add a new application service or a long-running background task to EchoHub.Server, add a new type alongside the existing services and register it where services are composed. The examples in src/EchoHub.Server/Services show both ordinary services and BackgroundService-based hosted tasks; model a new instance on those concrete types and then wire it into the application startup.

## Reference implementation

```csharp
/// <summary>
/// Background service that periodically unmutes users whose timed mute has expired.
/// </summary>
public sealed class MuteExpirationService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MuteExpirationService> _logger;

    public MuteExpirationService(IServiceScopeFactory scopeFactory, ILogger<MuteExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UnmuteExpiredUsersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error checking mute expirations");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task UnmuteExpiredUsersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var now = DateTimeOffset.UtcNow;
        var expired = await db.Users
            .Where(u => u.IsMuted && u.MutedUntil.HasValue && u.MutedUntil.Value <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        foreach (var user in expired)
        {
            user.IsMuted = false;
            user.MutedUntil = null;
            _logger.LogInformation("Auto-unmuted user {Username} (timed mute expired)", user.Username);
        }

        await db.SaveChangesAsync(ct);
    }
}
```

## Where it lives

Create the new service type under src/EchoHub.Server/Services. The repository contains multiple service types in that folder whose type names end with "Service", for example ChannelService, ChatService, FileCleanupService, FileStorageService, LinkEmbedService, MessageEncryptionService, MuteExpirationService, and ServerDirectoryService.

## Wiring

Registration and composition of services was detected in src/EchoHub.Server/Program.cs. Add the new service's registration in that file alongside the existing service registrations; inspect src/EchoHub.Server/Program.cs and the exemplars to follow the same wiring approach used for other services.

## Existing examples

- [`ChannelService`](../../Code/src/EchoHub.Server/Services/ChannelService.cs.md)
- [`ChatService`](../../Code/src/EchoHub.Server/Services/ChatService.cs.md)
- [`FileCleanupService`](../../Code/src/EchoHub.Server/Services/FileCleanupService.cs.md)
- [`FileStorageService`](../../Code/src/EchoHub.Server/Services/FileStorageService.cs.md)
- [`LinkEmbedService`](../../Code/src/EchoHub.Server/Services/LinkEmbedService.cs.md)
- [`MessageEncryptionService`](../../Code/src/EchoHub.Server/Services/MessageEncryptionService.cs.md)
- [`MuteExpirationService`](../../Code/src/EchoHub.Server/Services/MuteExpirationService.cs.md)
- [`ServerDirectoryService`](../../Code/src/EchoHub.Server/Services/ServerDirectoryService.cs.md)

---
*Synthesised by AurionDocs on 2026-07-23 09:35:20 UTC*
