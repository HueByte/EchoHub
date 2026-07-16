using System.Text.RegularExpressions;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Setup;

public static partial class DataMigrationService
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("EchoHub.Server.Setup.DataMigration");

        await EnsureDefaultChannelsPublicAsync(db, logger);
        await MigrateAnsiMessagesAsync(db, logger);
        await MigrateEmbedJsonToArrayAsync(db, logger);
        await MigrateLegacyAttachmentsAsync(db, logger);
        await EnsureConfiguredAdminsAsync(db, config, logger);
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

    /// <summary>
    /// Fold legacy single-attachment messages (which stored the file on the message row and,
    /// for images, the ASCII art in Content) into the new Attachments model. Idempotent:
    /// only migrates messages that still have a legacy AttachmentUrl and no Attachment rows.
    /// After migrating, Content becomes empty (the ASCII art moves to the attachment preview)
    /// and the legacy columns are nulled out.
    /// </summary>
    private static async Task MigrateLegacyAttachmentsAsync(EchoHubDbContext db, ILogger logger)
    {
        var legacy = await db.Messages
            .Where(m => m.AttachmentUrl != null && m.Attachments.Count == 0)
            .ToListAsync();

        if (legacy.Count == 0)
            return;

        logger.LogInformation("Migrating {Count} legacy single-attachment messages to the attachments model...", legacy.Count);

        foreach (var message in legacy)
        {
            var kind = message.Type switch
            {
                Core.Models.MessageType.Image => AttachmentKind.Image,
                Core.Models.MessageType.Audio => AttachmentKind.Audio,
                _ => AttachmentKind.File,
            };

            // For images the ASCII art lived in Content; for audio/file Content was just the
            // filename (now redundant with the attachment). Either way the caption becomes empty.
            var preview = kind == AttachmentKind.Image ? message.Content : null;

            db.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Kind = kind,
                Url = message.AttachmentUrl!,
                FileName = message.AttachmentFileName ?? "file",
                FileSize = message.AttachmentFileSize ?? 0,
                AsciiPreview = preview,
            });

            message.Content = string.Empty;
            message.AttachmentUrl = null;
            message.AttachmentFileName = null;
            message.AttachmentFileSize = null;
            message.Type = Core.Models.MessageType.Text;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Migrated {Count} legacy attachments.", legacy.Count);
    }

    /// <summary>
    /// Ensure usernames listed in Server:Admins config are at least Admin role.
    /// Acts as a safety net in case the first registered user didn't get Owner role.
    /// </summary>
    private static async Task EnsureConfiguredAdminsAsync(EchoHubDbContext db, IConfiguration config, ILogger logger)
    {
        var adminUsernames = config.GetSection("Server:Admins").Get<string[]>();
        if (adminUsernames is not { Length: > 0 })
            return;

        var promoted = 0;
        foreach (var username in adminUsernames)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
            {
                logger.LogWarning("Configured admin '{Username}' not found in database (not registered yet).", username);
                continue;
            }

            if (user.Role < ServerRole.Admin)
            {
                var oldRole = user.Role;
                user.Role = ServerRole.Admin;
                promoted++;
                logger.LogInformation("Promoted '{Username}' from {OldRole} to Admin (configured in Server:Admins).",
                    username, oldRole);
            }
        }

        if (promoted > 0)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Migrate old single-object EmbedJson ("{...}") to array format ("[{...}]").
    /// </summary>
    private static async Task MigrateEmbedJsonToArrayAsync(EchoHubDbContext db, ILogger logger)
    {
        var messages = await db.Messages
            .Where(m => m.EmbedJson != null)
            .ToListAsync();

        var toMigrate = messages
            .Where(m => m.EmbedJson!.TrimStart().StartsWith('{'))
            .ToList();

        if (toMigrate.Count == 0)
            return;

        logger.LogInformation("Found {Count} messages with legacy single-embed JSON. Migrating to array format...", toMigrate.Count);

        var modified = 0;
        foreach (var message in toMigrate)
        {
            try
            {
                var single = System.Text.Json.JsonSerializer.Deserialize<EmbedDto>(message.EmbedJson!);
                if (single is not null)
                {
                    message.EmbedJson = System.Text.Json.JsonSerializer.Serialize(new[] { single });
                    modified++;
                }
            }
            catch
            {
                // Skip malformed JSON
            }
        }

        if (modified > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Migrated {Count} embed records from single-object to array format.", modified);
        }
    }

}
