using System.Security.Claims;
using System.Security.Cryptography;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

/// <summary>
/// Invite-code management for invite-gated registration (Admin+ only).
/// Codes live in this server's own database — there is no central service.
/// </summary>
[ApiController]
[Route("api/invites")]
[Authorize]
[EnableRateLimiting("general")]
public class InvitesController : ControllerBase
{
    private const int MaxActiveInvites = 200;

    private readonly EchoHubDbContext _db;
    private readonly ILogger<InvitesController> _logger;

    public InvitesController(EchoHubDbContext db, ILogger<InvitesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest request)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var maxUses = request.MaxUses ?? 1;
        if (maxUses is < 1 or > 1000)
            return BadRequest(new ErrorResponse("MaxUses must be between 1 and 1000."));

        if (request.ExpiresInHours is < 1 or > 24 * 365)
            return BadRequest(new ErrorResponse("ExpiresInHours must be between 1 and 8760."));

        if (await _db.InviteCodes.CountAsync(i => i.UseCount < i.MaxUses) >= MaxActiveInvites)
            return BadRequest(new ErrorResponse($"Too many active invites (max {MaxActiveInvites}). Revoke unused ones first."));

        var invite = new InviteCode
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            CreatedByUserId = caller!.Id,
            CreatedByUsername = caller.Username,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = request.ExpiresInHours is { } hours ? DateTimeOffset.UtcNow.AddHours(hours) : null,
            MaxUses = maxUses,
        };

        _db.InviteCodes.Add(invite);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Invite code created by {User} (uses: {MaxUses}, expires: {Expires})",
            caller.Username, invite.MaxUses, invite.ExpiresAt?.ToString("u") ?? "never");

        return Ok(ToDto(invite));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var (_, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var invites = await _db.InviteCodes
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Ok(invites.Select(ToDto).ToList());
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Revoke(string code)
    {
        var (caller, error) = await GetCallerAsync(ServerRole.Admin);
        if (error is not null) return error;

        var normalized = code.Trim().ToUpperInvariant();
        var invite = await _db.InviteCodes.FirstOrDefaultAsync(i => i.Code == normalized);
        if (invite is null)
            return NotFound(new ErrorResponse("Invite code not found."));

        _db.InviteCodes.Remove(invite);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Invite code revoked by {User}", caller!.Username);
        return Ok();
    }

    /// <summary>Unguessable, unambiguous code like "K7QM-3XPF" (no 0/O/1/I).</summary>
    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return $"{new string(chars[..4])}-{new string(chars[4..])}";
    }

    private static InviteDto ToDto(InviteCode i) =>
        new(i.Code, i.CreatedByUsername, i.CreatedAt, i.ExpiresAt, i.MaxUses, i.UseCount);

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
