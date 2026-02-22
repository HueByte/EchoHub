using EchoHub.Core.Models;

namespace EchoHub.Client.Services;

/// <summary>
/// Holds the current user's session state (username, status, status message).
/// </summary>
internal sealed class UserSession
{
    public string Username { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Online;
    public string? StatusMessage { get; set; }

    public void Reset()
    {
        Username = string.Empty;
        Status = UserStatus.Online;
        StatusMessage = null;
    }
}
