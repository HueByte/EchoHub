using EchoHub.Core.DTOs;
using EchoHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EchoHub.Server.Controllers;

[ApiController]
[Route("api/server")]
public class ServerController(EchoHubDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet("info")]
    public async Task<IActionResult> GetInfo()
    {
        var userCount = await db.Users.CountAsync();
        var channelCount = await db.Channels.CountAsync();

        var status = new ServerStatusDto(
            config["Server:Name"] ?? "EchoHub Server",
            config["Server:Description"],
            userCount,
            channelCount);

        return Ok(status);
    }
}
