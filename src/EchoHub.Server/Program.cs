using System.Text;
using EchoHub.Core.Constants;
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

// ── Routing ───────────────────────────────────────────────────────────────────
app.MapControllers();
app.MapHub<ChatHub>(HubConstants.ChatHubPath);

app.Run();
