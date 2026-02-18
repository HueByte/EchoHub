using EchoHub.Api.Data;
using EchoHub.Api.Services;
using EchoHub.Core.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EchoHub.Api.Extensions;

public static class ServerDirectoryExtensions
{
    public static IServiceCollection AddServerDirectory(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ServerDirectoryDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ServerDirectoryService>();

        return services;
    }

    public static WebApplication MapServerDirectoryApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/servers");

        group.MapGet("/", async (ServerDirectoryService service) =>
        {
            var servers = await service.GetAllServersAsync();
            return Results.Ok(servers);
        });

        group.MapGet("/search", async (string q, ServerDirectoryService service) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Query parameter 'q' is required.");

            var servers = await service.SearchServersAsync(q);
            return Results.Ok(servers);
        });

        group.MapGet("/{id:guid}", async (Guid id, ServerDirectoryService service) =>
        {
            var server = await service.GetServerByIdAsync(id);
            return server is null ? Results.NotFound() : Results.Ok(server);
        });

        group.MapPost("/", async (RegisterServerRequest request, ServerDirectoryService service) =>
        {
            try
            {
                var server = await service.RegisterServerAsync(request);
                return Results.Created($"/api/servers/{server.Id}", server);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:guid}/status", async (Guid id, ServerStatusDto status, ServerDirectoryService service) =>
        {
            var server = await service.UpdateServerStatusAsync(id, status.OnlineUsers, status.TotalChannels);
            return server is null ? Results.NotFound() : Results.Ok(server);
        });

        return app;
    }
}
