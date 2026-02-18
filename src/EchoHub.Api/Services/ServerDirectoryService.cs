using EchoHub.Api.Data;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Api.Services;

public class ServerDirectoryService(ServerDirectoryDbContext db)
{
    public async Task<List<ServerInfoDto>> GetAllServersAsync()
    {
        return await db.Servers
            .OrderByDescending(s => s.OnlineUsers)
            .Select(s => new ServerInfoDto(
                s.Id, s.Name, s.Description, s.Url,
                s.OnlineUsers, s.TotalChannels, s.IsOnline, s.LastPingAt))
            .ToListAsync();
    }

    public async Task<ServerInfoDto?> GetServerByIdAsync(Guid id)
    {
        var server = await db.Servers.FindAsync(id);
        return server is null ? null : MapToDto(server);
    }

    public async Task<ServerInfoDto> RegisterServerAsync(RegisterServerRequest request)
    {
        var exists = await db.Servers.AnyAsync(s => s.Url == request.Url);
        if (exists)
            throw new InvalidOperationException($"A server with URL '{request.Url}' is already registered.");

        var server = new ServerInfo
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Url = request.Url,
            OnlineUsers = 0,
            TotalChannels = 0,
            RegisteredAt = DateTimeOffset.UtcNow,
            LastPingAt = DateTimeOffset.UtcNow,
            IsOnline = true
        };

        db.Servers.Add(server);
        await db.SaveChangesAsync();

        return MapToDto(server);
    }

    public async Task<ServerInfoDto?> UpdateServerStatusAsync(Guid id, int onlineUsers, int totalChannels)
    {
        var server = await db.Servers.FindAsync(id);
        if (server is null)
            return null;

        server.OnlineUsers = onlineUsers;
        server.TotalChannels = totalChannels;
        server.LastPingAt = DateTimeOffset.UtcNow;
        server.IsOnline = true;

        await db.SaveChangesAsync();

        return MapToDto(server);
    }

    public async Task<List<ServerInfoDto>> SearchServersAsync(string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        return await db.Servers
            .Where(s => s.Name.ToLower().Contains(lowerQuery)
                     || (s.Description != null && s.Description.ToLower().Contains(lowerQuery)))
            .OrderByDescending(s => s.OnlineUsers)
            .Select(s => new ServerInfoDto(
                s.Id, s.Name, s.Description, s.Url,
                s.OnlineUsers, s.TotalChannels, s.IsOnline, s.LastPingAt))
            .ToListAsync();
    }

    private static ServerInfoDto MapToDto(ServerInfo server) => new(
        server.Id,
        server.Name,
        server.Description,
        server.Url,
        server.OnlineUsers,
        server.TotalChannels,
        server.IsOnline,
        server.LastPingAt);
}
