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
    private UpdateProgressDialog? _progressDialog;
    private bool _manualCheck;


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

    private async void OnUpdateAvailable(string version, string changelogUrl)
    {
        Log.Information("Update available: v{Version}", version);

        _app.Invoke(() =>
        {
            var confirmed = UpdateConfirmDialog.Show(_app, CurrentVersion, version);

            if (confirmed)
            {
                _progressDialog = new UpdateProgressDialog(_app, version);

                _ = Task.Run(async () =>
                {
                    // Create backup before the update starts
                    try
                    {
                        _app.Invoke(() => _progressDialog?.UpdateProgress(0f, "Creating backup..."));
                        UpdateBackupService.CreateBackup();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to create pre-update backup");

                        var proceed = false;
                        _app.Invoke(() =>
                        {
                            proceed = MessageBox.Query(
                                _app,
                                "Backup Warning",
                                $"Could not create backup: {ex.Message}\n\nContinue update without backup?",
                                "Continue", "Cancel") == 0;
                        });

                        if (!proceed)
                        {
                            _app.Invoke(() =>
                            {
                                _progressDialog?.Close();
                                _progressDialog = null;
                            });
                            return;
                        }
                    }

                    _app.Invoke(() => _progressDialog?.UpdateProgress(0f, "Downloading update..."));
                    await _updater.UpdateAsync();
                });

                _progressDialog.Show();
            }
        });
    }

    private void OnProgressChanged(UpdateStep step, long itemsProcessed, long? totalItems, double? progressPercentage)
    {
        var fraction = progressPercentage.HasValue ? (float)(progressPercentage.Value / 100.0) : 0f;
        var statusText = $"{step}: {itemsProcessed}/{totalItems ?? 0} ({progressPercentage ?? 0:F0}%)";

        if (!progressPercentage.HasValue)
        {
            statusText = $"{step}...";
        }

        _progressDialog?.UpdateProgress(fraction, statusText);
    }

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
        _app.Invoke(() =>
        {
            _progressDialog?.Close();
            _progressDialog = null;

            if (UpdateBackupService.BackupExists())
            {
                var restore = MessageBox.Query(
                    _app,
                    "Update Failed",
                    $"The update failed: {exception.Message}\n\n"
                    + "A backup of the previous version is available.\nRestore now? (The app will restart.)",
                    "Restore", "Cancel");

                if (restore == 0)
                {
                    try
                    {
                        UpdateBackupService.RestoreBackup();
                        // RestoreBackup calls Environment.Exit(0)
                    }
                    catch (Exception restoreEx)
                    {
                        Log.Error(restoreEx, "Backup restoration failed");
                        MessageBox.ErrorQuery(_app, "Restore Failed",
                            $"Could not restore backup: {restoreEx.Message}\n\nYou may need to re-download the application.", "OK");
                    }
                }
            }
            else
            {
                MessageBox.ErrorQuery(_app, "Update Failed",
                    $"The update failed: {exception.Message}\n\nYou may need to re-download the application.", "OK");
            }
        });
    }

    public void Dispose()
    {
        _updater.UpdateAvailable -= OnUpdateAvailable;
        _updater.ProgressChanged -= OnProgressChanged;
        _updater.UpdateStarted -= OnUpdateStarted;
        _updater.NoUpdateAvailable -= OnNoUpdateAvailable;
        _updater.OnException -= OnException;
    }
}
