using System.Text;
using System.Threading.RateLimiting;
using EchoHub.Core.Constants;
using Microsoft.AspNetCore.RateLimiting;
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

// ── Controllers + SignalR ───────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSignalR();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSingleton<ImageToAsciiService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddHttpClient("ImageDownload", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.MaxResponseContentBufferSize = 10 * 1024 * 1024; // 10 MB
});

// ── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("upload", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("general", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// ── CORS ─────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();

        if (allowedOrigins is { Length: > 0 })
            policy.WithOrigins(allowedOrigins);
        else
            policy.SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

// ── Database initialization ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EchoHubDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // If the DB was created by EnsureCreated (no __EFMigrationsHistory table),
        // back it up and recreate so MigrateAsync can manage the schema properly.
        if (await db.Database.CanConnectAsync())
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var hasMigrationTable = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

            if (!hasMigrationTable)
            {
                cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Users'";
                var hasLegacyTables = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

                if (hasLegacyTables)
                {
                    var dbPath = conn.DataSource;
                    await conn.CloseAsync();

                    // Back up the legacy DB file before deleting
                    if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var backupPath = $"{dbPath}.legacy_{timestamp}";
                        File.Copy(dbPath, backupPath, overwrite: false);
                        logger.LogWarning("Legacy database backed up to '{BackupPath}'.", backupPath);
                    }

                    await db.Database.EnsureDeletedAsync();
                    logger.LogWarning("Legacy database removed. A new database will be created with migration support.");
                }
                else
                {
                    await conn.CloseAsync();
                }
            }
            else
            {
                await conn.CloseAsync();
            }
        }

        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed.");
        throw;
    }

    // Seed the default channel if it doesn't exist
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
        logger.LogInformation("Default channel '{Channel}' created.", HubConstants.DefaultChannel);
    }
}

// ── Middleware ─────────────────────────────────────────────────────────────────
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Routing ───────────────────────────────────────────────────────────────────
app.MapControllers();
app.MapHub<ChatHub>(HubConstants.ChatHubPath);

app.Run();
