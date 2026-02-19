namespace EchoHub.Core.DTOs;

public record ServerStatusDto(string Name, string? Description, int OnlineUsers, int TotalChannels);
