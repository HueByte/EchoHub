using System.Security.Claims;
using System.Text;
using EchoHub.Core.Constants;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Server.Auth;
using EchoHub.Server.Data;
using EchoHub.Server.Hubs;
using EchoHub.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── SQLite + EF Core ──────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=echohub.db";

builder.Services.AddDbContext<EchoHubDbContext>(options =>
    options.UseSqlite(connectionString));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EchoHub.Server";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EchoHub.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
    };

    // Allow SignalR clients to send the JWT via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(HubConstants.ChatHubPath))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
    };
});

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSingleton<ImageToAsciiService>();
builder.Services.AddSingleton<FileStorageService>();

// ── CORS (allow all for development) ──────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

// ── Database initialization ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
    await db.Database.EnsureCreatedAsync();

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

// ── Middleware ─────────────────────────────────────────────────────────────────
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Auth Endpoints ────────────────────────────────────────────────────────────
var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (RegisterRequest request, EchoHubDbContext db, JwtTokenService jwt) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { Error = "Username and password are required." });
    }

    if (request.Username.Length < 3 || request.Username.Length > 50)
    {
        return Results.BadRequest(new { Error = "Username must be between 3 and 50 characters." });
    }

    if (request.Password.Length < 6)
    {
        return Results.BadRequest(new { Error = "Password must be at least 6 characters." });
    }

    var normalizedUsername = request.Username.ToLowerInvariant().Trim();

    if (await db.Users.AnyAsync(u => u.Username == normalizedUsername))
    {
        return Results.Conflict(new { Error = "Username is already taken." });
    }

    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = normalizedUsername,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        DisplayName = request.DisplayName?.Trim(),
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = jwt.GenerateToken(user);

    return Results.Ok(new LoginResponse(token, user.Username, user.DisplayName, user.NicknameColor));
});

auth.MapPost("/login", async (LoginRequest request, EchoHubDbContext db, JwtTokenService jwt) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { Error = "Username and password are required." });
    }

    var normalizedUsername = request.Username.ToLowerInvariant().Trim();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    user.LastSeenAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();

    var token = jwt.GenerateToken(user);

    return Results.Ok(new LoginResponse(token, user.Username, user.DisplayName, user.NicknameColor));
});

// ── Channel Endpoints ─────────────────────────────────────────────────────────
app.MapGet("/api/channels", async (EchoHubDbContext db) =>
{
    var channels = await db.Channels
        .Select(c => new ChannelDto(
            c.Id,
            c.Name,
            c.Topic,
            c.Messages.Count,
            c.CreatedAt))
        .ToListAsync();

    return Results.Ok(channels);
})
.RequireAuthorization();

// ── Server Info Endpoint ──────────────────────────────────────────────────────
app.MapGet("/api/server/info", async (EchoHubDbContext db, IConfiguration config) =>
{
    var userCount = await db.Users.CountAsync();
    var channelCount = await db.Channels.CountAsync();

    var status = new ServerStatusDto(
        config["Server:Name"] ?? "EchoHub Server",
        config["Server:Description"],
        userCount,
        channelCount);

    return Results.Ok(status);
});

// ── User Profile Endpoints ────────────────────────────────────────────────────
app.MapGet("/api/users/{username}/profile", async (string username, EchoHubDbContext db) =>
{
    var normalizedUsername = username.ToLowerInvariant().Trim();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);

    if (user is null)
    {
        return Results.NotFound(new { Error = "User not found." });
    }

    var profile = new UserProfileDto(
        user.Id,
        user.Username,
        user.DisplayName,
        user.Bio,
        user.NicknameColor,
        user.AvatarAscii,
        user.Status,
        user.StatusMessage,
        user.CreatedAt,
        user.LastSeenAt);

    return Results.Ok(profile);
});

app.MapPut("/api/users/profile", async (UpdateProfileRequest request, EchoHubDbContext db, HttpContext ctx) =>
{
    var userIdClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdClaim is null)
        return Results.Unauthorized();

    var userId = Guid.Parse(userIdClaim);
    var user = await db.Users.FindAsync(userId);

    if (user is null)
        return Results.NotFound(new { Error = "User not found." });

    if (request.DisplayName is not null)
        user.DisplayName = request.DisplayName.Trim();

    if (request.Bio is not null)
        user.Bio = request.Bio.Trim();

    if (request.NicknameColor is not null)
        user.NicknameColor = request.NicknameColor.Trim();

    await db.SaveChangesAsync();

    var profile = new UserProfileDto(
        user.Id,
        user.Username,
        user.DisplayName,
        user.Bio,
        user.NicknameColor,
        user.AvatarAscii,
        user.Status,
        user.StatusMessage,
        user.CreatedAt,
        user.LastSeenAt);

    return Results.Ok(profile);
})
.RequireAuthorization();

app.MapPost("/api/users/avatar", async (HttpRequest req, EchoHubDbContext db, ImageToAsciiService asciiService, HttpContext ctx) =>
{
    var userIdClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdClaim is null)
        return Results.Unauthorized();

    var userId = Guid.Parse(userIdClaim);
    var user = await db.Users.FindAsync(userId);

    if (user is null)
        return Results.NotFound(new { Error = "User not found." });

    if (!req.HasFormContentType || req.Form.Files.Count == 0)
        return Results.BadRequest(new { Error = "No file uploaded." });

    var file = req.Form.Files[0];

    if (file.Length > HubConstants.MaxAvatarSizeBytes)
        return Results.BadRequest(new { Error = $"File size exceeds maximum of {HubConstants.MaxAvatarSizeBytes / (1024 * 1024)} MB." });

    using var stream = file.OpenReadStream();
    var asciiArt = asciiService.ConvertToAscii(stream);

    user.AvatarAscii = asciiArt;
    await db.SaveChangesAsync();

    return Results.Ok(new { AvatarAscii = asciiArt });
})
.RequireAuthorization();

// ── File Upload Endpoints ────────────────────────────────────────────────────
app.MapPost("/api/channels/{channel}/upload", async (string channel, HttpRequest req, EchoHubDbContext db, FileStorageService fileStorage, ImageToAsciiService asciiService, HttpContext ctx) =>
{
    var userIdClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var usernameClaim = ctx.User.FindFirstValue("username");
    if (userIdClaim is null || usernameClaim is null)
        return Results.Unauthorized();

    var userId = Guid.Parse(userIdClaim);
    var channelName = channel.ToLowerInvariant().Trim();

    var dbChannel = await db.Channels.FirstOrDefaultAsync(c => c.Name == channelName);
    if (dbChannel is null)
        return Results.NotFound(new { Error = $"Channel '{channelName}' does not exist." });

    if (!req.HasFormContentType || req.Form.Files.Count == 0)
        return Results.BadRequest(new { Error = "No file uploaded." });

    var file = req.Form.Files[0];

    if (file.Length > HubConstants.MaxFileSizeBytes)
        return Results.BadRequest(new { Error = $"File size exceeds maximum of {HubConstants.MaxFileSizeBytes / (1024 * 1024)} MB." });

    using var stream = file.OpenReadStream();
    var (fileId, filePath) = await fileStorage.SaveFileAsync(stream, file.FileName);

    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var isImage = imageExtensions.Contains(extension);

    var messageType = isImage ? MessageType.Image : MessageType.File;
    var content = isImage ? asciiService.ConvertToAscii(File.OpenRead(filePath)) : file.FileName;
    var attachmentUrl = $"/api/files/{fileId}";

    var sender = await db.Users.FindAsync(userId);

    var message = new Message
    {
        Id = Guid.NewGuid(),
        Content = content,
        Type = messageType,
        AttachmentUrl = attachmentUrl,
        AttachmentFileName = file.FileName,
        SentAt = DateTimeOffset.UtcNow,
        ChannelId = dbChannel.Id,
        SenderUserId = userId,
        SenderUsername = usernameClaim,
    };

    db.Messages.Add(message);
    await db.SaveChangesAsync();

    var messageDto = new MessageDto(
        message.Id,
        message.Content,
        message.SenderUsername,
        sender?.NicknameColor,
        channelName,
        messageType,
        attachmentUrl,
        file.FileName,
        message.SentAt);

    return Results.Ok(messageDto);
})
.RequireAuthorization();

app.MapGet("/api/files/{fileId}", (string fileId, FileStorageService fileStorage) =>
{
    var filePath = fileStorage.GetFilePath(fileId);

    if (filePath is null)
        return Results.NotFound(new { Error = "File not found." });

    var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };

    var fileName = Path.GetFileName(filePath);
    return Results.File(filePath, contentType, fileName);
});

// ── SignalR Hub ───────────────────────────────────────────────────────────────
app.MapHub<ChatHub>(HubConstants.ChatHubPath);

app.Run();
