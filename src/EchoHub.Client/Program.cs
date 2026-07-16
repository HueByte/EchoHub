using EchoHub.Client;
using EchoHub.Client.Config;
using EchoHub.Client.Services;
using EchoHub.Client.Themes;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Settings.Configuration;
using Terminal.Gui.App;

// == CLI rollback: works without TUI, before anything else ================
if (args.Contains("--rollback"))
{
    if (UpdateBackupService.BackupExists())
    {
        var info = UpdateBackupService.GetBackupInfo();
        Console.WriteLine($"Rolling back to version {info?.Version ?? "unknown"}...");
        try
        {
            UpdateBackupService.RestoreBackup();
            // RestoreBackup calls Environment.Exit(0)
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Rollback failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
    else
    {
        Console.Error.WriteLine("No backup available to restore.");
        Environment.Exit(1);
    }
}

// == Unix permission self-check (defense-in-depth after auto-update) ==
if (!OperatingSystem.IsWindows())
{
    var exePath = Environment.ProcessPath;
    if (!string.IsNullOrEmpty(exePath))
    {
        try
        {
            var mode = File.GetUnixFileMode(exePath);
            if ((mode & UnixFileMode.UserExecute) == 0)
            {
                File.SetUnixFileMode(exePath, mode | UnixFileMode.UserExecute);
            }
        }
        catch
        {
            // Best-effort; if we're running, we already have execute permission
        }
    }
}

// == Normal startup ==
var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(appSettingsPath))
{
    using var stream = typeof(AppOrchestrator).Assembly
        .GetManifestResourceStream("EchoHub.Client.appsettings.example.json");

    if (stream is not null)
    {
        using var file = File.Create(appSettingsPath);
        stream.CopyTo(file);
    }
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();

// Explicit sink-assembly reference is required under PublishSingleFile — the default
// AssemblyFinder scans for Serilog.Sinks.*.dll on disk, which don't exist in a bundled exe.
var serilogOptions = new ConfigurationReaderOptions(typeof(FileLoggerConfigurationExtensions).Assembly);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration, serilogOptions)
    .CreateLogger();

Log.Information("EchoHub client starting");

// == Ensure echohub is on PATH for convenient terminal access ============
PathSetup.EnsureOnPath();

// == Post-update detection: stale backup cleanup or flag for rollback menu ================
if (UpdateBackupService.BackupExists())
{
    var backupInfo = UpdateBackupService.GetBackupInfo();
    if (backupInfo is not null && DateTimeOffset.UtcNow - backupInfo.CreatedAt > TimeSpan.FromDays(7))
    {
        Log.Information("Deleting stale update backup from {Date}", backupInfo.CreatedAt);
        UpdateBackupService.DeleteBackup();
    }
    else
    {
        Log.Information("Post-update: backup of v{OldVersion} available for rollback",
            backupInfo?.Version ?? "unknown");
        UpdateBackupService.IsPostUpdate = true;
    }
}

// == Windows: clean up .old executable left by rollback restore ===========
if (OperatingSystem.IsWindows())
{
    var currentExe = Environment.ProcessPath;
    if (!string.IsNullOrEmpty(currentExe))
    {
        var oldExe = currentExe + ".old";
        if (File.Exists(oldExe))
        {
            try { File.Delete(oldExe); }
            catch { /* locked or permission issue — will be cleaned next launch */ }
        }
    }
}

try
{
    var config = ConfigManager.Load();
    Log.Information("Configuration loaded, active theme: {Theme}", config.ActiveTheme);

    var app = Application.Create().Init();

    var theme = ThemeManager.GetTheme(config.ActiveTheme);
    ThemeManager.ApplyTheme(theme);

    var orchestrator = new AppOrchestrator(app, config);

    app.Run(orchestrator.MainWindow);

    // Capture any confirmed update before tearing anything down, then restore the console.
    var pendingUpdate = orchestrator.PendingUpdate;
    app.Dispose();

    // Apply the update on a clean console: the TUI has released it, so the updater can extract
    // and restart the process without deadlocking against the alternate-screen buffer. This call
    // ends by starting the new version and calling Environment.Exit, so it does not return.
    if (pendingUpdate is not null)
    {
        Log.Information("Applying confirmed update after shutdown");
        await pendingUpdate();
    }

    orchestrator.Dispose();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EchoHub client crashed");
    throw;
}
finally
{
    Log.Information("EchoHub client shutting down");
    Log.CloseAndFlush();
}
