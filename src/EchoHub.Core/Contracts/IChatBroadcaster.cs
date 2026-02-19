using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IChatBroadcaster
{
    Task SendMessageToChannelAsync(string channelName, MessageDto message);
    Task SendUserJoinedAsync(string channelName, string username, string? excludeConnectionId = null);
    Task SendUserLeftAsync(string channelName, string username);
    Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null);
    Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence);
    Task SendUserKickedAsync(string channelName, string username, string? reason);
    Task SendUserBannedAsync(string username, string? reason);
    Task SendMessageDeletedAsync(string channelName, Guid messageId);
    Task SendChannelNukedAsync(string channelName);
    Task SendErrorAsync(string connectionId, string message);
    Task ForceDisconnectUserAsync(List<string> connectionIds, string reason);
}
