using System.Security.Claims;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EchoHub.Server.Hubs;

[Authorize]
public class ChatHub(IChatService chatService, ILogger<ChatHub> logger) : Hub<IEchoHubClient>
{
    private Guid CurrentUserId =>
        Guid.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("User ID claim not found."));

    private string CurrentUsername =>
        Context.User?.FindFirstValue("username")
            ?? throw new HubException("Username claim not found.");

    public override async Task OnConnectedAsync()
    {
        try
        {
            await chatService.UserConnectedAsync(Context.ConnectionId, CurrentUserId, CurrentUsername);
            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in OnConnectedAsync for {ConnectionId}", Context.ConnectionId);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await chatService.UserDisconnectedAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in OnDisconnectedAsync for {ConnectionId}", Context.ConnectionId);
            throw;
        }
    }

    public async Task<List<MessageDto>> JoinChannel(string channelName)
    {
        try
        {
            var (history, error) = await chatService.JoinChannelAsync(
                Context.ConnectionId, CurrentUserId, CurrentUsername, channelName);

            if (error is not null)
            {
                await Clients.Caller.Error(error);
                return [];
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channelName.ToLowerInvariant().Trim());
            return history;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining channel '{Channel}' for {User}", channelName, CurrentUsername);
            await Clients.Caller.Error($"Failed to join channel: {ex.Message}");
            return [];
        }
    }

    public async Task LeaveChannel(string channelName)
    {
        try
        {
            channelName = channelName.ToLowerInvariant().Trim();
            await chatService.LeaveChannelAsync(Context.ConnectionId, CurrentUsername, channelName);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error leaving channel '{Channel}' for {User}", channelName, CurrentUsername);
            await Clients.Caller.Error($"Failed to leave channel: {ex.Message}");
        }
    }

    public async Task SendMessage(string channelName, string content)
    {
        try
        {
            var error = await chatService.SendMessageAsync(CurrentUserId, CurrentUsername, channelName, content);
            if (error is not null)
                await Clients.Caller.Error(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message in '{Channel}' for {User}", channelName, CurrentUsername);
            await Clients.Caller.Error($"Failed to send message: {ex.Message}");
        }
    }

    public async Task<List<MessageDto>> GetChannelHistory(string channelName, int count = HubConstants.DefaultHistoryCount)
    {
        try
        {
            return await chatService.GetChannelHistoryAsync(channelName, count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching history for '{Channel}'", channelName);
            await Clients.Caller.Error($"Failed to load history: {ex.Message}");
            return [];
        }
    }

    public async Task UpdateStatus(UserStatus status, string? statusMessage)
    {
        try
        {
            var error = await chatService.UpdateStatusAsync(CurrentUserId, CurrentUsername, status, statusMessage);
            if (error is not null)
                await Clients.Caller.Error(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for {User}", CurrentUsername);
            await Clients.Caller.Error($"Failed to update status: {ex.Message}");
        }
    }

    public async Task<List<UserPresenceDto>> GetOnlineUsers(string channelName)
    {
        try
        {
            return await chatService.GetOnlineUsersAsync(channelName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing users in '{Channel}'", channelName);
            await Clients.Caller.Error($"Failed to list users: {ex.Message}");
            return [];
        }
    }
}
