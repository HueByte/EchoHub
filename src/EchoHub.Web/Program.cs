using EchoHub.Api.Data;
using EchoHub.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=serverdirectory.db";

builder.Services.AddServerDirectory(connectionString);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDirectoryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();

app.MapGet("/", () => Results.Content("""
    <!DOCTYPE html>
    <html>
    <head><title>EchoHub</title></head>
    <body>
        <h1>Welcome to EchoHub</h1>
        <p>EchoHub is a decentralized IRC-like chat network.</p>
        <p>Use the <a href="/api/servers">Server Directory API</a> to browse available servers.</p>
    </body>
    </html>
    """, "text/html"));

app.MapServerDirectoryApi();

app.Run();
