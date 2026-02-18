using EchoHub.Core.Models;

namespace EchoHub.Core.DTOs;

public record UserProfileDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? NicknameColor,
    string? AvatarAscii,
    UserStatus Status,
    string? StatusMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt);

public record UpdateProfileRequest(
    string? DisplayName = null,
    string? Bio = null,
    string? NicknameColor = null);

public record UpdateStatusRequest(
    UserStatus Status,
    string? StatusMessage = null);

public record UserPresenceDto(
    string Username,
    string? DisplayName,
    string? NicknameColor,
    UserStatus Status,
    string? StatusMessage);
