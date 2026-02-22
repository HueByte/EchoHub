using NetCoreAudio;
using Serilog;

namespace EchoHub.Client.Services;

public class AudioPlaybackService
{
    private readonly Player _player = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsPlaying => _player.Playing;
    public bool IsPaused => _player.Paused;

    public event EventHandler? PlaybackFinished;

    public AudioPlaybackService()
    {
        _player.PlaybackFinished += (s, e) => PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }

    public async Task PlayAsync(string filePath)
    {
        await _lock.WaitAsync();
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
        finally
        {
            _lock.Release();
        }
    }

    public async Task PauseAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_player.Playing && !_player.Paused)
                await _player.Pause();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to pause audio playback");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResumeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_player.Paused)
                await _player.Resume();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to resume audio playback");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_player.Playing || _player.Paused)
                await _player.Stop();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to stop audio playback");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetVolumeAsync(byte volume)
    {
        await _lock.WaitAsync();
        try
        {
            await _player.SetVolume(Math.Min(volume, (byte)100));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set audio volume");
        }
        finally
        {
            _lock.Release();
        }
    }
}
