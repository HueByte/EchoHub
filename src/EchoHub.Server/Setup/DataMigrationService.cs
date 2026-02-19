using System.Text.RegularExpressions;
using EchoHub.Core.Constants;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Setup;

public static partial class DataMigrationService
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("EchoHub.Server.Setup.DataMigration");

        await EnsureDefaultChannelsPublicAsync(db, logger);
        await MigrateAnsiMessagesAsync(db, logger);
    }

    /// <summary>
    /// Ensure the #general channel (and any pre-existing channels from before the IsPublic column) are public.
    /// </summary>
    private static async Task EnsureDefaultChannelsPublicAsync(EchoHubDbContext db, ILogger logger)
    {
        var general = await db.Channels.FirstOrDefaultAsync(c => c.Name == HubConstants.DefaultChannel);
        if (general is not null && !general.IsPublic)
        {
            general.IsPublic = true;
            await db.SaveChangesAsync();
            logger.LogInformation("Marked #{Channel} as public.", HubConstants.DefaultChannel);
        }
    }

    private static async Task MigrateAnsiMessagesAsync(EchoHubDbContext db, ILogger logger)
    {
        // Load messages that contain the ESC byte (0x1B) — these have legacy ANSI color codes.
        // Filter by Image type first (only images have ANSI art), then check content in memory.
        var messages = await db.Messages
            .Where(m => m.Type == Core.Models.MessageType.Image)
            .ToListAsync();

        var toMigrate = messages.Where(m => m.Content.Contains('\x1b')).ToList();

        if (toMigrate.Count == 0)
            return;

        logger.LogInformation("Found {Count} messages with legacy ANSI color codes. Migrating to color tag format...", toMigrate.Count);

        var modified = 0;
        foreach (var message in toMigrate)
        {
            var converted = AnsiToColorTags(message.Content);
            if (converted != message.Content)
            {
                message.Content = converted;
                modified++;
            }
        }

        if (modified > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Migrated {Count} messages from ANSI escape codes to printable color tags.", modified);
        }
    }

    /// <summary>
    /// Convert ANSI escape codes to printable color tags.
    /// \x1b[38;2;R;G;Bm → {F:RRGGBB}, \x1b[48;2;R;G;Bm → {B:RRGGBB}, \x1b[0m → {X}
    /// </summary>
    public static string AnsiToColorTags(string text)
    {
        return AnsiColorRegex().Replace(text, match =>
        {
            if (match.Groups[1].Value == "0")
                return "{X}";

            if (match.Groups[2].Success)
            {
                var r = int.Parse(match.Groups[3].Value);
                var g = int.Parse(match.Groups[4].Value);
                var b = int.Parse(match.Groups[5].Value);
                var type = match.Groups[2].Value == "38;2" ? "F" : "B";
                return $"{{{type}:{r:X2}{g:X2}{b:X2}}}";
            }

            return match.Value;
        });
    }

    [GeneratedRegex(@"\x1b\[(?:(0)|(?:(38;2|48;2);(\d{1,3});(\d{1,3});(\d{1,3})))m")]
    private static partial Regex AnsiColorRegex();
}
