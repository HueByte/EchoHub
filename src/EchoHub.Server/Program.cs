using System.Text;
using System.Threading.RateLimiting;
using EchoHub.Core.Constants;
using EchoHub.Core.Contracts;
using EchoHub.Core.Models;
using EchoHub.Server.Auth;
using EchoHub.Server.Data;
using EchoHub.Server.Hubs;
using EchoHub.Server.Irc;
using EchoHub.Server.Services;
using EchoHub.Server.Setup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// ── First-run setup (once) ──────────────────────────────────────────────────
FirstRunSetup.EnsureAppSettings();

// ── Bootstrap logger (replaced by full Serilog once host starts) ────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// ── Auto-restart loop ───────────────────────────────────────────────────────
const int maxConsecutiveFailures = 5;
var consecutiveFailures = 0;

while (true)
{
    var startTime = DateTimeOffset.UtcNow;

    try
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Host options ────────────────────────────────────────────────────
        builder.Services.Configure<HostOptions>(options =>
            options.ShutdownTimeout = TimeSpan.FromSeconds(5));

        // ── Serilog ──────────────────────────────────────────────────────────
        builder.Host.UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration));

        // ── SQLite + EF Core ─────────────────────────────────────────────────
        var defaultDbPath = Path.Combine(AppContext.BaseDirectory, "echohub.db");
        var configured = builder.Configuration.GetConnectionString("DefaultConnection");
        var connectionString = string.IsNullOrWhiteSpace(configured)
            ? $"Data Source={defaultDbPath}"
            : configured;

        builder.Services.AddDbContext<EchoHubDbContext>(options =>
            options.UseSqlite(connectionString));

        // ── JWT Authentication ───────────────────────────────────────────────
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

        // ── Controllers + SignalR ────────────────────────────────────────────
        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        // ── Services ─────────────────────────────────────────────────────────
        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddSingleton<PresenceTracker>();
        builder.Services.AddSingleton<ImageToAsciiService>();
        builder.Services.AddSingleton<FileStorageService>();
        builder.Services.AddHostedService<ServerDirectoryService>();

        // ── Chat Service + Broadcasters ─────────────────────────────────────
        builder.Services.AddSingleton<IChatBroadcaster, SignalRBroadcaster>();
        builder.Services.AddSingleton<IChatService, ChatService>();

        // ── IRC Gateway (optional) ──────────────────────────────────────────
        builder.AddIrcGateway();

        builder.Services.AddHttpClient("ImageDownload", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.MaxResponseContentBufferSize = 10 * 1024 * 1024; // 10 MB
        });

        // ── Rate Limiting ────────────────────────────────────────────────────
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

        // ── CORS ─────────────────────────────────────────────────────────────
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

        await using var app = builder.Build();

        // ── Database initialization ──────────────────────────────────────────
        Console.Error.WriteLine("[DIAG] Database initialization starting...");
        await DatabaseSetup.InitializeAsync(app.Services);
        Console.Error.WriteLine("[DIAG] Database initialization complete.");

        // ── Middleware ────────────────────────────────────────────────────────
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // ── Routing ──────────────────────────────────────────────────────────
        app.MapControllers();
        app.MapHub<ChatHub>(HubConstants.ChatHubPath);

        // ── Diagnostic hooks ────────────────────────────────────────────────
        app.Lifetime.ApplicationStarted.Register(
            () => Console.Error.WriteLine("[DIAG] ApplicationStarted fired"));
        app.Lifetime.ApplicationStopping.Register(
            () => Console.Error.WriteLine("[DIAG] ApplicationStopping fired"));
        app.Lifetime.ApplicationStopped.Register(
            () => Console.Error.WriteLine("[DIAG] ApplicationStopped fired"));

        // Heartbeat — proves the process is alive even if nothing else logs
        var heartbeatCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!heartbeatCts.Token.IsCancellationRequested)
            {
                await Task.Delay(5000, heartbeatCts.Token).ConfigureAwait(false);
                Console.Error.WriteLine($"[DIAG] heartbeat {DateTimeOffset.UtcNow:HH:mm:ss}");
            }
        }, heartbeatCts.Token);

        Console.Error.WriteLine("[DIAG] Calling app.StartAsync()...");
        await app.StartAsync();
        Console.Error.WriteLine("[DIAG] app.StartAsync() completed — server is running.");

        await app.WaitForShutdownAsync();
        Console.Error.WriteLine("[DIAG] WaitForShutdownAsync returned.");
        heartbeatCts.Cancel();

        // Graceful shutdown (Ctrl+C) — exit the loop
        Log.Information("Server shut down gracefully");
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DIAG] Top-level exception: {ex}");

        var uptime = DateTimeOffset.UtcNow - startTime;

        // If server ran for over 60 seconds, it's a runtime crash — reset failure count
        if (uptime.TotalSeconds > 60)
            consecutiveFailures = 0;

        consecutiveFailures++;
        Log.Fatal(ex, "Server crashed after {Uptime:g} (failure {Count}/{Max})",
            uptime, consecutiveFailures, maxConsecutiveFailures);

        if (consecutiveFailures >= maxConsecutiveFailures)
        {
            Log.Fatal("Too many consecutive failures, server will not restart");
            break;
        }

        var delaySeconds = Math.Min(Math.Pow(2, consecutiveFailures), 30);
        Log.Information("Restarting server in {Delay}s...", delaySeconds);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
    }
}

Console.Error.WriteLine("[DIAG] Calling Log.CloseAndFlush()...");
Log.CloseAndFlush();
Console.Error.WriteLine("[DIAG] Log.CloseAndFlush() done. Exiting process.");
