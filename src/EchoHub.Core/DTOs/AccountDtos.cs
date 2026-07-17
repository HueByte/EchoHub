namespace EchoHub.Core.DTOs;

/// <summary>Password re-confirmation for destructive self-service account actions.</summary>
public record DeleteAccountRequest(string Password);

/// <summary>
/// Everything the server holds about a user, as stored. For end-to-end encrypted rooms the
/// message content in here is room ciphertext — the server cannot include plaintext it never had.
/// </summary>
public record UserDataExportDto(
    DateTimeOffset ExportedAt,
    string ServerName,
    UserProfileDto Profile,
    List<ExportedMessageDto> Messages,
    List<ExportedAttachmentDto> Attachments);

public record ExportedMessageDto(
    Guid Id,
    string ChannelName,
    DateTimeOffset SentAt,
    string Content,
    Guid? ReplyToMessageId);

public record ExportedAttachmentDto(
    string FileName,
    string Url,
    long FileSize,
    string Kind,
    string ChannelName,
    DateTimeOffset SentAt);
