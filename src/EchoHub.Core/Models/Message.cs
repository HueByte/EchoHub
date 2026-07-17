namespace EchoHub.Core.Models;

public class Message
{
    public Guid Id { get; set; }

    /// <summary>The message text/caption. May be empty when the message only carries attachments.</summary>
    public required string Content { get; set; }

    public string? EmbedJson { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }

    public Guid SenderUserId { get; set; }
    public required string SenderUsername { get; set; }

    /// <summary>Message this one replies to, if any. The target may since have been deleted.</summary>
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>Files attached to this message. Empty for a plain text message.</summary>
    public List<Attachment> Attachments { get; set; } = [];

    // ── Legacy columns (pre-attachments model) ──────────────────────────────
    // Retained so the one-time startup data migration can fold old single-attachment
    // messages into Attachments. New code never writes these; they are nulled out
    // once migrated. Not exposed in DTOs. See DataMigrationService.MigrateLegacyAttachmentsAsync.
    public MessageType Type { get; set; } = MessageType.Text;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentFileSize { get; set; }
}
