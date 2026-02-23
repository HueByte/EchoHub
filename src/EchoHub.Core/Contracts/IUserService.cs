using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IUserService
{
    Task<UserOperationResult> RegisterUserAsync(string username, string password, string? displayName = null);
    Task<UserOperationResult> AuthenticateUserAsync(string username, string password);
    Task<UserProfileDto?> GetUserProfileAsync(string username);
    Task<UserProfileDto?> GetUserByIdAsync(Guid userId);
    Task<UserOperationResult> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? nicknameColor);
    Task<UserOperationResult> SetAvatarAsync(Guid userId, string asciiArt);
}
