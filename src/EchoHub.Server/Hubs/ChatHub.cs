using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Hubs;

[Authorize]
public class ChatHub(EchoHubDbContext db, ILogger<ChatHub> logger, PresenceTracker presenceTracker) : Hub<IEchoHubClient>
{
    private Guid CurrentUserId =>
        Guid.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("User ID claim not found."));

    private string CurrentUsername =>
        Context.User?.FindFirstValue("username")
            ?? throw new HubException("Username claim not found.");

    public override async Task OnConnectedAsync()
    {
        presenceTracker.UserConnected(Context.ConnectionId, CurrentUserId, CurrentUsername);

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is not null)
        {
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.Status = UserStatus.Online;
            await db.SaveChangesAsync();
        }

        await base.OnConnectedAsync();

        logger.LogInformation("{User} connected (ConnectionId: {ConnectionId})", CurrentUsername, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Capture channels before disconnecting, since UserDisconnected clears them
        // when the last connection for a user is removed.
        var preDisconnectUsername = Context.User?.FindFirstValue("username");
        var channelsBeforeDisconnect = preDisconnectUsername is not null
            ? presenceTracker.GetChannelsForUser(preDisconnectUsername)
            : [];

        var username = presenceTracker.UserDisconnected(Context.ConnectionId);

        if (username is not null && !presenceTracker.IsOnline(username))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is not null)
            {
                user.LastSeenAt = DateTimeOffset.UtcNow;
                user.Status = UserStatus.Invisible;
                await db.SaveChangesAsync();

                var presence = new UserPresenceDto(
                    username,
                    user.DisplayName,
                    user.NicknameColor,
                    UserStatus.Invisible,
                    user.StatusMessage);

                foreach (var channel in channelsBeforeDisconnect)
                {
                    await Clients.Group(channel).UserStatusChanged(presence);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);

        logger.LogInformation("{User} disconnected (ConnectionId: {ConnectionId})", username ?? "Unknown", Context.ConnectionId);
    }

    public async Task<List<MessageDto>> JoinChannel(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

        if (channel is null)
        {
            channel = new Channel
            {
                Id = Guid.NewGuid(),
                Name = channelName,
                CreatedByUserId = CurrentUserId,
            };

            db.Channels.Add(channel);
            await db.SaveChangesAsync();

            logger.LogInformation("Channel '{Channel}' created by {User}", channelName, CurrentUsername);
        }

        presenceTracker.JoinChannel(CurrentUsername, channelName);

        await Groups.AddToGroupAsync(Context.ConnectionId, channelName);
        await Clients.OthersInGroup(channelName).UserJoined(channelName, CurrentUsername);

        logger.LogInformation("{User} joined channel '{Channel}'", CurrentUsername, channelName);

        var history = await GetChannelHistory(channelName, HubConstants.DefaultHistoryCount);
        return history;
    }

    public async Task LeaveChannel(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        presenceTracker.LeaveChannel(CurrentUsername, channelName);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName);
        await Clients.OthersInGroup(channelName).UserLeft(channelName, CurrentUsername);

        logger.LogInformation("{User} left channel '{Channel}'", CurrentUsername, channelName);
    }

    public async Task SendMessage(string channelName, string content)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            await Clients.Caller.Error("Message content cannot be empty.");
            return;
        }

        if (content.Length > HubConstants.MaxMessageLength)
        {
            await Clients.Caller.Error($"Message exceeds maximum length of {HubConstants.MaxMessageLength} characters.");
            return;
        }

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

        if (channel is null)
        {
            await Clients.Caller.Error($"Channel '{channelName}' does not exist.");
            return;
        }

        var sender = await db.Users.FindAsync(CurrentUserId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            Type = MessageType.Text,
            SentAt = DateTimeOffset.UtcNow,
            ChannelId = channel.Id,
            SenderUserId = CurrentUserId,
            SenderUsername = CurrentUsername,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var messageDto = new MessageDto(
            message.Id,
            message.Content,
            message.SenderUsername,
            sender?.NicknameColor,
            channelName,
            MessageType.Text,
            null,
            null,
            message.SentAt);

        await Clients.Group(channelName).ReceiveMessage(messageDto);

        logger.LogDebug("{User} sent message in '{Channel}'", CurrentUsername, channelName);
    }

    public async Task<List<MessageDto>> GetChannelHistory(string channelName, int count = HubConstants.DefaultHistoryCount)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

        if (channel is null)
        {
            return [];
        }

        var messages = await db.Messages
            .Where(m => m.ChannelId == channel.Id)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .Join(db.Users,
                m => m.SenderUserId,
                u => u.Id,
                (m, u) => new MessageDto(
                    m.Id,
                    m.Content,
                    m.SenderUsername,
                    u.NicknameColor,
                    channelName,
                    m.Type,
                    m.AttachmentUrl,
                    m.AttachmentFileName,
                    m.SentAt))
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task UpdateStatus(UserStatus status, string? statusMessage)
    {
        var user = await db.Users.FindAsync(CurrentUserId);

        if (user is null)
        {
            await Clients.Caller.Error("User not found.");
            return;
        }

        user.Status = status;
        user.StatusMessage = statusMessage;
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var presence = new UserPresenceDto(
            user.Username,
            user.DisplayName,
            user.NicknameColor,
            status,
            statusMessage);

        var channels = presenceTracker.GetChannelsForUser(CurrentUsername);

        foreach (var channel in channels)
        {
            await Clients.Group(channel).UserStatusChanged(presence);
        }
    }

    public async Task<List<UserPresenceDto>> GetOnlineUsers(string channelName)
    {
        channelName = channelName.ToLowerInvariant().Trim();

        var onlineUsernames = presenceTracker.GetOnlineUsersInChannel(channelName);

        var users = await db.Users
            .Where(u => onlineUsernames.Contains(u.Username))
            .Select(u => new UserPresenceDto(
                u.Username,
                u.DisplayName,
                u.NicknameColor,
                u.Status,
                u.StatusMessage))
            .ToListAsync();

        return users;
    }
}
