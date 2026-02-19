namespace EchoHub.Core.Models;

public class Message
{
    public Guid Id { get; set; }
    public required string Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? EmbedJson { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }

    public Guid SenderUserId { get; set; }
    public required string SenderUsername { get; set; }
}
