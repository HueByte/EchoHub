using EchoHub.Core.Constants;
using EchoHub.Core.Models;

namespace EchoHub.Server.Config;

/// <summary>
/// Admin-configurable upload limits, bound from the <c>Uploads</c> configuration section.
/// Sizes are expressed in megabytes in configuration; the <c>*Bytes</c> accessors convert them
/// for enforcement. Every default mirrors <see cref="HubConstants"/> so an absent or partial
/// <c>Uploads</c> section preserves the historical built-in limits.
/// </summary>
public sealed class UploadLimits
{
    public int MaxFileSizeMB { get; init; } = HubConstants.MaxFileSizeBytes / (1024 * 1024);
    public int MaxImageSizeMB { get; init; } = HubConstants.MaxImageSizeBytes / (1024 * 1024);
    public int MaxAudioSizeMB { get; init; } = HubConstants.MaxAudioFileSizeBytes / (1024 * 1024);
    public int MaxAvatarSizeMB { get; init; } = HubConstants.MaxAvatarSizeBytes / (1024 * 1024);
    public int MaxAttachmentsPerMessage { get; init; } = HubConstants.MaxAttachmentsPerMessage;

    public long MaxFileSizeBytes => (long)MaxFileSizeMB * 1024 * 1024;
    public long MaxImageSizeBytes => (long)MaxImageSizeMB * 1024 * 1024;
    public long MaxAudioSizeBytes => (long)MaxAudioSizeMB * 1024 * 1024;
    public long MaxAvatarSizeBytes => (long)MaxAvatarSizeMB * 1024 * 1024;

    /// <summary>Maximum accepted size for a single attachment of the given kind.</summary>
    public long MaxForKind(AttachmentKind kind) => kind switch
    {
        AttachmentKind.Image => MaxImageSizeBytes,
        AttachmentKind.Audio => MaxAudioSizeBytes,
        _ => MaxFileSizeBytes,
    };

    /// <summary>
    /// Absolute ceiling for one message request body (largest file × the attachment cap). Used to
    /// size the request-body and multipart limits so a configured increase actually takes effect.
    /// </summary>
    public long MaxRequestBodyBytes => MaxFileSizeBytes * MaxAttachmentsPerMessage;
}
