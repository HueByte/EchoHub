using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IChatBroadcaster
{
    /// <summary>
    /// Broadcast a chat message to a channel. <paramref name="excludeConnectionId"/> is the
    /// connection the message originated from (IRC convention: never echo a message back to
    /// the connection that sent it — its client already displayed it locally). Other
    /// connections of the same user (e.g. an IRC session alongside a TUI session) still
    /// receive the message.
    /// </summary>
    Task SendMessageToChannelAsync(string channelName, MessageDto message, string? excludeConnectionId = null);
    Task SendUserJoinedAsync(string channelName, string username, UserPresenceDto? presence, string? excludeConnectionId = null);
    Task SendUserLeftAsync(string channelName, string username);
    Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null);
    Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence);
    Task SendUserKickedAsync(string channelName, string username, string? reason);
    Task SendUserBannedAsync(string username, string? reason);
    Task SendMessageDeletedAsync(string channelName, Guid messageId);
    Task SendChannelDeletedAsync(string channelName);
    Task SendChannelNukedAsync(string channelName);
    Task SendErrorAsync(string connectionId, string message);
    Task ForceDisconnectUserAsync(List<string> connectionIds, string reason);
}
