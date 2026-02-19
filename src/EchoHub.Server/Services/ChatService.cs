using System.Text;
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

        // Sanitize: convert emoji to text, collapse newlines
        content = ConvertEmoji(content);
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
    /// Replace emoji with text shortcodes. TUI terminals can't render wide chars reliably.
    /// </summary>
    private static string ConvertEmoji(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var rune in content.EnumerateRunes())
        {
            if (EmojiMap.TryGetValue(rune.Value, out var name))
                sb.Append(name);
            else if (rune.Value >= 0x1F000) // supplementary emoji planes
                sb.Append($"[?]");
            else if (rune.Value is 0x200D or 0xFE0F or 0xFE0E) // ZWJ, variation selectors
                { } // strip silently
            else
                sb.Append(rune.ToString());
        }
        return sb.ToString();
    }

    private static readonly Dictionary<int, string> EmojiMap = new()
    {
        [0x1F600] = ":grinning:", [0x1F601] = ":grin:", [0x1F602] = ":joy:",
        [0x1F603] = ":smiley:", [0x1F604] = ":smile:", [0x1F605] = ":sweat_smile:",
        [0x1F606] = ":laughing:", [0x1F607] = ":angel:", [0x1F608] = ":imp:",
        [0x1F609] = ":wink:", [0x1F60A] = ":blush:", [0x1F60B] = ":yum:",
        [0x1F60C] = ":relieved:", [0x1F60D] = ":heart_eyes:", [0x1F60E] = ":sunglasses:",
        [0x1F60F] = ":smirk:", [0x1F610] = ":neutral:", [0x1F611] = ":expressionless:",
        [0x1F612] = ":unamused:", [0x1F613] = ":sweat:", [0x1F614] = ":pensive:",
        [0x1F615] = ":confused:", [0x1F616] = ":confounded:", [0x1F617] = ":kiss:",
        [0x1F618] = ":kissing_heart:", [0x1F619] = ":kissing:", [0x1F61A] = ":kissing_closed_eyes:",
        [0x1F61B] = ":tongue:", [0x1F61C] = ":wink_tongue:", [0x1F61D] = ":squint_tongue:",
        [0x1F61E] = ":disappointed:", [0x1F61F] = ":worried:", [0x1F620] = ":angry:",
        [0x1F621] = ":rage:", [0x1F622] = ":cry:", [0x1F623] = ":persevere:",
        [0x1F624] = ":triumph:", [0x1F625] = ":disappointed_relieved:", [0x1F626] = ":frowning:",
        [0x1F627] = ":anguished:", [0x1F628] = ":fearful:", [0x1F629] = ":weary:",
        [0x1F62A] = ":sleepy:", [0x1F62B] = ":tired:", [0x1F62C] = ":grimacing:",
        [0x1F62D] = ":sob:", [0x1F62E] = ":open_mouth:", [0x1F62F] = ":hushed:",
        [0x1F630] = ":cold_sweat:", [0x1F631] = ":scream:", [0x1F632] = ":astonished:",
        [0x1F633] = ":flushed:", [0x1F634] = ":sleeping:", [0x1F635] = ":dizzy_face:",
        [0x1F636] = ":no_mouth:", [0x1F637] = ":mask:", [0x1F638] = ":smile_cat:",
        [0x1F642] = ":slight_smile:", [0x1F643] = ":upside_down:",
        [0x1F644] = ":roll_eyes:", [0x1F910] = ":zipper_mouth:",
        [0x1F911] = ":money_mouth:", [0x1F912] = ":thermometer_face:",
        [0x1F913] = ":nerd:", [0x1F914] = ":thinking:", [0x1F915] = ":head_bandage:",
        [0x1F920] = ":cowboy:", [0x1F921] = ":clown:", [0x1F923] = ":rofl:",
        [0x1F924] = ":drooling:", [0x1F925] = ":lying:",
        [0x1F970] = ":smiling_hearts:", [0x1F971] = ":yawning:",
        [0x1F972] = ":smiling_tear:", [0x1F973] = ":party:",
        [0x1F974] = ":woozy:", [0x1F975] = ":hot:", [0x1F976] = ":cold:",
        [0x1F978] = ":disguised:", [0x1F979] = ":holding_back_tears:",
        [0x1F97A] = ":pleading:", [0x1F92A] = ":zany:", [0x1F92B] = ":shushing:",
        [0x1F92C] = ":censored:", [0x1F92D] = ":hand_over_mouth:",
        [0x1F92E] = ":vomiting:", [0x1F92F] = ":exploding_head:",
        // Gestures
        [0x1F44D] = ":+1:", [0x1F44E] = ":-1:", [0x1F44F] = ":clap:",
        [0x1F44B] = ":wave:", [0x1F44C] = ":ok_hand:", [0x1F44A] = ":punch:",
        [0x1F4AA] = ":muscle:", [0x1F64F] = ":pray:", [0x1F91D] = ":handshake:",
        [0x1F90C] = ":pinched_fingers:", [0x1F918] = ":metal:", [0x1F919] = ":call_me:",
        // Hearts
        [0x2764] = "<3", [0x1F494] = "</3", [0x1F495] = "<3<3",
        [0x1F496] = ":sparkling_heart:", [0x1F497] = ":heartbeat:",
        [0x1F499] = ":blue_heart:", [0x1F49A] = ":green_heart:",
        [0x1F49B] = ":yellow_heart:", [0x1F49C] = ":purple_heart:",
        [0x1F5A4] = ":black_heart:", [0x1F90D] = ":white_heart:",
        // Common objects
        [0x1F525] = ":fire:", [0x1F4A9] = ":poop:", [0x1F480] = ":skull:",
        [0x1F4AF] = ":100:", [0x1F389] = ":tada:", [0x1F38A] = ":confetti:",
        [0x1F3B5] = ":music:", [0x1F3B6] = ":notes:", [0x1F4A4] = ":zzz:",
        [0x1F4A5] = ":boom:", [0x1F4A2] = ":anger:", [0x1F4AC] = ":speech:",
        [0x1F440] = ":eyes:", [0x1F648] = ":see_no_evil:",
        [0x1F649] = ":hear_no_evil:", [0x1F64A] = ":speak_no_evil:",
        // Misc BMP symbols commonly used as emoji
        [0x2728] = ":sparkles:", [0x2B50] = ":star:", [0x26A1] = ":zap:",
        [0x2705] = ":white_check:", [0x274C] = ":x:", [0x274E] = ":x:",
        [0x2049] = ":!?:", [0x203C] = ":!!:",
    };

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
