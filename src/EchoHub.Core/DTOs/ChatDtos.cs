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
    DateTimeOffset SentAt);

public record ChannelDto(
    Guid Id,
    string Name,
    string? Topic,
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
