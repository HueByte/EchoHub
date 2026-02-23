using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Manages pre-update backups and rollback restoration for the auto-updater.
/// Backup location: ~/.echohub/update-backup/
/// </summary>
public static class UpdateBackupService
{
    private static readonly string BackupDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".echohub", "update-backup");

    private static readonly string BackupZipPath = Path.Combine(BackupDir, "backup.zip");
    private static readonly string BackupInfoPath = Path.Combine(BackupDir, "backup-info.json");

    /// <summary>
    /// True if a backup exists from a recent update (set at startup).
    /// </summary>
    public static bool IsPostUpdate { get; set; }

    /// <summary>
    /// Creates a ZIP backup of the current app directory before an update.
    /// Deletes any previous backup first. Uses fastest compression for speed.
    /// </summary>
    public static void CreateBackup()
    {
        var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var version = UpdateChecker.CurrentVersion;

        if (Directory.Exists(BackupDir))
            Directory.Delete(BackupDir, true);

        Directory.CreateDirectory(BackupDir);

        Log.Information("Creating pre-update backup of {AppDir} (v{Version})", appDir, version);

        ZipFile.CreateFromDirectory(appDir, BackupZipPath, CompressionLevel.Fastest, includeBaseDirectory: false);

        var info = new BackupInfo(version, appDir, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(info, BackupJsonContext.Default.BackupInfo);
        File.WriteAllText(BackupInfoPath, json);

        Log.Information("Backup created at {BackupPath}", BackupZipPath);
    }

    /// <summary>
    /// Returns true if a valid backup exists (both ZIP and metadata file present).
    /// </summary>
    public static bool BackupExists()
        => File.Exists(BackupZipPath) && File.Exists(BackupInfoPath);

    /// <summary>
    /// Reads backup metadata. Returns null if no backup exists or metadata is unreadable.
    /// </summary>
    public static BackupInfo? GetBackupInfo()
    {
        if (!File.Exists(BackupInfoPath))
            return null;

        try
        {
            var json = File.ReadAllText(BackupInfoPath);
            return JsonSerializer.Deserialize(json, BackupJsonContext.Default.BackupInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read backup metadata");
            return null;
        }
    }

    /// <summary>
    /// Restores the backup ZIP to the app directory, then restarts the process.
    /// This method does not return — it calls Environment.Exit(0).
    /// </summary>
    public static void RestoreBackup()
    {
        var info = GetBackupInfo()
            ?? throw new InvalidOperationException("No backup metadata found");

        var appDir = info.AppDirectory;
        Log.Information("Restoring backup v{Version} to {AppDir}", info.Version, appDir);

        // On Windows, rename the running executable so extraction can overwrite it
        if (OperatingSystem.IsWindows())
        {
            var currentExe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
            {
                var oldExe = currentExe + ".old";
                if (File.Exists(oldExe))
                    File.Delete(oldExe);
                File.Move(currentExe, oldExe);
            }
        }

        ZipFile.ExtractToDirectory(BackupZipPath, appDir, overwriteFiles: true);

        // Restore execute permission on Unix
        if (!OperatingSystem.IsWindows())
        {
            var exePath = Environment.ProcessPath
                ?? Path.Combine(appDir, "EchoHub.Client");

            if (File.Exists(exePath))
            {
                var mode = File.GetUnixFileMode(exePath);
                File.SetUnixFileMode(exePath, mode | UnixFileMode.UserExecute);
            }
        }

        // Start the restored version and exit
        var processPath = Environment.ProcessPath
            ?? Path.Combine(appDir, "EchoHub.Client");

        Log.Information("Launching restored version v{Version}: {Path}", info.Version, processPath);
        Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = false });
        Environment.Exit(0);
    }

    /// <summary>
    /// Deletes the backup directory and all contents.
    /// </summary>
    public static void DeleteBackup()
    {
        if (!Directory.Exists(BackupDir))
            return;

        try
        {
            Directory.Delete(BackupDir, true);
            Log.Information("Update backup deleted");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete update backup");
        }
    }
}

public record BackupInfo(
    string Version,
    string AppDirectory,
    DateTimeOffset CreatedAt);

[System.Text.Json.Serialization.JsonSerializable(typeof(BackupInfo))]
internal partial class BackupJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
