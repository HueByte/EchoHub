using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public class ChannelService : IChannelService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceTracker _presenceTracker;
    private readonly SpamGuard _spamGuard;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        SpamGuard spamGuard,
        ILogger<ChannelService> logger)
    {
        _scopeFactory = scopeFactory;
        _presenceTracker = presenceTracker;
        _spamGuard = spamGuard;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ChannelDto>> GetChannelsAsync(Guid userId, int offset, int limit)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        await EnsureDefaultChannelAsync(db);

        var query = db.Channels.Where(c =>
            c.IsPublic || db.ChannelMemberships.Any(m => m.ChannelId == c.Id && m.UserId == userId));
        var total = await query.CountAsync();

        var channels = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ChannelDto(
                c.Id, c.Name, c.Topic, c.IsPublic, c.Messages.Count, c.CreatedAt,
                c.PasswordHash != null, c.WrappedRoomKey != null))
            .ToListAsync();

        return new PaginatedResponse<ChannelDto>(channels, total, offset, limit);
    }

    public async Task<ChannelOperationResult> CreateChannelAsync(
        Guid creatorUserId, string name, string? topic, bool isPublic,
        string? password = null, string? encryptionSalt = null, string? wrappedRoomKey = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Channel name is required.");

        var channelName = name.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                "Channel name must be 2-100 characters and contain only letters, digits, underscores, or hyphens.");

        var passwordError = ValidateChannelPassword(ref password);
        if (passwordError is not null)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed, passwordError);

        // The E2E envelope (client-generated) only makes sense on password-gated channels
        var hasEnvelope = !string.IsNullOrWhiteSpace(encryptionSalt) && !string.IsNullOrWhiteSpace(wrappedRoomKey);
        if (hasEnvelope && password is null)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                "Encrypted channels require a password.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        // Channel-creation throttle (spam guard; Mods and above are exempt)
        if (_spamGuard.Enabled)
        {
            var creator = await db.Users.FindAsync(creatorUserId);
            var verdict = _spamGuard.CheckChannelCreate(creatorUserId, creator?.Role ?? ServerRole.Member);
            if (verdict.Kind != SpamVerdictKind.Allowed)
                return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                    verdict.Reason ?? "You're creating channels too fast.");
        }

        if (await db.Channels.AnyAsync(c => c.Name == channelName))
            return ChannelOperationResult.Fail(ChannelError.AlreadyExists, $"Channel '{channelName}' already exists.");

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = channelName,
            Topic = topic?.Trim(),
            IsPublic = isPublic,
            CreatedByUserId = creatorUserId,
            PasswordHash = password is not null ? BCrypt.Net.BCrypt.HashPassword(password) : null,
            EncryptionSalt = hasEnvelope ? encryptionSalt : null,
            WrappedRoomKey = hasEnvelope ? wrappedRoomKey : null,
        };

        db.Channels.Add(channel);

        // Creator automatically becomes a member
        db.ChannelMemberships.Add(new ChannelMembership
        {
            UserId = creatorUserId,
            ChannelId = channel.Id,
        });

        await db.SaveChangesAsync();

        var dto = new ChannelDto(channel.Id, channel.Name, channel.Topic, channel.IsPublic, 0, channel.CreatedAt,
            channel.PasswordHash != null, channel.WrappedRoomKey != null);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<ChannelOperationResult> UpdateTopicAsync(
        Guid callerUserId, string channelName, string? topic)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (topic is not null && topic.Length > ValidationConstants.MaxChannelTopicLength)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                $"Topic must not exceed {ValidationConstants.MaxChannelTopicLength} characters.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        if (dbChannel.CreatedByUserId != callerUserId)
            return ChannelOperationResult.Fail(ChannelError.Forbidden, "Only the channel creator can update the topic.");

        dbChannel.Topic = topic?.Trim();
        await db.SaveChangesAsync();

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id);
        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, messageCount, dbChannel.CreatedAt,
            dbChannel.PasswordHash != null, dbChannel.WrappedRoomKey != null);
        return ChannelOperationResult.Success(dto);
    }

    /// <summary>
    /// Sets, changes, or clears (null) a channel's join password. Creator or admin only.
    /// Not available on end-to-end encrypted channels — those change passphrase via
    /// <see cref="RekeyChannelAsync"/> so the room key envelope stays consistent.
    /// </summary>
    public async Task<ChannelOperationResult> SetChannelPasswordAsync(Guid callerUserId, string channelName, string? password)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        var passwordError = ValidateChannelPassword(ref password);
        if (passwordError is not null)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed, passwordError);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        if (dbChannel.WrappedRoomKey is not null)
            return ChannelOperationResult.Fail(ChannelError.Protected,
                "This channel is end-to-end encrypted — change its passphrase from the EchoHub client (/passwd).");

        var caller = await db.Users.FindAsync(callerUserId);
        if (dbChannel.CreatedByUserId != callerUserId && (caller is null || caller.Role < ServerRole.Admin))
            return ChannelOperationResult.Fail(ChannelError.Forbidden,
                "Only the channel creator or an admin can change the channel password.");

        dbChannel.PasswordHash = password is not null ? BCrypt.Net.BCrypt.HashPassword(password) : null;
        await db.SaveChangesAsync();

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id);
        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, messageCount, dbChannel.CreatedAt,
            dbChannel.PasswordHash != null, dbChannel.WrappedRoomKey != null);
        return ChannelOperationResult.Success(dto);
    }

    /// <summary>
    /// Changes an encrypted channel's passphrase by swapping the join-gate hash and the
    /// wrapped room key. The room content key itself never changes, so history stays
    /// readable — the client re-wraps it under the new passphrase-derived key.
    /// Creator only: admins cannot rekey a room whose passphrase they don't know.
    /// </summary>
    public async Task<ChannelOperationResult> RekeyChannelAsync(Guid callerUserId, string channelName,
        string oldPassword, string newPassword, string newEncryptionSalt, string newWrappedRoomKey)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        string? validatedNew = newPassword;
        var passwordError = ValidateChannelPassword(ref validatedNew);
        if (passwordError is not null)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed, passwordError);
        if (validatedNew is null || string.IsNullOrWhiteSpace(newEncryptionSalt) || string.IsNullOrWhiteSpace(newWrappedRoomKey))
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                "New password, salt, and wrapped room key are required.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        if (dbChannel.WrappedRoomKey is null || dbChannel.PasswordHash is null)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                "This channel is not end-to-end encrypted.");

        if (dbChannel.CreatedByUserId != callerUserId)
            return ChannelOperationResult.Fail(ChannelError.Forbidden,
                "Only the channel creator can change the passphrase.");

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, dbChannel.PasswordHash))
            return ChannelOperationResult.Fail(ChannelError.Forbidden, "The current passphrase is incorrect.");

        dbChannel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(validatedNew);
        dbChannel.EncryptionSalt = newEncryptionSalt;
        dbChannel.WrappedRoomKey = newWrappedRoomKey;
        await db.SaveChangesAsync();

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id);
        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, messageCount, dbChannel.CreatedAt,
            true, true);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<ChannelOperationResult> DeleteChannelAsync(Guid callerUserId, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (channelName == HubConstants.DefaultChannel)
            return ChannelOperationResult.Fail(ChannelError.Protected,
                $"The '{HubConstants.DefaultChannel}' channel cannot be deleted.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        var caller = await db.Users.FindAsync(callerUserId);
        if (dbChannel.CreatedByUserId != callerUserId && (caller is null || caller.Role < ServerRole.Admin))
            return ChannelOperationResult.Fail(ChannelError.Forbidden,
                "Only the channel creator or an admin can delete the channel.");

        db.Channels.Remove(dbChannel);
        await db.SaveChangesAsync();

        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, 0, dbChannel.CreatedAt,
            dbChannel.PasswordHash != null, dbChannel.WrappedRoomKey != null);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<(string? Topic, bool Exists)> GetChannelTopicAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null) return (null, false);

        return (channel.Topic, true);
    }

    public async Task<List<ChannelListItem>> GetChannelListAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channels = await db.Channels.OrderBy(c => c.Name).ToListAsync();

        return channels.Select(c => new ChannelListItem(
            c.Name, c.Topic,
            _presenceTracker.GetOnlineUsersInChannel(c.Name).Count,
            c.IsPublic, c.PasswordHash != null)).ToList();
    }

    public async Task<ChannelDto?> GetChannelByNameAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var c = await db.Channels.FirstOrDefaultAsync(ch => ch.Name == channelName);
        if (c is null) return null;

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == c.Id);
        return new ChannelDto(c.Id, c.Name, c.Topic, c.IsPublic, messageCount, c.CreatedAt,
            c.PasswordHash != null, c.WrappedRoomKey != null);
    }

    public async Task<ChannelMetaDto?> GetChannelMetaAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var c = await db.Channels.FirstOrDefaultAsync(ch => ch.Name == channelName);
        if (c is null) return null;

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == c.Id);

        // Distinct senders that have posted here. Works the same for encrypted channels —
        // sender identity is metadata the server keeps even when it can't read the messages.
        var uniqueUsers = await db.Messages
            .Where(m => m.ChannelId == c.Id)
            .Select(m => m.SenderUserId)
            .Distinct()
            .CountAsync();

        // Estimated footprint: stored attachment blob sizes + message text length. For encrypted
        // channels these are the ciphertext sizes, which is the server's real on-disk cost.
        var attachmentBytes = await db.Messages
            .Where(m => m.ChannelId == c.Id)
            .SelectMany(m => m.Attachments)
            .SumAsync(a => (long?)a.FileSize) ?? 0;
        var textBytes = await db.Messages
            .Where(m => m.ChannelId == c.Id)
            .SumAsync(m => (long?)m.Content.Length) ?? 0;

        return new ChannelMetaDto(
            c.Id, c.Name, c.Topic,
            c.WrappedRoomKey != null, c.PasswordHash != null,
            messageCount, uniqueUsers, attachmentBytes + textBytes, c.CreatedAt);
    }

    public async Task<ChannelCryptoDto?> GetChannelCryptoAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var c = await db.Channels.FirstOrDefaultAsync(ch => ch.Name == channelName);
        if (c is null) return null;

        return new ChannelCryptoDto(c.WrappedRoomKey != null, c.EncryptionSalt);
    }

    public async Task<(string? EncryptionSalt, string? WrappedRoomKey)> GetChannelKeyEnvelopeAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var c = await db.Channels.FirstOrDefaultAsync(ch => ch.Name == channelName);
        return (c?.EncryptionSalt, c?.WrappedRoomKey);
    }

    public async Task<(bool Success, string? Error, bool PasswordRequired)> EnsureChannelMembershipAsync(
        Guid userId, string channelName, string? password = null)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return (false, "Invalid channel name. Use 2-100 characters: letters, digits, underscores, or hyphens.", false);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
        {
            // Auto-recreate #general if it was somehow removed
            if (channelName == HubConstants.DefaultChannel)
            {
                channel = new Channel
                {
                    Id = Guid.NewGuid(),
                    Name = HubConstants.DefaultChannel,
                    Topic = "General discussion",
                    CreatedByUserId = Guid.Empty,
                };
                db.Channels.Add(channel);
                await db.SaveChangesAsync();
                _logger.LogWarning("Default channel '{Channel}' was missing and has been recreated", HubConstants.DefaultChannel);
            }
            else
            {
                return (false, $"Channel '{channelName}' does not exist. Create it first via the channel list.", false);
            }
        }

        var hasMembership = await db.ChannelMemberships
            .AnyAsync(m => m.UserId == userId && m.ChannelId == channel.Id);
        if (!hasMembership)
        {
            // Password gate: existing members (incl. the creator) joined before, so only
            // first-time joins of a protected channel need the password.
            if (channel.PasswordHash is not null)
            {
                if (string.IsNullOrEmpty(password))
                    return (false, $"Channel '{channelName}' is password protected.", true);

                if (!BCrypt.Net.BCrypt.Verify(password, channel.PasswordHash))
                    return (false, $"Incorrect password for channel '{channelName}'.", true);
            }

            db.ChannelMemberships.Add(new ChannelMembership
            {
                UserId = userId,
                ChannelId = channel.Id,
            });
            await db.SaveChangesAsync();
        }

        return (true, null, false);
    }

    /// <summary>
    /// Normalizes and validates a channel password. Whitespace-only becomes null (no password).
    /// Returns an error message, or null when valid.
    /// </summary>
    private static string? ValidateChannelPassword(ref string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            password = null;
            return null;
        }

        if (password.Length < ValidationConstants.MinChannelPasswordLength)
            return $"Channel password must be at least {ValidationConstants.MinChannelPasswordLength} characters.";

        if (password.Length > ValidationConstants.MaxPasswordLength)
            return $"Channel password must not exceed {ValidationConstants.MaxPasswordLength} characters.";

        return null;
    }

    private static async Task EnsureDefaultChannelAsync(EchoHubDbContext db)
    {
        if (!await db.Channels.AnyAsync(c => c.Name == HubConstants.DefaultChannel))
        {
            db.Channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                Name = HubConstants.DefaultChannel,
                Topic = "General discussion",
                CreatedByUserId = Guid.Empty,
            });
            await db.SaveChangesAsync();
        }
    }
}
