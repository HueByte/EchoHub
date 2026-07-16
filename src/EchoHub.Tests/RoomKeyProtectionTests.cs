using System.Security.Cryptography;
using EchoHub.Client.Services;
using EchoHub.Core.Security;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// At-rest encryption of the cached room keys (RoomKeyProtector) and the store-level
/// decisions built on it (RoomKeyStore): fresh envelopes overwrite stale cached keys,
/// legacy plain-base64 entries are recognized for the one-way migration.
/// </summary>
public class RoomKeyProtectorTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("echohub-keyprotector-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void KeyFile_Protect_RoundTrips()
    {
        var protector = new RoomKeyProtector(_dir, useDpapi: false);
        var key = RoomCrypto.GenerateRoomKey();

        var stored = protector.Protect(key);

        Assert.StartsWith(RoomKeyProtector.KeyFilePrefix, stored);
        Assert.True(protector.TryUnprotect(stored, out var recovered, out var wasLegacy));
        Assert.Equal(key, recovered);
        Assert.False(wasLegacy);
    }

    [Fact]
    public void Dpapi_Protect_RoundTrips()
    {
        if (!OperatingSystem.IsWindows()) return; // DPAPI is Windows-only

        var protector = new RoomKeyProtector(_dir, useDpapi: true);
        var key = RoomCrypto.GenerateRoomKey();

        var stored = protector.Protect(key);

        Assert.StartsWith(RoomKeyProtector.DpapiPrefix, stored);
        Assert.True(protector.TryUnprotect(stored, out var recovered, out var wasLegacy));
        Assert.Equal(key, recovered);
        Assert.False(wasLegacy);
    }

    [Fact]
    public void Protect_DoesNotStoreThePlainKey()
    {
        var protector = new RoomKeyProtector(_dir, useDpapi: false);
        var key = RoomCrypto.GenerateRoomKey();

        var stored = protector.Protect(key);

        Assert.DoesNotContain(Convert.ToBase64String(key), stored);
    }

    [Fact]
    public void Legacy_PlainBase64_IsAccepted_AndFlaggedForMigration()
    {
        var protector = new RoomKeyProtector(_dir, useDpapi: false);
        var key = RoomCrypto.GenerateRoomKey();

        Assert.True(protector.TryUnprotect(Convert.ToBase64String(key), out var recovered, out var wasLegacy));
        Assert.Equal(key, recovered);
        Assert.True(wasLegacy);

        // The migration re-protects it; the upgraded value round-trips and is no longer legacy
        var upgraded = protector.Protect(recovered);
        Assert.True(protector.TryUnprotect(upgraded, out var recoveredAgain, out var stillLegacy));
        Assert.Equal(key, recoveredAgain);
        Assert.False(stillLegacy);
    }

    [Fact]
    public void Malformed_Values_AreRejected()
    {
        var protector = new RoomKeyProtector(_dir, useDpapi: false);

        Assert.False(protector.TryUnprotect("not base64 at all!!", out _, out _));
        Assert.False(protector.TryUnprotect(RoomKeyProtector.KeyFilePrefix + "not base64!!", out _, out _));
        // Valid base64 but not a 32-byte room key → not a usable legacy entry
        Assert.False(protector.TryUnprotect(Convert.ToBase64String([1, 2, 3]), out _, out _));
    }

    [Fact]
    public void KeyFile_Lost_MakesStoredValuesUnreadable_NotThrow()
    {
        var protector = new RoomKeyProtector(_dir, useDpapi: false);
        var stored = protector.Protect(RoomCrypto.GenerateRoomKey());

        File.Delete(Path.Combine(_dir, "roomkeys.key"));

        // A fresh protector regenerates a different master key — the value must fail
        // cleanly (entry dropped, passphrase prompt recovers it), not throw
        var fresh = new RoomKeyProtector(_dir, useDpapi: false);
        Assert.False(fresh.TryUnprotect(stored, out _, out _));
    }
}

public class RoomKeyStoreEnvelopeTests
{
    // Note: without LoadForServer the store never touches the config file on disk —
    // these tests exercise the in-memory decision logic only.

    private static RoomKeyStore NewStore() =>
        new(new RoomKeyProtector(Path.Combine(Path.GetTempPath(), "echohub-unused"), useDpapi: false));

    [Fact]
    public void TryStoreFromEnvelope_FreshEnvelope_OverwritesStaleCachedKey()
    {
        var store = NewStore();
        var stale = RoomCrypto.GenerateRoomKey();
        store.StoreKey("vault", stale);

        // Channel deleted and recreated under the same name → new room key, new envelope
        var fresh = RoomCrypto.GenerateRoomKey();
        var kek = RandomNumberGenerator.GetBytes(32);
        var wrapped = RoomCrypto.WrapRoomKey(fresh, kek);

        Assert.True(store.TryStoreFromEnvelope("vault", wrapped, kek));
        Assert.True(store.TryGetKey("vault", out var current));
        Assert.Equal(fresh, current);
    }

    [Fact]
    public void TryStoreFromEnvelope_WrongKek_KeepsCachedKey()
    {
        var store = NewStore();
        var cached = RoomCrypto.GenerateRoomKey();
        store.StoreKey("vault", cached);

        var wrapped = RoomCrypto.WrapRoomKey(RoomCrypto.GenerateRoomKey(), RandomNumberGenerator.GetBytes(32));

        Assert.False(store.TryStoreFromEnvelope("vault", wrapped, RandomNumberGenerator.GetBytes(32)));
        Assert.True(store.TryGetKey("vault", out var current));
        Assert.Equal(cached, current);
    }

    [Fact]
    public void MarkChannelEncrypted_TracksAndUntracks()
    {
        var store = NewStore();

        Assert.False(store.IsChannelEncrypted("vault"));

        store.MarkChannelEncrypted("vault", true);
        Assert.True(store.IsChannelEncrypted("vault"));
        Assert.True(store.IsChannelEncrypted("VAULT")); // channel names are case-insensitive

        store.MarkChannelEncrypted("vault", false);
        Assert.False(store.IsChannelEncrypted("vault"));
    }

    [Fact]
    public void StoreKey_MarksChannelEncrypted()
    {
        var store = NewStore();
        store.StoreKey("vault", RoomCrypto.GenerateRoomKey());

        Assert.True(store.IsChannelEncrypted("vault"));
    }
}
