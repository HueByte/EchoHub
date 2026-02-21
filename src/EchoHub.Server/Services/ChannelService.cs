using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public class ChannelService : IChannelService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PresenceTracker _presenceTracker;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IServiceScopeFactory scopeFactory,
        PresenceTracker presenceTracker,
        ILogger<ChannelService> logger)
    {
        _scopeFactory = scopeFactory;
        _presenceTracker = presenceTracker;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ChannelDto>> GetChannelsAsync(Guid userId, int offset, int limit)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        await EnsureDefaultChannelAsync(db);

        var query = db.Channels.Where(c =>
            c.IsPublic || db.ChannelMemberships.Any(m => m.ChannelId == c.Id && m.UserId == userId));
        var total = await query.CountAsync();

        var channels = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ChannelDto(
                c.Id, c.Name, c.Topic, c.IsPublic, c.Messages.Count, c.CreatedAt))
            .ToListAsync();

        return new PaginatedResponse<ChannelDto>(channels, total, offset, limit);
    }

    public async Task<ChannelOperationResult> CreateChannelAsync(
        Guid creatorUserId, string name, string? topic, bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Channel name is required.");

        var channelName = name.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                "Channel name must be 2-100 characters and contain only letters, digits, underscores, or hyphens.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        if (await db.Channels.AnyAsync(c => c.Name == channelName))
            return ChannelOperationResult.Fail(ChannelError.AlreadyExists, $"Channel '{channelName}' already exists.");

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = channelName,
            Topic = topic?.Trim(),
            IsPublic = isPublic,
            CreatedByUserId = creatorUserId,
        };

        db.Channels.Add(channel);

        // Creator automatically becomes a member
        db.ChannelMemberships.Add(new ChannelMembership
        {
            UserId = creatorUserId,
            ChannelId = channel.Id,
        });

        await db.SaveChangesAsync();

        var dto = new ChannelDto(channel.Id, channel.Name, channel.Topic, channel.IsPublic, 0, channel.CreatedAt);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<ChannelOperationResult> UpdateTopicAsync(
        Guid callerUserId, string channelName, string? topic)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (topic is not null && topic.Length > ValidationConstants.MaxChannelTopicLength)
            return ChannelOperationResult.Fail(ChannelError.ValidationFailed,
                $"Topic must not exceed {ValidationConstants.MaxChannelTopicLength} characters.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        if (dbChannel.CreatedByUserId != callerUserId)
            return ChannelOperationResult.Fail(ChannelError.Forbidden, "Only the channel creator can update the topic.");

        dbChannel.Topic = topic?.Trim();
        await db.SaveChangesAsync();

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == dbChannel.Id);
        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, messageCount, dbChannel.CreatedAt);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<ChannelOperationResult> DeleteChannelAsync(Guid callerUserId, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (channelName == HubConstants.DefaultChannel)
            return ChannelOperationResult.Fail(ChannelError.Protected,
                $"The '{HubConstants.DefaultChannel}' channel cannot be deleted.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (dbChannel is null)
            return ChannelOperationResult.Fail(ChannelError.NotFound, $"Channel '{channelName}' does not exist.");

        var caller = await db.Users.FindAsync(callerUserId);
        if (dbChannel.CreatedByUserId != callerUserId && (caller is null || caller.Role < ServerRole.Admin))
            return ChannelOperationResult.Fail(ChannelError.Forbidden,
                "Only the channel creator or an admin can delete the channel.");

        db.Channels.Remove(dbChannel);
        await db.SaveChangesAsync();

        var dto = new ChannelDto(dbChannel.Id, dbChannel.Name, dbChannel.Topic, dbChannel.IsPublic, 0, dbChannel.CreatedAt);
        return ChannelOperationResult.Success(dto);
    }

    public async Task<(string? Topic, bool Exists)> GetChannelTopicAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null) return (null, false);

        return (channel.Topic, true);
    }

    public async Task<List<ChannelListItem>> GetChannelListAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channels = await db.Channels.OrderBy(c => c.Name).ToListAsync();

        return channels.Select(c => new ChannelListItem(
            c.Name, c.Topic,
            _presenceTracker.GetOnlineUsersInChannel(c.Name).Count)).ToList();
    }

    public async Task<ChannelDto?> GetChannelByNameAsync(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var c = await db.Channels.FirstOrDefaultAsync(ch => ch.Name == channelName);
        if (c is null) return null;

        var messageCount = await db.Messages.CountAsync(m => m.ChannelId == c.Id);
        return new ChannelDto(c.Id, c.Name, c.Topic, c.IsPublic, messageCount, c.CreatedAt);
    }

    public async Task<(bool Success, string? Error)> EnsureChannelMembershipAsync(Guid userId, string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (!ValidationConstants.ChannelNameRegex().IsMatch(channelName))
            return (false, "Invalid channel name. Use 2-100 characters: letters, digits, underscores, or hyphens.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
        {
            // Auto-recreate #general if it was somehow removed
            if (channelName == HubConstants.DefaultChannel)
            {
                channel = new Channel
                {
                    Id = Guid.NewGuid(),
                    Name = HubConstants.DefaultChannel,
                    Topic = "General discussion",
                    CreatedByUserId = Guid.Empty,
                };
                db.Channels.Add(channel);
                await db.SaveChangesAsync();
                _logger.LogWarning("Default channel '{Channel}' was missing and has been recreated", HubConstants.DefaultChannel);
            }
            else
            {
                return (false, $"Channel '{channelName}' does not exist. Create it first via the channel list.");
            }
        }

        var hasMembership = await db.ChannelMemberships
            .AnyAsync(m => m.UserId == userId && m.ChannelId == channel.Id);
        if (!hasMembership)
        {
            db.ChannelMemberships.Add(new ChannelMembership
            {
                UserId = userId,
                ChannelId = channel.Id,
            });
            await db.SaveChangesAsync();
        }

        return (true, null);
    }

    private static async Task EnsureDefaultChannelAsync(EchoHubDbContext db)
    {
        if (!await db.Channels.AnyAsync(c => c.Name == HubConstants.DefaultChannel))
        {
            db.Channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                Name = HubConstants.DefaultChannel,
                Topic = "General discussion",
                CreatedByUserId = Guid.Empty,
            });
            await db.SaveChangesAsync();
        }
    }
}
