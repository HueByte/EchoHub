using EchoHub.Core.Models;

namespace EchoHub.Core.DTOs;

public record MessageDto(
    Guid Id,
    string Content,
    string SenderUsername,
    string? SenderNicknameColor,
    string ChannelName,
    DateTimeOffset SentAt,
    List<AttachmentDto>? Attachments = null,
    List<EmbedDto>? Embeds = null,
    string? SenderDisplayName = null);

/// <summary>
/// A file attached to a message. <see cref="AsciiPreview"/> holds the color-tag art for
/// images (null otherwise). For end-to-end encrypted channels the content behind
/// <see cref="Url"/> and the preview are ciphertext the server cannot read.
/// </summary>
public record AttachmentDto(
    AttachmentKind Kind,
    string Url,
    string FileName,
    long FileSize,
    string? AsciiPreview = null);

public record ChannelDto(
    Guid Id,
    string Name,
    string? Topic,
    bool IsPublic,
    int MessageCount,
    DateTimeOffset CreatedAt,
    bool IsProtected = false,
    bool IsEncrypted = false);

public record UserDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? NicknameColor,
    UserStatus Status,
    DateTimeOffset LastSeenAt);

public record SendMessageRequest(string ChannelName, string Content);

public record CreateChannelRequest(
    string Name,
    string? Topic = null,
    bool IsPublic = true,
    string? Password = null,
    string? EncryptionSalt = null,
    string? WrappedRoomKey = null);

/// <summary>
/// Public crypto metadata for a channel — enough for a client to derive its join
/// credential from a passphrase. Never includes the wrapped room key.
/// </summary>
public record ChannelCryptoDto(bool IsEncrypted, string? EncryptionSalt);

/// <summary>
/// Human-facing summary of a channel (the <c>/meta</c> command). For encrypted channels the
/// server still knows these figures — count, timestamps, and stored blob sizes — even though it
/// cannot read the content itself. <see cref="EstimatedSizeBytes"/> is the sum of stored
/// attachment blob sizes plus message text length, so it is an estimate, not an exact on-disk total.
/// </summary>
public record ChannelMetaDto(
    Guid Id,
    string Name,
    string? Topic,
    bool IsEncrypted,
    bool IsProtected,
    int MessageCount,
    int UniqueUserCount,
    long EstimatedSizeBytes,
    DateTimeOffset CreatedAt);

/// <summary>
/// Passphrase change for an encrypted channel: the client proves knowledge of the old
/// passphrase (old auth key), then supplies the re-wrapped room key under the new one.
/// </summary>
public record RekeyChannelRequest(
    string OldPassword,
    string NewPassword,
    string NewEncryptionSalt,
    string NewWrappedRoomKey);

public record UpdateTopicRequest(string? Topic);

public record SendUrlRequest(string Url);

public record JoinChannelResult(
    bool Success,
    List<MessageDto> History,
    string? Error = null,
    bool PasswordRequired = false,
    string? EncryptionSalt = null,
    string? WrappedRoomKey = null);

public record EmbedDto(
    string? SiteName,
    string? Title,
    string? Description,
    string? ImageAscii,
    string Url,
    string? ThemeColor = null);
