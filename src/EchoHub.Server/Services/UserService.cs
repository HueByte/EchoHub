using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EchoHub.Server.Services;

public class UserService : IUserService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public UserService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    /// <summary>Registration mode from config: "open" (default), "invite", or "closed".</summary>
    public string RegistrationMode =>
        (_configuration["Server:Registration"] ?? "open").Trim().ToLowerInvariant() switch
        {
            "invite" => "invite",
            "closed" => "closed",
            _ => "open",
        };

    public async Task<UserOperationResult> RegisterUserAsync(string username, string password, string? displayName = null, string? inviteCode = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return UserOperationResult.Fail(UserError.ValidationFailed, "Username and password are required.");

        if (!ValidationConstants.UsernameRegex().IsMatch(username))
            return UserOperationResult.Fail(UserError.ValidationFailed,
                "Username must be 3-50 characters and contain only letters, digits, underscores, or hyphens.");

        if (password.Length < 6)
            return UserOperationResult.Fail(UserError.ValidationFailed, "Password must be at least 6 characters.");

        if (password.Length > ValidationConstants.MaxPasswordLength)
            return UserOperationResult.Fail(UserError.ValidationFailed,
                $"Password must not exceed {ValidationConstants.MaxPasswordLength} characters.");

        var normalizedUsername = username.ToLowerInvariant().Trim();

        // Reserved: deleted accounts' messages are re-attributed to this name
        if (normalizedUsername == Controllers.UsersController.DeletedUserName)
            return UserOperationResult.Fail(UserError.ValidationFailed, "This username is reserved.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        if (await db.Users.AnyAsync(u => u.Username == normalizedUsername))
            return UserOperationResult.Fail(UserError.AlreadyExists, "Username is already taken.");

        var isFirstUser = !await db.Users.AnyAsync();

        // Registration gate. The very first account (server owner bootstrap) is always allowed —
        // otherwise a fresh invite/closed server could never mint its first admin.
        if (!isFirstUser)
        {
            switch (RegistrationMode)
            {
                case "closed":
                    return UserOperationResult.Fail(UserError.ValidationFailed,
                        "Registration is closed on this server.");

                case "invite":
                    var inviteError = await TryConsumeInviteAsync(db, inviteCode);
                    if (inviteError is not null)
                        return UserOperationResult.Fail(UserError.ValidationFailed, inviteError);
                    break;
            }
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName?.Trim(),
            Role = isFirstUser ? ServerRole.Owner : ServerRole.Member,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return UserOperationResult.Success(ToProfileDto(user));
    }

    /// <summary>
    /// Validates and consumes one use of an invite code. Returns an error message, or null on
    /// success. The increment is a guarded UPDATE so two racing registrations can't both take
    /// a code's last use.
    /// </summary>
    private static async Task<string?> TryConsumeInviteAsync(EchoHubDbContext db, string? inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return "Registration is invite-only on this server. An invite code is required.";

        var code = inviteCode.Trim().ToUpperInvariant();
        var invite = await db.InviteCodes.FirstOrDefaultAsync(i => i.Code == code);

        if (invite is null || invite.UseCount >= invite.MaxUses)
            return "Invalid invite code.";

        if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            return "This invite code has expired.";

        var consumed = await db.InviteCodes
            .Where(i => i.Id == invite.Id && i.UseCount < i.MaxUses)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UseCount, i => i.UseCount + 1));

        return consumed == 1 ? null : "Invalid invite code.";
    }

    public async Task<UserOperationResult> AuthenticateUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return UserOperationResult.Fail(UserError.ValidationFailed, "Username and password are required.");

        var normalizedUsername = username.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return UserOperationResult.Fail(UserError.InvalidCredentials, "Invalid username or password.");

        if (user.IsBanned)
            return UserOperationResult.Fail(UserError.Banned, "Your account has been banned.");

        user.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return UserOperationResult.Success(ToProfileDto(user));
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        var normalizedUsername = username.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);
        return user is null ? null : ToProfileDto(user);
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        return user is null ? null : ToProfileDto(user);
    }

    public async Task<UserOperationResult> UpdateProfileAsync(
        Guid userId, string? displayName, string? bio, string? nicknameColor)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return UserOperationResult.Fail(UserError.NotFound, "User not found.");

        if (displayName is not null)
        {
            if (displayName.Length > ValidationConstants.MaxDisplayNameLength)
                return UserOperationResult.Fail(UserError.ValidationFailed,
                    $"Display name must not exceed {ValidationConstants.MaxDisplayNameLength} characters.");
            user.DisplayName = displayName.Trim();
        }

        if (bio is not null)
        {
            if (bio.Length > ValidationConstants.MaxBioLength)
                return UserOperationResult.Fail(UserError.ValidationFailed,
                    $"Bio must not exceed {ValidationConstants.MaxBioLength} characters.");
            user.Bio = bio.Trim();
        }

        if (nicknameColor is not null)
        {
            var color = nicknameColor.Trim();
            if (color.Length > 0 && !ValidationConstants.HexColorRegex().IsMatch(color))
                return UserOperationResult.Fail(UserError.ValidationFailed,
                    "Nickname color must be a valid hex color (e.g. #FF5500).");
            user.NicknameColor = color.Length > 0 ? color : null;
        }

        await db.SaveChangesAsync();

        return UserOperationResult.Success(ToProfileDto(user));
    }

    public async Task<UserOperationResult> SetAvatarAsync(Guid userId, string asciiArt)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return UserOperationResult.Fail(UserError.NotFound, "User not found.");

        user.AvatarAscii = asciiArt;
        await db.SaveChangesAsync();

        return UserOperationResult.Success(ToProfileDto(user));
    }

    private static UserProfileDto ToProfileDto(User user) => new(
        user.Id,
        user.Username,
        user.DisplayName,
        user.Bio,
        user.NicknameColor,
        user.AvatarAscii,
        user.Status,
        user.StatusMessage,
        user.Role,
        user.CreatedAt,
        user.LastSeenAt);
}
