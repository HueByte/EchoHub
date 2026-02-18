namespace EchoHub.Core.Models;

public class Channel
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Topic { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public List<Message> Messages { get; set; } = [];
}
