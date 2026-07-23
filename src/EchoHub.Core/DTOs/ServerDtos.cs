namespace EchoHub.Core.DTOs;

public record ServerStatusDto(
    string Name,
    string? Description,
    int OnlineUsers,
    int TotalChannels,
    string RegistrationMode = "open",
    string Version = "0.0.0");

public record EncryptionKeyResponse(string Key);
