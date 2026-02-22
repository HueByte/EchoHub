using NetCoreAudio;
using Serilog;

namespace EchoHub.Client.Services;

public class AudioPlaybackService
{
    private readonly Player _player = new();

    public bool IsPlaying => _player.Playing;
    public bool IsPaused => _player.Paused;

    public event EventHandler? PlaybackFinished;

    public AudioPlaybackService()
    {
        _player.PlaybackFinished += (s, e) => PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }

    public async Task PlayAsync(string filePath)
    {
        try
        {
            if (_player.Playing)
                await _player.Stop();

            await _player.Play(filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to play audio file: {Path}", filePath);
        }
    }

    public async Task PauseAsync()
    {
        try
        {
            if (_player.Playing && !_player.Paused)
                await _player.Pause();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to pause audio playback");
        }
    }

    public async Task ResumeAsync()
    {
        try
        {
            if (_player.Paused)
                await _player.Resume();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to resume audio playback");
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_player.Playing)
                await _player.Stop();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop audio playback");
        }
    }

    public async Task SetVolumeAsync(byte volume)
    {
        try
        {
            await _player.SetVolume(Math.Min(volume, (byte)100));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set audio volume");
        }
    }
}
