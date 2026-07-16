using EchoHub.Client.Config;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Holds room content keys for end-to-end encrypted channels: in-memory for the
/// active session, persisted per-server in the client config (like saved sessions)
/// so users don't retype the passphrase every launch. Keys never leave this machine.
/// </summary>
public sealed class RoomKeyStore
{
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();
    private string? _serverUrl;

    /// <summary>Binds the store to a server and loads that server's cached keys from config.</summary>
    public void LoadForServer(string serverUrl)
    {
        lock (_lock)
        {
            _serverUrl = serverUrl;
            _keys.Clear();

            var server = FindServer(ConfigManager.Load(), serverUrl);
            if (server is null) return;

            foreach (var (channel, base64) in server.ChannelKeys)
            {
                try
                {
                    _keys[channel] = Convert.FromBase64String(base64);
                }
                catch (FormatException)
                {
                    Log.Warning("Ignoring malformed cached room key for #{Channel}", channel);
                }
            }
        }
    }

    public bool TryGetKey(string channelName, out byte[] key)
    {
        lock (_lock)
        {
            if (_keys.TryGetValue(channelName, out var k))
            {
                key = k;
                return true;
            }
        }

        key = [];
        return false;
    }

    public bool HasKey(string channelName) => TryGetKey(channelName, out _);

    /// <summary>Stores a key for the session and persists it to the server's config entry.</summary>
    public void StoreKey(string channelName, byte[] key)
    {
        lock (_lock)
        {
            _keys[channelName] = key;
            Persist(server => server.ChannelKeys[channelName] = Convert.ToBase64String(key));
        }
    }

    public void RemoveKey(string channelName)
    {
        lock (_lock)
        {
            _keys.Remove(channelName);
            Persist(server => server.ChannelKeys.Remove(channelName));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _keys.Clear();
            _serverUrl = null;
        }
    }

    private void Persist(Action<SavedServer> mutate)
    {
        if (_serverUrl is null) return;

        try
        {
            var config = ConfigManager.Load();
            var server = FindServer(config, _serverUrl);
            if (server is null) return; // server not saved yet — key stays in-memory only

            mutate(server);
            ConfigManager.Save(config);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to persist room key cache");
        }
    }

    private static SavedServer? FindServer(ClientConfig config, string url) =>
        config.SavedServers.FirstOrDefault(s =>
            string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));
}
