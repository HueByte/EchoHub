namespace EchoHub.Core.DTOs;

public record RegisterRequest(string Username, string Password, string? DisplayName = null, string? InviteCode = null);

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string? DisplayName,
    string? NicknameColor);

public record RefreshRequest(string RefreshToken);
