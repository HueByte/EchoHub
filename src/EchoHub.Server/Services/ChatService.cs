using System.Text.Json;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Core.Security;
using EchoHub.Server.Data;
using EchoHub.Server.Services.ServerLogs;
using EchoHub.Server.Services.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public class ChatService : IChatService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceTracker _presenceTracker;
    private readonly IEnumerable<IChatBroadcaster> _broadcasters;
    private readonly LinkEmbedService _embedService;
    private readonly IMessageEncryptionService _encryption;
    private readonly IChannelService _channelService;
    private readonly FileStorageService _fileStorage;
    private readonly SpamGuard _spamGuard;
    private readonly ServerLogsService _serverLogs;
    private readonly ServerStatsCollector _statsCollector;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        IEnumerable<IChatBroadcaster> broadcasters,
        LinkEmbedService embedService,
        IMessageEncryptionService encryption,
        IChannelService channelService,
        FileStorageService fileStorage,
        SpamGuard spamGuard,
        ServerLogsService serverLogs,
        ServerStatsCollector statsCollector,
        ILogger<ChatService> logger)
    {
        _scopeFactory = scopeFactory;
        _presenceTracker = presenceTracker;
        _broadcasters = broadcasters;
        _embedService = embedService;
        _encryption = encryption;
        _channelService = channelService;
        _fileStorage = fileStorage;
        _spamGuard = spamGuard;
        _serverLogs = serverLogs;
        _statsCollector = statsCollector;
        _logger = logger;
    }

    public async Task UserConnectedAsync(string connectionId, Guid userId, string username)
    {
        _presenceTracker.UserConnected(connectionId, userId, username);
        _statsCollector.RecordConnection(_presenceTracker.GetOnlineUserCount());

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.Status = UserStatus.Online;
            await db.SaveChangesAsync();
        }

        // Debug-level: connect/disconnect churn is high-volume on a busy server. Aggregate
        // counts land in the periodic stats report instead.
        _logger.LogDebug("{User} connected (ConnectionId: {ConnectionId})", username, connectionId);
    }

    public async Task<string?> UserDisconnectedAsync(string connectionId)
    {
        var preDisconnectUsername = _presenceTracker.GetUsernameForConnection(connectionId);
        var channelsBeforeDisconnect = preDisconnectUsername is not null
            ? _presenceTracker.GetChannelsForUser(preDisconnectUsername)
            : [];

        var username = _presenceTracker.UserDisconnected(connectionId);
        _statsCollector.RecordDisconnection(_presenceTracker.GetOnlineUserCount());

        if (username is not null && !_presenceTracker.IsOnline(username))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is not null)
            {
                user.LastSeenAt = DateTimeOffset.UtcNow;
                user.Status = UserStatus.Invisible;
                await db.SaveChangesAsync();

                var presence = new UserPresenceDto(
                    username,
                    user.DisplayName,
                    user.NicknameColor,
                    UserStatus.Invisible,
                    user.StatusMessage,
                    user.Role);

                await BroadcastToAllAsync(b => b.SendUserStatusChangedAsync(channelsBeforeDisconnect, presence));
            }
        }

        _logger.LogDebug("{User} disconnected (ConnectionId: {ConnectionId})", username ?? "Unknown", connectionId);
        return username;
    }

    public async Task<(List<MessageDto> History, string? Error, bool PasswordRequired)> JoinChannelAsync(
        string connectionId, Guid userId, string username, string channelName, string? password = null)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        // Join throttle — stops channel-cycling spam from any protocol. Only *first-time*
        // joins (no membership yet) count: the client auto-joins every known channel on
        // connect/reconnect, and that burst must never trip the guard.
        if (_spamGuard.Enabled)
        {
            using var guardScope = _scopeFactory.CreateScope();
            var guardDb = guardScope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

            var isExistingMember = await guardDb.Channels
                .Where(c => c.Name == channelName)
                .AnyAsync(c => guardDb.ChannelMemberships.Any(m => m.ChannelId == c.Id && m.UserId == userId));

            if (!isExistingMember)
            {
                var joiner = await guardDb.Users.FindAsync(userId);
                var joinVerdict = _spamGuard.CheckJoin(userId, joiner?.Role ?? ServerRole.Member);
                if (joinVerdict.Kind != SpamVerdictKind.Allowed)
                    return ([], joinVerdict.Reason, false);
            }
        }

        // Delegate channel validation + membership (incl. password gate) to ChannelService
        var (success, error, passwordRequired) = await _channelService.EnsureChannelMembershipAsync(userId, channelName, password);
        if (!success)
            return ([], error, passwordRequired);

        var isNewJoin = _presenceTracker.JoinChannel(username, channelName);

        if (isNewJoin)
        {
            // Fetch presence data so clients can update their lists incrementally
            UserPresenceDto? presence = null;
            try
            {
                using var presenceScope = _scopeFactory.CreateScope();
                var presenceDb = presenceScope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
                var user = await presenceDb.Users.FindAsync(userId);
                if (user is not null)
                {
                    presence = new UserPresenceDto(
                        user.Username, user.DisplayName, user.NicknameColor,
                        user.Status, user.StatusMessage, user.Role,
                        _presenceTracker.IsIrcOnly(user.Username));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch presence for {User} on join", username);
            }

            // Don't broadcast join for invisible users — they still get history but stay hidden
            if (presence is null || presence.Status != UserStatus.Invisible)
            {
                await BroadcastToAllAsync(b => b.SendUserJoinedAsync(channelName, username, presence, connectionId));
            }

            _logger.LogDebug("{User} joined channel '{Channel}'", username, channelName);
        }

        var history = await GetChannelHistoryAsync(channelName, HubConstants.DefaultHistoryCount);
        return (history, null, false);
    }

    public async Task LeaveChannelAsync(string connectionId, string username, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();
        _presenceTracker.LeaveChannel(username, channelName);
        await BroadcastToAllAsync(b => b.SendUserLeftAsync(channelName, username));
        _logger.LogDebug("{User} left channel '{Channel}'", username, channelName);
    }

    public async Task<string?> SendMessageAsync(Guid userId, string username, string channelName, string content, string? originConnectionId = null, Guid? replyToMessageId = null)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return "Invalid channel name.";

        // The live log room is read-only for everyone, every role. Reject by name before any
        // work so a streamed log line can never provoke a DB write or another log event.
        if (_serverLogs.IsLogsChannel(channelName))
            return "This channel is read-only.";

        // Decrypt content (client sends encrypted; IRC sends plaintext — Decrypt handles both)
        var plaintext = _encryption.Decrypt(content);

        // Strip encryption prefix if a user typed it literally (prevents spoofing)
        while (plaintext.StartsWith("$ENC$"))
            plaintext = plaintext["$ENC$".Length..];

        if (string.IsNullOrWhiteSpace(plaintext))
            return "Message content cannot be empty.";

        if (plaintext.Length > HubConstants.MaxMessageLength)
            return $"Message exceeds maximum length of {HubConstants.MaxMessageLength} characters.";

        // Sanitize on plaintext: collapse excessive newlines
        plaintext = SanitizeNewlines(plaintext);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
            return $"Channel '{channelName}' does not exist.";

        if (channel.IsSystem)
            return "This channel is read-only.";

        var sender = await db.Users.FindAsync(userId);

        // Check mute status
        if (sender is not null && sender.IsMuted)
        {
            if (sender.MutedUntil.HasValue && sender.MutedUntil.Value <= DateTimeOffset.UtcNow)
            {
                sender.IsMuted = false;
                sender.MutedUntil = null;
                await db.SaveChangesAsync();
            }
            else
            {
                return "You are muted and cannot send messages.";
            }
        }

        // Spam guard — operates on the stored content (ciphertext for E2E rooms), never decrypts
        if (sender is not null)
        {
            var verdict = _spamGuard.CheckMessage(userId, sender.Role, plaintext);
            switch (verdict.Kind)
            {
                case SpamVerdictKind.AutoMute:
                    // Escalate through the normal timed-mute machinery: moderation endpoints
                    // list it and MuteExpirationService lifts it. Issued by the server itself.
                    sender.IsMuted = true;
                    sender.MutedUntil = DateTimeOffset.UtcNow.Add(verdict.MuteDuration);
                    await db.SaveChangesAsync();
                    _logger.LogWarning("Spam protection auto-muted {User} for {Minutes} minutes in '{Channel}'",
                        username, (int)verdict.MuteDuration.TotalMinutes, channelName);
                    return $"You have been automatically muted for {(int)verdict.MuteDuration.TotalMinutes} minutes (spam protection).";

                case SpamVerdictKind.Rejected:
                    return verdict.Reason;
            }
        }

        // Replies must target an existing message in the same channel
        Message? replyTarget = null;
        if (replyToMessageId is { } replyId)
        {
            replyTarget = await db.Messages.FirstOrDefaultAsync(m => m.Id == replyId && m.ChannelId == channel.Id);
            if (replyTarget is null)
                return "The message you're replying to no longer exists.";
        }

        // Attempt to fetch link embeds for URLs in the plaintext message
        List<EmbedDto>? embeds = null;
        try
        {
            embeds = await _embedService.TryGetEmbedsAsync(plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch link embeds for message in '{Channel}'", channelName);
        }

        // Store in DB — encrypted at rest if enabled, plaintext otherwise
        var embedJson = embeds is not null ? JsonSerializer.Serialize(embeds) : null;
        var dbContent = _encryption.EncryptDatabaseEnabled ? _encryption.Encrypt(plaintext) : plaintext;
        var dbEmbedJson = _encryption.EncryptDatabaseEnabled ? _encryption.EncryptNullable(embedJson) : embedJson;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = dbContent,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channel.Id,
            SenderUserId = userId,
            SenderUsername = username,
            EmbedJson = dbEmbedJson,
            ReplyToMessageId = replyTarget?.Id,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Broadcast encrypted for SignalR clients; IRC broadcaster gets plaintext
        var encryptedContent = _encryption.Encrypt(plaintext);
        var messageDto = new MessageDto(
            message.Id,
            encryptedContent,
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            message.SentAt,
            Embeds: embeds,
            SenderDisplayName: sender?.DisplayName,
            ReplyTo: replyTarget is null ? null : BuildReplyRef(replyTarget));

        await BroadcastToAllAsync(b => b.SendMessageToChannelAsync(channelName, messageDto, originConnectionId));

        _logger.LogDebug("{User} sent message in '{Channel}'", username, channelName);
        return null;
    }

    public async Task<List<MessageDto>> GetChannelHistoryAsync(string channelName, int count, int offset = 0)
    {
        channelName = channelName.ToLowerInvariant().Trim();
        count = Math.Clamp(count, 1, ValidationConstants.MaxHistoryCount);
        offset = Math.Max(offset, 0);

        // The log room has no DB messages — its backlog is the tail of the rolling log file.
        // Only the first page carries the backlog; older pages are empty (files are the archive).
        if (_serverLogs.IsLogsChannel(channelName))
            return offset > 0 ? [] : BuildLogBacklog(channelName);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        return await GetChannelHistoryInternalAsync(db, channelName, count, offset);
    }

    /// <summary>
    /// Turns the log-file backlog into transport-encrypted <see cref="MessageDto"/>s so clients
    /// render past log lines exactly like streamed ones. Never touches the database.
    /// </summary>
    private List<MessageDto> BuildLogBacklog(string channelName)
    {
        return _serverLogs.ReadBacklog()
            .Select(entry => new MessageDto(
                Guid.NewGuid(),
                _encryption.Encrypt(entry.Content),
                ServerLogsService.SenderName,
                null,
                channelName,
                entry.Timestamp))
            .ToList();
    }

    /// <summary>
    /// Builds the wire reference for a reply target. Plaintext snippets are truncated
    /// server-side; end-to-end room ciphertext must pass through whole (a truncated blob
    /// can't be decrypted), so the client truncates those after decrypting.
    /// </summary>
    private ReplyRefDto BuildReplyRef(Message target)
    {
        const int maxSnippetLength = 120;

        // Strip the at-rest layer; E2E room content stays $RC1$ ciphertext
        var plain = _encryption.Decrypt(target.Content);
        if (!RoomCrypto.IsRoomCiphertext(plain) && plain.Length > maxSnippetLength)
            plain = plain[..maxSnippetLength] + "…";

        return new ReplyRefDto(target.Id, target.SenderUsername, _encryption.Encrypt(plain));
    }

    public async Task<string?> UpdateStatusAsync(Guid userId, string username, UserStatus status, string? statusMessage)
    {
        // SignalR happily binds any int to the enum parameter — reject undefined values
        if (!Enum.IsDefined(status))
            return "Invalid status. Use online, away, dnd, or invisible.";

        if (statusMessage is not null && statusMessage.Length > ValidationConstants.MaxStatusMessageLength)
            return $"Status message must not exceed {ValidationConstants.MaxStatusMessageLength} characters.";

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return "User not found.";

        user.Status = status;
        user.StatusMessage = statusMessage?.Trim();
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var presence = new UserPresenceDto(
            user.Username,
            user.DisplayName,
            user.NicknameColor,
            status,
            statusMessage,
            user.Role,
            _presenceTracker.IsIrcOnly(user.Username));

        var channels = _presenceTracker.GetChannelsForUser(username);
        await BroadcastToAllAsync(b => b.SendUserStatusChangedAsync(channels, presence));

        return null;
    }

    public async Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();
        var onlineUsernames = _presenceTracker.GetOnlineUsersInChannel(channelName);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var users = await db.Users
            .Where(u => onlineUsernames.Contains(u.Username) && u.Status != UserStatus.Invisible)
            .ToListAsync();

        // IsIrcOnly comes from the in-memory tracker, so map outside the EF query
        return users.Select(u => new UserPresenceDto(
                u.Username,
                u.DisplayName,
                u.NicknameColor,
                u.Status,
                u.StatusMessage,
                u.Role,
                _presenceTracker.IsIrcOnly(u.Username)))
            .ToList();
    }

    public Task BroadcastMessageAsync(string channelName, MessageDto message)
        => BroadcastToAllAsync(b => b.SendMessageToChannelAsync(channelName, message));

    public Task BroadcastChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
        => BroadcastToAllAsync(b => b.SendChannelUpdatedAsync(channel, channelName));

    public Task BroadcastChannelDeletedAsync(string channelName)
        => BroadcastToAllAsync(b => b.SendChannelDeletedAsync(channelName));

    private async Task BroadcastToAllAsync(Func<IChatBroadcaster, Task> action)
    {
        foreach (var broadcaster in _broadcasters)
        {
            try
            {
                await action(broadcaster);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcaster {Type} failed", broadcaster.GetType().Name);
            }
        }
    }

    public Task<List<string>> GetChannelsForUserAsync(string username)
        => Task.FromResult(_presenceTracker.GetChannelsForUser(username));

    /// <summary>
    /// Collapse consecutive newlines and cap total line count to prevent newline spam.
    /// </summary>
    private static string SanitizeNewlines(string content)
    {
        // Normalize \r\n → \n
        content = content.Replace("\r\n", "\n").Replace('\r', '\n');

        // Collapse consecutive blank/whitespace-only lines into max 1 blank line
        var lines = content.Split('\n');
        var result = new List<string>(lines.Length);
        int consecutiveBlanks = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveBlanks++;
                if (consecutiveBlanks <= HubConstants.MaxConsecutiveNewlines)
                    result.Add(line);
            }
            else
            {
                consecutiveBlanks = 0;
                result.Add(line);
            }
        }

        // Cap total lines
        if (result.Count > HubConstants.MaxMessageNewlines)
            result = result.Take(HubConstants.MaxMessageNewlines).ToList();

        return string.Join('\n', result);
    }

    private async Task<List<MessageDto>> GetChannelHistoryInternalAsync(EchoHubDbContext db, string channelName, int count, int offset = 0)
    {
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
            return [];

        // Left join: tombstoned messages (deleted accounts, SenderUserId cleared) must
        // still appear in history — an inner join would silently drop them.
        var raw = await db.Messages
            .Where(m => m.ChannelId == channel.Id)
            .OrderByDescending(m => m.SentAt)
            .Skip(offset)
            .Take(count)
            .GroupJoin(db.Users,
                m => m.SenderUserId,
                u => u.Id,
                (m, users) => new { m, users })
            .SelectMany(x => x.users.DefaultIfEmpty(),
                (x, u) => new { x.m, NicknameColor = u != null ? u.NicknameColor : null, DisplayName = u != null ? u.DisplayName : null })
            .ToListAsync();

        raw.Reverse();

        // Reply targets referenced by this batch, for quote snippets
        var replyIds = raw
            .Where(x => x.m.ReplyToMessageId.HasValue)
            .Select(x => x.m.ReplyToMessageId!.Value)
            .Distinct()
            .ToList();
        var replyTargets = replyIds.Count > 0
            ? (await db.Messages.Where(m => replyIds.Contains(m.Id)).ToListAsync()).ToDictionary(m => m.Id)
            : new Dictionary<Guid, Message>();

        var messageIds = raw.Select(x => x.m.Id).ToList();
        var attachmentsByMessage = (await db.Attachments
                .Where(a => messageIds.Contains(a.MessageId))
                .ToListAsync())
            .GroupBy(a => a.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Attachment blobs can be pruned by retention while the message rows remain. Check what's
        // actually on disk (one scan) so we never render a dead download, and so we can drop
        // attachment-only messages whose files are all gone.
        var storedFileIds = attachmentsByMessage.Count > 0 ? _fileStorage.GetStoredFileIds() : [];

        var result = new List<MessageDto>(raw.Count);
        var deadMessageIds = new List<Guid>();

        foreach (var x in raw)
        {
            // Decrypt DB content (handles both encrypted and plaintext via prefix detection)
            var plaintext = _encryption.Decrypt(x.m.Content);

            List<AttachmentDto>? attachments = null;
            var hadAttachments = attachmentsByMessage.TryGetValue(x.m.Id, out var atts) && atts.Count > 0;
            if (hadAttachments)
            {
                // Keep only attachments whose underlying file still exists on disk.
                var live = atts!.Where(a => storedFileIds.Contains(FileIdFromUrl(a.Url))).ToList();

                // Attachment-only message whose files are all gone → prune it entirely.
                if (live.Count == 0 && string.IsNullOrEmpty(plaintext))
                {
                    deadMessageIds.Add(x.m.Id);
                    continue;
                }

                if (live.Count > 0)
                {
                    attachments = live.Select(a => new AttachmentDto(
                        a.Kind,
                        a.Url,
                        a.FileName,
                        a.FileSize,
                        // Preview re-encrypted for transport; client decrypts (and room-decrypts for E2E)
                        _encryption.EncryptNullable(_encryption.DecryptNullable(a.AsciiPreview)))).ToList();
                }
            }

            var embedJsonPlain = _encryption.DecryptNullable(x.m.EmbedJson);
            List<EmbedDto>? embeds = null;
            if (embedJsonPlain is not null)
            {
                try { embeds = JsonSerializer.Deserialize<List<EmbedDto>>(embedJsonPlain); }
                catch { /* ignore malformed JSON */ }
            }

            // Encrypt for transport — client decrypts
            result.Add(new MessageDto(
                x.m.Id,
                _encryption.Encrypt(plaintext),
                x.m.SenderUsername,
                x.NicknameColor,
                channelName,
                x.m.SentAt,
                attachments,
                embeds,
                x.DisplayName,
                x.m.ReplyToMessageId is { } rid && replyTargets.TryGetValue(rid, out var replyTarget)
                    ? BuildReplyRef(replyTarget)
                    : null));
        }

        // Lazily delete the pruned messages (+ their attachment rows) as they're encountered.
        if (deadMessageIds.Count > 0)
        {
            try
            {
                await db.Attachments.Where(a => deadMessageIds.Contains(a.MessageId)).ExecuteDeleteAsync();
                await db.Messages.Where(m => deadMessageIds.Contains(m.Id)).ExecuteDeleteAsync();
                _logger.LogInformation("Pruned {Count} attachment-only messages with missing files in '{Channel}'",
                    deadMessageIds.Count, channelName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to prune messages with missing attachments in '{Channel}'", channelName);
            }
        }

        return result;
    }

    /// <summary>Extracts the storage file id from an attachment URL (e.g. "/api/files/{id}").</summary>
    private static string FileIdFromUrl(string url) => url.Split('/')[^1];
}
