using EchoHub.Core.Models;

namespace EchoHub.Core.DTOs;

public record MessageDto(
    Guid Id,
    string Content,
    string SenderUsername,
    string? SenderNicknameColor,
    string ChannelName,
    MessageType Type,
    string? AttachmentUrl,
    string? AttachmentFileName,
    DateTimeOffset SentAt,
    long? AttachmentFileSize = null,
    List<EmbedDto>? Embeds = null);

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
