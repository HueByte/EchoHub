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
    DateTimeOffset CreatedAt);

public record UserDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? NicknameColor,
    UserStatus Status,
    DateTimeOffset LastSeenAt);

public record SendMessageRequest(string ChannelName, string Content);

public record CreateChannelRequest(string Name, string? Topic = null, bool IsPublic = true);

public record UpdateTopicRequest(string? Topic);

public record SendUrlRequest(string Url);

public record EmbedDto(
    string? SiteName,
    string? Title,
    string? Description,
    string? ImageAscii,
    string Url);
