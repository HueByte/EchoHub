using EchoHub.Client.Config;
using NetCoreAudio;
using Serilog;

namespace EchoHub.Client.Services;

public class NotificationSoundService
{
    private readonly Player _player = new();
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

        try
        {
            if (_player.Playing)
                await _player.Stop();

            await _player.SetVolume(_config.Volume);
            await _player.Play(_resolvedSoundPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to play notification sound");
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
