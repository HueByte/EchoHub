# Adding a new service

> *Workflow template auto-derived from 8 existing exemplar(s).*

Adding a new service

When you need to encapsulate a piece of server functionality—either a long-lived background job or an application service consumed by controllers and other services—you add a new service type in this codebase. Use the existing service types in src/EchoHub.Server/Services as your models: pick a clear name that ends with "Service", place the source alongside the other services, and wire it up where services are registered.

## Reference implementation

Real code from `src/EchoHub.Server/Services/MuteExpirationService.cs` that a new instance can be modelled on:

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

Service source files are placed in src/EchoHub.Server/Services. Existing service types include names such as ChannelService, ChatService, FileCleanupService, FileStorageService, LinkEmbedService, MessageEncryptionService, MuteExpirationService, and ServerDirectoryService; each service file in that folder defines the corresponding type (for example, public class ChannelService : IChannelService and public sealed class FileCleanupService : BackgroundService). Follow the same placement and name your new type with a Service suffix so it sits alongside these exemplars.

## Wiring

Detected registration/composition site: src/EchoHub.Server/Program.cs. Inspect that file to see how services from src/EchoHub.Server/Services are registered and how hosted/background services are added to the application; new service types should be wired there consistent with the existing registrations.

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
*Synthesised by Aurion on 2026-07-23 05:55:34 UTC*
