namespace EchoHub.Core.DTOs;

public record ServerStatusDto(
    string Name,
    string? Description,
    int OnlineUsers,
    int TotalChannels,
    string RegistrationMode = "open");

public record EncryptionKeyResponse(string Key);
