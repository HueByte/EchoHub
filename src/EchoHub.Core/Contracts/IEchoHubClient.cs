using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

/// <summary>
/// Methods the server can invoke on connected clients.
/// </summary>
public interface IEchoHubClient
{
    Task ReceiveMessage(MessageDto message);
    Task UserJoined(string channelName, string username);
    Task UserLeft(string channelName, string username);
    Task ChannelUpdated(ChannelDto channel);
    Task UserStatusChanged(UserPresenceDto presence);
    Task Error(string message);
}
