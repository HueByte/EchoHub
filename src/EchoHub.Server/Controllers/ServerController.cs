using EchoHub.Core.DTOs;
using EchoHub.Server.Data;
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

    public ServerController(EchoHubDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetInfo()
    {
        var userCount = await _db.Users.CountAsync();
        var channelCount = await _db.Channels.CountAsync();

        var status = new ServerStatusDto(
            _config["Server:Name"] ?? "EchoHub Server",
            _config["Server:Description"],
            userCount,
            channelCount);

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
}
