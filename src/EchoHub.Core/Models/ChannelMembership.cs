namespace EchoHub.Core.Models;

public class ChannelMembership
{
    public Guid UserId { get; set; }
    public Guid ChannelId { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
