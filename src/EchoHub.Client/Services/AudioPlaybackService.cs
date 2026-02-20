using NetCoreAudio;
using Serilog;

namespace EchoHub.Client.Services;

public class AudioPlaybackService
{
    private readonly Player _player = new();

    public bool IsPlaying => _player.Playing;

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
}
