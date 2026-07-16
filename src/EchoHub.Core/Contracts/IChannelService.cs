using EchoHub.Core.DTOs;

namespace EchoHub.Core.Contracts;

public interface IChannelService
{
    // Channel CRUD
    Task<PaginatedResponse<ChannelDto>> GetChannelsAsync(Guid userId, int offset, int limit);
    Task<ChannelOperationResult> CreateChannelAsync(Guid creatorUserId, string name, string? topic, bool isPublic, string? password = null);
    Task<ChannelOperationResult> UpdateTopicAsync(Guid callerUserId, string channelName, string? topic);
    Task<ChannelOperationResult> SetChannelPasswordAsync(Guid callerUserId, string channelName, string? password);
    Task<ChannelOperationResult> DeleteChannelAsync(Guid callerUserId, string channelName);

    // Channel queries
    Task<(string? Topic, bool Exists)> GetChannelTopicAsync(string channelName);
    Task<List<ChannelListItem>> GetChannelListAsync();
    Task<ChannelDto?> GetChannelByNameAsync(string channelName);

    // Membership
    Task<(bool Success, string? Error, bool PasswordRequired)> EnsureChannelMembershipAsync(Guid userId, string channelName, string? password = null);
}

public record ChannelListItem(string Name, string? Topic, int OnlineCount, bool IsPublic = true, bool IsProtected = false);
