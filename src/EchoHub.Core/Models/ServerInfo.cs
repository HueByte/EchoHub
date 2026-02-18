namespace EchoHub.Core.Models;

public class ServerInfo
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
    public int OnlineUsers { get; set; }
    public int TotalChannels { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastPingAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsOnline { get; set; } = true;
}
