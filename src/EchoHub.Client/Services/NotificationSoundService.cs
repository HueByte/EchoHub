using EchoHub.Client.Config;
using NetCoreAudio;
using Serilog;

namespace EchoHub.Client.Services;

public class NotificationSoundService
{
    // Safety net: if PlaybackFinished never fires we don't want to block future notifications forever.
    private static readonly TimeSpan PlaybackTimeout = TimeSpan.FromSeconds(10);

    private readonly Player _player = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly NotificationConfig _config;
    private string? _resolvedSoundPath;

    public NotificationSoundService(NotificationConfig config)
    {
        _config = config;
        ResolveSoundPath();
    }

    public void SetEnabled(bool enabled) => _config.Enabled = enabled;

    public void SetVolume(byte volume) => _config.Volume = Math.Min(volume, (byte)100);

    public async Task PlayAsync()
    {
        if (!_config.Enabled || _resolvedSoundPath is null)
            return;

        await PlayInternal();
    }

    /// <summary>
    /// Plays the notification sound regardless of the Enabled setting (for /test-sound).
    /// </summary>
    public async Task PlayTestAsync()
    {
        if (_resolvedSoundPath is null)
            return;

        await PlayInternal();
    }

    private async Task PlayInternal()
    {
        await _lock.WaitAsync();

        // _player.Play returns as soon as playback starts, so we wait on PlaybackFinished
        // to hold the lock for the duration of the sound. A one-shot handler + timeout
        // keeps the finally release robust: never-fires → timeout; fires twice → ignored
        // (TrySetResult); handler throws → caller's catch still runs finally.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnFinished(object? s, EventArgs e) => completion.TrySetResult();
        _player.PlaybackFinished += OnFinished;

        try
        {
            await _player.SetVolume(_config.Volume);
            await _player.Play(_resolvedSoundPath!);
            await Task.WhenAny(completion.Task, Task.Delay(PlaybackTimeout));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to play notification sound");
        }
        finally
        {
            _player.PlaybackFinished -= OnFinished;
            _lock.Release();
        }
    }

    private void ResolveSoundPath()
    {
        // 1. Explicit path from config (~/.echohub/config.json)
        if (!string.IsNullOrWhiteSpace(_config.SoundFile))
        {
            if (File.Exists(_config.SoundFile))
            {
                _resolvedSoundPath = Path.GetFullPath(_config.SoundFile);
                Log.Debug("Notification sound: {Path} (from config)", _resolvedSoundPath);
                return;
            }

            Log.Warning("Configured sound file not found: {Path}", _config.SoundFile);
        }

        // 2. Default: Notification.mp3 bundled next to the executable
        var defaultPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Notification.mp3");

        if (File.Exists(defaultPath))
        {
            _resolvedSoundPath = defaultPath;
            Log.Debug("Notification sound: {Path} (default)", _resolvedSoundPath);
            return;
        }

        Log.Information("No notification sound file found — notifications will be silent");
    }
}
