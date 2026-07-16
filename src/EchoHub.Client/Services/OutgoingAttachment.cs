namespace EchoHub.Client.Services;

/// <summary>
/// One file to upload as part of a message. For end-to-end encrypted channels the stream
/// is already ciphertext, <see cref="DeclaredKind"/> is set (image/audio/file), and
/// <see cref="EncryptedPreview"/> holds the room-encrypted ASCII art for images.
/// For normal channels only <see cref="Stream"/> and <see cref="FileName"/> are set.
/// </summary>
public sealed record OutgoingAttachment(
    Stream Stream,
    string FileName,
    string? DeclaredKind = null,
    string? EncryptedPreview = null);
