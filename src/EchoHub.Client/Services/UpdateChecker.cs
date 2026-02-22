using AlwaysUpToDate;

using EchoHub.Client.UI.Dialogs;

using Serilog;

using Terminal.Gui.App;

namespace EchoHub.Client.Services;

public sealed class UpdateChecker : IDisposable
{
    private const string ManifestUrl = "https://echohub.voidcube.cloud/api/app/version";

    private readonly Updater _updater;
    private readonly IApplication _app;
    private UpdateProgressDialog? _progressDialog;

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

    private async void OnUpdateAvailable(string version, string changelogUrl)
    {
        Log.Information("Update available: v{Version}", version);

        var confirmed = false;
        _app.Invoke(() =>
        {
            confirmed = UpdateConfirmDialog.Show(_app, CurrentVersion, version);


            if (confirmed)
            {
                _progressDialog = new UpdateProgressDialog(_app, version);

                // Start the update; progress is reported via OnProgressChanged
                _ = Task.Run(async () =>
                {
                    await _updater.UpdateAsync();
                });

                _progressDialog?.Show();
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
    }

    private void OnException(Exception exception)
    {
        Log.Error(exception, "Update check failed");
        _app.Invoke(() =>
        {
            _progressDialog?.Close();
            _progressDialog = null;
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
