using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IUserService
{
    // inviteCode is required when the server runs with Server:Registration = "invite";
    // "closed" refuses all new accounts. Both REST and the IRC gateway funnel through here.
    Task<UserOperationResult> RegisterUserAsync(string username, string password, string? displayName = null, string? inviteCode = null);
    Task<UserOperationResult> AuthenticateUserAsync(string username, string password);
    Task<UserProfileDto?> GetUserProfileAsync(string username);
    Task<UserProfileDto?> GetUserByIdAsync(Guid userId);
    Task<UserOperationResult> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? nicknameColor);
    Task<UserOperationResult> SetAvatarAsync(Guid userId, string asciiArt);
}
