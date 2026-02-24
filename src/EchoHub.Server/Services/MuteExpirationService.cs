using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Services;

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
