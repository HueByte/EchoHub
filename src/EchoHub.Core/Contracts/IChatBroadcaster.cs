using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IChatBroadcaster
{
    Task SendMessageToChannelAsync(string channelName, MessageDto message);
    Task SendUserJoinedAsync(string channelName, string username, string? excludeConnectionId = null);
    Task SendUserLeftAsync(string channelName, string username);
    Task SendChannelUpdatedAsync(ChannelDto channel, string? channelName = null);
    Task SendUserStatusChangedAsync(List<string> channelNames, UserPresenceDto presence);
    Task SendErrorAsync(string connectionId, string message);
}
