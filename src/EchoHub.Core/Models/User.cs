namespace EchoHub.Core.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? NicknameColor { get; set; }
    public string? AvatarAscii { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Online;
    public string? StatusMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
