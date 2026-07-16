using System.Security.Cryptography;
using EchoHub.Core.Security;
using Serilog;

namespace EchoHub.Client.Services;

/// <summary>
/// Encrypts cached room content keys at rest so the client config never holds them as
/// plain base64. Windows uses DPAPI (current-user scope, format prefix "dp1:"). On other
/// platforms the keys are AES-GCM encrypted with a per-user master key file stored next
/// to the config with 0600 permissions (prefix "k1:") — without an OS keychain that is
/// file-permission-level protection, not zero-knowledge: anyone who can read both the
/// config and the key file can recover the room keys. Values with no recognized prefix
/// are legacy plain-base64 keys from older clients; they load once and are re-encrypted.
/// The room passphrase itself is never stored in any form.
/// </summary>
public sealed class RoomKeyProtector
{
    public const string DpapiPrefix = "dp1:";
    public const string KeyFilePrefix = "k1:";

    private const string KeyFileName = "roomkeys.key";
    private const int MasterKeySizeBytes = 32;
    private const int RoomKeySizeBytes = 32;

    private readonly string _keyFilePath;
    private readonly bool _useDpapi;
    private readonly Lock _lock = new();
    private byte[]? _masterKey;

    /// <param name="keyDirectory">Directory holding the master key file (the client config dir).</param>
    /// <param name="useDpapi">Overrides the platform default (DPAPI on Windows) — for tests.</param>
    public RoomKeyProtector(string keyDirectory, bool? useDpapi = null)
    {
        _keyFilePath = Path.Combine(keyDirectory, KeyFileName);
        _useDpapi = useDpapi ?? OperatingSystem.IsWindows();
    }

    /// <summary>Encrypts a room key for storage in the config file.</summary>
    public string Protect(byte[] roomKey)
    {
        if (_useDpapi && OperatingSystem.IsWindows())
            return DpapiPrefix + Convert.ToBase64String(
                ProtectedData.Protect(roomKey, null, DataProtectionScope.CurrentUser));

        return KeyFilePrefix + Convert.ToBase64String(RoomCrypto.EncryptBytes(roomKey, GetMasterKey()));
    }

    /// <summary>
    /// Decrypts a stored value back into a room key. <paramref name="wasLegacy"/> is true when
    /// the value was an unencrypted legacy entry that should be re-persisted via
    /// <see cref="Protect"/>. Returns false for unreadable values (wrong user/machine, missing
    /// or regenerated key file, malformed data) — the caller drops the entry and the user can
    /// recover it by re-entering the passphrase.
    /// </summary>
    public bool TryUnprotect(string stored, out byte[] roomKey, out bool wasLegacy)
    {
        roomKey = [];
        wasLegacy = false;

        try
        {
            if (stored.StartsWith(DpapiPrefix, StringComparison.Ordinal))
            {
                if (!OperatingSystem.IsWindows())
                    return false; // config copied from a Windows machine

                roomKey = ProtectedData.Unprotect(
                    Convert.FromBase64String(stored[DpapiPrefix.Length..]), null, DataProtectionScope.CurrentUser);
                return roomKey.Length > 0;
            }

            if (stored.StartsWith(KeyFilePrefix, StringComparison.Ordinal))
            {
                if (!File.Exists(_keyFilePath))
                    return false;

                roomKey = RoomCrypto.DecryptBytes(
                    Convert.FromBase64String(stored[KeyFilePrefix.Length..]), GetMasterKey());
                return roomKey.Length > 0;
            }

            // No recognized prefix — legacy plain-base64 room key from a pre-encryption client
            roomKey = Convert.FromBase64String(stored);
            wasLegacy = true;
            return roomKey.Length == RoomKeySizeBytes;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException
            or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private byte[] GetMasterKey()
    {
        lock (_lock)
        {
            if (_masterKey is not null)
                return _masterKey;

            if (File.Exists(_keyFilePath))
            {
                var existing = File.ReadAllBytes(_keyFilePath);
                if (existing.Length == MasterKeySizeBytes)
                    return _masterKey = existing;

                Log.Warning("Room-key master key file has unexpected size — regenerating (previously cached keys become unreadable)");
            }

            var key = RandomNumberGenerator.GetBytes(MasterKeySizeBytes);
            Directory.CreateDirectory(Path.GetDirectoryName(_keyFilePath)!);
            File.WriteAllBytes(_keyFilePath, key);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            return _masterKey = key;
        }
    }
}
