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
    Task UserKicked(string channelName, string username, string? reason);
    Task UserBanned(string username, string? reason);
    Task MessageDeleted(string channelName, Guid messageId);
    Task ChannelNuked(string channelName);
    Task Error(string message);
}
