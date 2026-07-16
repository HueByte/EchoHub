using EchoHub.Client.Config;
using EchoHub.Core.Security;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Holds room content keys for end-to-end encrypted channels: in-memory for the
/// active session, persisted per-server in the client config (like saved sessions)
/// so users don't retype the passphrase every launch. Keys never leave this machine
/// and are encrypted at rest by <see cref="RoomKeyProtector"/>. Also tracks which
/// channels are known to be end-to-end encrypted, so senders can refuse to emit
/// plaintext into a room whose key isn't cached yet.
/// </summary>
public sealed class RoomKeyStore
{
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _encryptedChannels = new(StringComparer.OrdinalIgnoreCase);
    private readonly RoomKeyProtector _protector;
    private readonly Lock _lock = new();
    private string? _serverUrl;

    public RoomKeyStore() : this(new RoomKeyProtector(ConfigManager.ConfigDirectory))
    {
    }

    public RoomKeyStore(RoomKeyProtector protector)
    {
        _protector = protector;
    }

    /// <summary>Binds the store to a server and loads that server's cached keys from config.</summary>
    public void LoadForServer(string serverUrl)
    {
        lock (_lock)
        {
            _serverUrl = serverUrl;
            _keys.Clear();
            _encryptedChannels.Clear();

            var server = FindServer(ConfigManager.Load(), serverUrl);
            if (server is null) return;

            var legacyFound = false;
            foreach (var (channel, stored) in server.ChannelKeys)
            {
                if (_protector.TryUnprotect(stored, out var key, out var wasLegacy))
                {
                    _keys[channel] = key;
                    legacyFound |= wasLegacy;
                }
                else
                {
                    Log.Warning("Ignoring unreadable cached room key for #{Channel}", channel);
                }
            }

            // One-way upgrade: legacy plain-base64 entries get re-persisted encrypted
            // (unreadable entries drop out — the unlock prompt recovers those rooms).
            if (legacyFound)
                Persist(s =>
                {
                    s.ChannelKeys.Clear();
                    foreach (var (channel, key) in _keys)
                        s.ChannelKeys[channel] = _protector.Protect(key);
                });
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
            _encryptedChannels.Add(channelName);
            Persist(server => server.ChannelKeys[channelName] = _protector.Protect(key));
        }
    }

    /// <summary>
    /// Unwraps a fresh key envelope and caches the key, overwriting any stale cached key
    /// (e.g. the channel was deleted and recreated under the same name, so the old key
    /// would encrypt messages nobody else can read). Returns false when the KEK doesn't
    /// open the envelope — the cache is left untouched.
    /// </summary>
    public bool TryStoreFromEnvelope(string channelName, string wrappedRoomKey, byte[] kek)
    {
        if (!RoomCrypto.TryUnwrapRoomKey(wrappedRoomKey, kek, out var roomKey))
            return false;

        StoreKey(channelName, roomKey);
        return true;
    }

    public void RemoveKey(string channelName)
    {
        lock (_lock)
        {
            _keys.Remove(channelName);
            Persist(server => server.ChannelKeys.Remove(channelName));
        }
    }

    /// <summary>
    /// Records whether a channel is end-to-end encrypted (from channel listings, crypto
    /// metadata, or join outcomes). Senders consult this to block plaintext into rooms
    /// whose key isn't cached.
    /// </summary>
    public void MarkChannelEncrypted(string channelName, bool isEncrypted)
    {
        lock (_lock)
        {
            if (isEncrypted)
                _encryptedChannels.Add(channelName);
            else
                _encryptedChannels.Remove(channelName);
        }
    }

    public bool IsChannelEncrypted(string channelName)
    {
        lock (_lock)
        {
            return _encryptedChannels.Contains(channelName);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _keys.Clear();
            _encryptedChannels.Clear();
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
