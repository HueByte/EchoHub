using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public class ChatService : IChatService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceTracker _presenceTracker;
    private readonly IEnumerable<IChatBroadcaster> _broadcasters;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        IEnumerable<IChatBroadcaster> broadcasters,
        ILogger<ChatService> logger)
    {
        _scopeFactory = scopeFactory;
        _presenceTracker = presenceTracker;
        _broadcasters = broadcasters;
        _logger = logger;
    }

    public async Task UserConnectedAsync(string connectionId, Guid userId, string username)
    {
        _presenceTracker.UserConnected(connectionId, userId, username);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.Status = UserStatus.Online;
            await db.SaveChangesAsync();
        }

        _logger.LogInformation("{User} connected (ConnectionId: {ConnectionId})", username, connectionId);
    }

    public async Task<string?> UserDisconnectedAsync(string connectionId)
    {
        var preDisconnectUsername = _presenceTracker.GetUsernameForConnection(connectionId);
        var channelsBeforeDisconnect = preDisconnectUsername is not null
            ? _presenceTracker.GetChannelsForUser(preDisconnectUsername)
            : [];

        var username = _presenceTracker.UserDisconnected(connectionId);

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

        _logger.LogInformation("{User} disconnected (ConnectionId: {ConnectionId})", username ?? "Unknown", connectionId);
        return username;
    }

    public async Task<(List<MessageDto> History, string? Error)> JoinChannelAsync(
        string connectionId, Guid userId, string username, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return ([], "Invalid channel name. Use 2-100 characters: letters, digits, underscores, or hyphens.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
            return ([], $"Channel '{channelName}' does not exist. Create it first via the channel list.");

        // Persist membership so the channel shows in the user's channel list
        var hasMembership = await db.ChannelMemberships
            .AnyAsync(m => m.UserId == userId && m.ChannelId == channel.Id);
        if (!hasMembership)
        {
            db.ChannelMemberships.Add(new ChannelMembership
            {
                UserId = userId,
                ChannelId = channel.Id,
            });
            await db.SaveChangesAsync();
        }

        var isNewJoin = _presenceTracker.JoinChannel(username, channelName);

        if (isNewJoin)
        {
            await BroadcastToAllAsync(b => b.SendUserJoinedAsync(channelName, username, connectionId));
            _logger.LogInformation("{User} joined channel '{Channel}'", username, channelName);
        }

        var history = await GetChannelHistoryInternalAsync(db, channelName, HubConstants.DefaultHistoryCount);
        return (history, null);
    }

    public async Task LeaveChannelAsync(string connectionId, string username, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();
        _presenceTracker.LeaveChannel(username, channelName);
        await BroadcastToAllAsync(b => b.SendUserLeftAsync(channelName, username));
        _logger.LogInformation("{User} left channel '{Channel}'", username, channelName);
    }

    public async Task<string?> SendMessageAsync(Guid userId, string username, string channelName, string content)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return "Invalid channel name.";

        if (string.IsNullOrWhiteSpace(content))
            return "Message content cannot be empty.";

        if (content.Length > HubConstants.MaxMessageLength)
            return $"Message exceeds maximum length of {HubConstants.MaxMessageLength} characters.";

        // Sanitize: collapse excessive newlines
        content = SanitizeNewlines(content);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
            return $"Channel '{channelName}' does not exist.";

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

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            Type = MessageType.Text,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channel.Id,
            SenderUserId = userId,
            SenderUsername = username,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            message.Content,
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            MessageType.Text,
            null,
            null,
            message.SentAt);

        await BroadcastToAllAsync(b => b.SendMessageToChannelAsync(channelName, messageDto));

        _logger.LogDebug("{User} sent message in '{Channel}'", username, channelName);
        return null;
    }

    public async Task<List<MessageDto>> GetChannelHistoryAsync(string channelName, int count)
    {
        channelName = channelName.ToLowerInvariant().Trim();
        count = Math.Clamp(count, 1, ValidationConstants.MaxHistoryCount);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        return await GetChannelHistoryInternalAsync(db, channelName, count);
    }

    public async Task<string?> UpdateStatusAsync(Guid userId, string username, UserStatus status, string? statusMessage)
    {
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
            user.Role);

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

        return await db.Users
            .Where(u => onlineUsernames.Contains(u.Username))
            .Select(u => new UserPresenceDto(
                u.Username,
                u.DisplayName,
                u.NicknameColor,
                u.Status,
                u.StatusMessage,
                u.Role))
            .ToListAsync();
    }

    public Task BroadcastMessageAsync(string channelName, MessageDto message)
        => BroadcastToAllAsync(b => b.SendMessageToChannelAsync(channelName, message));

    public Task BroadcastChannelUpdatedAsync(ChannelDto channel, string? channelName = null)
        => BroadcastToAllAsync(b => b.SendChannelUpdatedAsync(channel, channelName));

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

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        username = username.ToLowerInvariant();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return null;

        return new UserProfileDto(
            user.Id, user.Username, user.DisplayName, user.Bio,
            user.NicknameColor, user.AvatarAscii, user.Status,
            user.StatusMessage, user.Role, user.CreatedAt, user.LastSeenAt);
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
            c.Name,
            c.Topic,
            _presenceTracker.GetOnlineUsersInChannel(c.Name).Count)).ToList();
    }

    public Task<List<string>> GetChannelsForUserAsync(string username)
        => Task.FromResult(_presenceTracker.GetChannelsForUser(username));

    public async Task<(Guid UserId, string Username)?> AuthenticateUserAsync(string username, string password)
    {
        username = username.ToLowerInvariant();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return (user.Id, user.Username);
    }

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

    private static async Task<List<MessageDto>> GetChannelHistoryInternalAsync(EchoHubDbContext db, string channelName, int count)
    {
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
            return [];

        var messages = await db.Messages
            .Where(m => m.ChannelId == channel.Id)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .Join(db.Users,
                m => m.SenderUserId,
                u => u.Id,
                (m, u) => new MessageDto(
                    m.Id,
                    m.Content,
                    m.SenderUsername,
                    u.NicknameColor,
                    channelName,
                    m.Type,
                    m.AttachmentUrl,
                    m.AttachmentFileName,
                    m.SentAt))
            .ToListAsync();

        messages.Reverse();
        return messages;
    }
}
