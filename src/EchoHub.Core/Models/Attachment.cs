namespace EchoHub.Core.Models;

/// <summary>
/// A file attached to a message (image, audio, or any file). A message may carry
/// zero or more attachments alongside its text content (Discord-style).
/// </summary>
public class Attachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public AttachmentKind Kind { get; set; }

    /// <summary>Relative download URL, e.g. <c>/api/files/{fileId}</c>.</summary>
    public required string Url { get; set; }
    public required string FileName { get; set; }

    /// <summary>Stored blob size in bytes (ciphertext size for encrypted channels).</summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Rendered ASCII-art preview for images (color-tag format). Null for audio/files.
    /// Stored encrypted-at-rest when database encryption is enabled, and room-encrypted
    /// for end-to-end encrypted channels.
    /// </summary>
    public string? AsciiPreview { get; set; }
}
