using System.Text;

using AlwaysUpToDate;

using EchoHub.Client.UI.Dialogs;

using Serilog;

using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace EchoHub.Client.Services;

public sealed class UpdateChecker : IDisposable
{
    private const string ManifestUrl = "https://echohub.voidcube.cloud/api/app/version";

    private readonly Updater _updater;
    private readonly IApplication _app;
    private string? _pendingVersion;
    private bool _applying;
    private UpdateStep _lastStep = (UpdateStep)(-1);
    private bool _manualCheck;

    /// <summary>
    /// Set when the user confirms an update. The host runs this <b>after</b> the Terminal.Gui
    /// main loop has exited and the console is restored, so the library's in-place restart doesn't
    /// deadlock against a TUI that still owns the console.
    /// </summary>
    public Func<Task>? PendingUpdate { get; private set; }

    public static string CurrentVersion => typeof(UpdateChecker).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public UpdateChecker(IApplication app)
    {
        _app = app;
        _updater = new Updater(TimeSpan.FromHours(1), ManifestUrl, false);

        _updater.UpdateAvailable += OnUpdateAvailable;
        _updater.ProgressChanged += OnProgressChanged;
        _updater.UpdateStarted += OnUpdateStarted;
        _updater.NoUpdateAvailable += OnNoUpdateAvailable;
        _updater.OnException += OnException;
    }

    public void Start()
    {
#if RELEASE
        _updater.Start();
#endif
    }

    public async Task CheckNowAsync()
    {
        _manualCheck = true;
        try
        {
            await _updater.CheckForUpdateAsync();
        }
        finally
        {
            _manualCheck = false;
        }
    }

    private void OnUpdateAvailable(string version, string changelogUrl)
    {
        Log.Information("Update available: v{Version}", version);

        _app.Invoke(() =>
        {
            var confirmed = UpdateConfirmDialog.Show(_app, CurrentVersion, version);
            if (!confirmed)
                return;

            // Defer the actual download/extract/restart to after the TUI is torn down.
            // Running it under the live main loop lets the library restart the process while
            // this one still holds the console in raw/alternate-screen mode — the two processes
            // then deadlock over the console (the "stuck at N/N extracting" hang).
            _pendingVersion = version;
            PendingUpdate = ApplyUpdateAsync;
            _app.RequestStop();
        });
    }

    /// <summary>
    /// Runs the update on a plain console (invoked by the host after the main loop exits).
    /// Ends by restarting the app and exiting the process, or restoring the backup on failure.
    /// </summary>
    private async Task ApplyUpdateAsync()
    {
        _applying = true;

        // The TUI restored the console on shutdown; make sure the block-glyph bar renders.
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* redirected/non-interactive */ }

        Console.WriteLine();
        Console.WriteLine($"Updating EchoHub to v{_pendingVersion}...");

        try
        {
            Console.WriteLine("Creating backup...");
            UpdateBackupService.CreateBackup();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create pre-update backup");
            Console.WriteLine($"Warning: could not create a backup ({ex.Message}). Continuing without one.");
        }

        Console.WriteLine("Downloading update...");
        await _updater.UpdateAsync(); // download → extract → restart → Environment.Exit(0)
    }

    private const int BarWidth = 28;

    private void OnProgressChanged(UpdateStep step, long itemsProcessed, long? totalItems, double? progressPercentage)
    {
        // Before the TUI is torn down (i.e. during a check) there is no progress surface; the
        // real work happens headless after shutdown, so draw a progress bar on the console.
        if (!_applying)
            return;

        // Finish the previous step's line so each step keeps its completed bar.
        if (step != _lastStep)
        {
            if (_lastStep != (UpdateStep)(-1))
                Console.WriteLine();
            _lastStep = step;
        }

        var label = Humanize(step);

        if (progressPercentage is { } percent)
        {
            var pct = (int)Math.Clamp(Math.Round(percent), 0, 100);
            var filled = pct * BarWidth / 100;
            var bar = new string('█', filled) + new string('░', BarWidth - filled); // █ / ░
            Console.Write($"\r  {label,-13} [{bar}] {pct,3}%   ");
        }
        else
        {
            // Steps with no measurable total (verifying, restarting): show an indeterminate marker.
            Console.Write($"\r  {label,-13} working...   ");
        }
    }

    private static string Humanize(UpdateStep step) => step switch
    {
        UpdateStep.Downloading => "Downloading",
        UpdateStep.VerifyingChecksum => "Verifying",
        UpdateStep.Extracting => "Extracting",
        UpdateStep.CleaningUp => "Cleaning up",
        UpdateStep.Restarting => "Restarting",
        _ => step.ToString(),
    };

    private void OnUpdateStarted(string version)
    {
        Log.Information("Update started: v{Version}", version);
    }

    private void OnNoUpdateAvailable()
    {
        Log.Debug("No update available");
        if (_manualCheck)
        {
            _app.Invoke(() =>
            {
                MessageBox.Query(_app, "Check for Updates", $"You are already on the latest version (v{CurrentVersion}).", "OK");
            });
        }
    }

    private void OnException(Exception exception)
    {
        Log.Error(exception, "Update failed");

        // Headless failure (post-shutdown): report and offer rollback on the console.
        if (_applying)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"Update failed: {exception.Message}");

            if (UpdateBackupService.BackupExists())
            {
                Console.WriteLine("Restoring the previous version...");
                try
                {
                    UpdateBackupService.RestoreBackup(); // calls Environment.Exit(0)
                }
                catch (Exception restoreEx)
                {
                    Log.Error(restoreEx, "Backup restoration failed");
                    Console.Error.WriteLine($"Restore failed: {restoreEx.Message}. Re-download EchoHub to recover.");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.Error.WriteLine("No backup available. Re-download EchoHub if it no longer starts.");
                Environment.Exit(1);
            }
            return;
        }

        // Failure during a check while the TUI is still running.
        _app.Invoke(() =>
        {
            MessageBox.ErrorQuery(_app, "Update Check Failed",
                $"Could not check for updates: {exception.Message}", "OK");
        });
    }

    public void Dispose()
    {
        _updater.UpdateAvailable -= OnUpdateAvailable;
        _updater.ProgressChanged -= OnProgressChanged;
        _updater.UpdateStarted -= OnUpdateStarted;
        _updater.NoUpdateAvailable -= OnNoUpdateAvailable;
        _updater.OnException -= OnException;
        _updater.Dispose();
    }
}
