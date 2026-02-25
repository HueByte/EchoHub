using EchoHub.Core.DTOs;
using EchoHub.Core.Models;

namespace EchoHub.Core.Contracts;

public interface IChatService
{
    // Connection lifecycle
    Task UserConnectedAsync(string connectionId, Guid userId, string username);
    Task<string?> UserDisconnectedAsync(string connectionId);

    // Channel operations
    Task<(List<MessageDto> History, string? Error)> JoinChannelAsync(string connectionId, Guid userId, string username, string channelName);
    Task LeaveChannelAsync(string connectionId, string username, string channelName);

    // Messaging
    Task<string?> SendMessageAsync(Guid userId, string username, string channelName, string content);
    Task<List<MessageDto>> GetChannelHistoryAsync(string channelName, int count, int offset = 0);

    // Presence
    Task<string?> UpdateStatusAsync(Guid userId, string username, UserStatus status, string? statusMessage);
    Task<List<UserPresenceDto>> GetOnlineUsersAsync(string channelName);

    // Broadcasting (used by controllers and IRC gateway)
    Task BroadcastMessageAsync(string channelName, MessageDto message);
    Task BroadcastChannelUpdatedAsync(ChannelDto channel, string? channelName = null);

    // Query operations (used by IRC gateway for WHOIS)
    Task<List<string>> GetChannelsForUserAsync(string username);
}
