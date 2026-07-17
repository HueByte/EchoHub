using System.Security.Claims;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
{
    private readonly EchoHubDbContext _db;
    private readonly IConfiguration _config;
    private readonly DirectoryClaimStore _claimStore;

    public ServerController(EchoHubDbContext db, IConfiguration config, DirectoryClaimStore claimStore)
    {
        _db = db;
        _config = config;
        _claimStore = claimStore;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetInfo()
    {
        var userCount = await _db.Users.CountAsync();
        var channelCount = await _db.Channels.CountAsync();

        var registrationMode = (_config["Server:Registration"] ?? "open").Trim().ToLowerInvariant() switch
        {
            "invite" => "invite",
            "closed" => "closed",
            _ => "open",
        };

        var status = new ServerStatusDto(
            _config["Server:Name"] ?? "EchoHub Server",
            _config["Server:Description"],
            userCount,
            channelCount,
            registrationMode);

        return Ok(status);
    }

    [HttpGet("encryption-key")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public IActionResult GetEncryptionKey()
    {
        var key = _config["Encryption:Key"];

        if (string.IsNullOrEmpty(key))
            return StatusCode(503, new ErrorResponse("Encryption is not configured on this server."));

        return Ok(new EncryptionKeyResponse(key));
    }

    /// <summary>
    /// Operator-facing view of the EchoHubSpace directory registration: ServerId for admin
    /// support tickets, current registration state, and the last error/conflict if any.
    /// Never exposes the claim token itself.
    /// </summary>
    [HttpGet("directory")]
    [Authorize]
    public async Task<IActionResult> GetDirectoryStatus()
    {
        var (_, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var status = _claimStore.Status;
        var response = new
        {
            ServerId = _claimStore.ServerId,
            HasClaimToken = _claimStore.ClaimToken is not null,
            status.IsRegistered,
            status.LastRegisteredAt,
            status.LastError,
            status.ConflictingHosts,
        };

        return Ok(response);
    }

    private async Task<(User? Caller, IActionResult? Error)> GetCallerAsync(ServerRole minimumRole)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return (null, Unauthorized(new ErrorResponse("Authentication required.")));

        var caller = await _db.Users.FindAsync(Guid.Parse(userIdClaim));
        if (caller is null)
            return (null, Unauthorized(new ErrorResponse("User not found.")));

        if (caller.Role < minimumRole)
            return (null, StatusCode(403, new ErrorResponse($"Requires {minimumRole} role or higher.")));

        return (caller, null);
    }
}
