namespace EchoHub.Core.DTOs;

public record ServerInfoDto(
    Guid Id,
    string Name,
    string? Description,
    string Url,
    int OnlineUsers,
    int TotalChannels,
    bool IsOnline,
    DateTimeOffset LastPingAt);

public record RegisterServerRequest(string Name, string Url, string? Description = null);

public record ServerStatusDto(string Name, string? Description, int OnlineUsers, int TotalChannels);
