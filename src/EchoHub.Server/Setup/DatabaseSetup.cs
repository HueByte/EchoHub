using EchoHub.Core.Constants;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Setup;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("EchoHub.Server.Setup.DatabaseSetup");

        await MigrateAsync(db, logger);
        await SeedDefaultChannelAsync(db, logger);

        // Run data migrations (e.g. ANSI → color tag format)
        await DataMigrationService.RunAsync(services);
    }

    private static async Task MigrateAsync(EchoHubDbContext db, ILogger logger)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
                await HandleLegacyDatabaseAsync(db, logger);

            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
            throw;
        }
    }

    private static async Task HandleLegacyDatabaseAsync(EchoHubDbContext db, ILogger logger)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        var hasMigrationTable = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

        if (!hasMigrationTable)
        {
            cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Users'";
            var hasLegacyTables = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

            if (hasLegacyTables)
            {
                var dbPath = conn.DataSource;
                await conn.CloseAsync();

                if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupPath = $"{dbPath}.legacy_{timestamp}";
                    File.Copy(dbPath, backupPath, overwrite: false);
                    logger.LogWarning("Legacy database backed up to '{BackupPath}'.", backupPath);
                }

                await db.Database.EnsureDeletedAsync();
                logger.LogWarning("Legacy database removed. A new database will be created with migration support.");
                return;
            }
        }

        await conn.CloseAsync();
    }

    private static async Task SeedDefaultChannelAsync(EchoHubDbContext db, ILogger logger)
    {
        if (await db.Channels.AnyAsync(c => c.Name == HubConstants.DefaultChannel))
            return;

        db.Channels.Add(new Channel
        {
            Id = Guid.NewGuid(),
            Name = HubConstants.DefaultChannel,
            Topic = "General discussion",
            CreatedByUserId = Guid.Empty,
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Default channel '{Channel}' created.", HubConstants.DefaultChannel);
    }
}
