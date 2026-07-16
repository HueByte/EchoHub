using EchoHub.Core.Security;
using Xunit;

namespace EchoHub.Tests;

public class RoomCryptoTests
{
    [Fact]
    public void EncryptText_RoundTrips()
    {
        var key = RoomCrypto.GenerateRoomKey();
        var ciphertext = RoomCrypto.EncryptText("hello secret room", key);

        Assert.StartsWith("$RC1$", ciphertext);
        Assert.True(RoomCrypto.TryDecryptText(ciphertext, key, out var plaintext));
        Assert.Equal("hello secret room", plaintext);
    }

    [Fact]
    public void TryDecryptText_WrongKey_ReturnsFalse()
    {
        var ciphertext = RoomCrypto.EncryptText("hello", RoomCrypto.GenerateRoomKey());

        Assert.False(RoomCrypto.TryDecryptText(ciphertext, RoomCrypto.GenerateRoomKey(), out _));
    }

    [Fact]
    public void TryDecryptText_PlainText_ReturnsFalse()
    {
        Assert.False(RoomCrypto.TryDecryptText("just a normal message", RoomCrypto.GenerateRoomKey(), out _));
    }

    [Fact]
    public void EncryptBytes_RoundTrips()
    {
        var key = RoomCrypto.GenerateRoomKey();
        var payload = new byte[4096];
        Random.Shared.NextBytes(payload);

        var blob = RoomCrypto.EncryptBytes(payload, key);
        var decrypted = RoomCrypto.DecryptBytes(blob, key);

        Assert.Equal(payload, decrypted);
    }

    [Fact]
    public void DeriveKeys_IsDeterministic_AndSaltSensitive()
    {
        var salt = RoomCrypto.GenerateSalt();
        var a = RoomCrypto.DeriveKeys("correct horse battery staple", salt);
        var b = RoomCrypto.DeriveKeys("correct horse battery staple", salt);
        var other = RoomCrypto.DeriveKeys("correct horse battery staple", RoomCrypto.GenerateSalt());

        Assert.Equal(a.AuthKeyHex, b.AuthKeyHex);
        Assert.Equal(a.KeyEncryptionKey, b.KeyEncryptionKey);
        Assert.NotEqual(a.AuthKeyHex, other.AuthKeyHex);
        Assert.NotEqual(a.AuthKeyHex, Convert.ToHexString(a.KeyEncryptionKey).ToLowerInvariant());
    }

    [Fact]
    public void WrapRoomKey_UnwrapsWithSameKek_FailsWithWrongKek()
    {
        var salt = RoomCrypto.GenerateSalt();
        var keys = RoomCrypto.DeriveKeys("passphrase-1", salt);
        var wrongKeys = RoomCrypto.DeriveKeys("passphrase-2", salt);
        var roomKey = RoomCrypto.GenerateRoomKey();

        var wrapped = RoomCrypto.WrapRoomKey(roomKey, keys.KeyEncryptionKey);

        Assert.True(RoomCrypto.TryUnwrapRoomKey(wrapped, keys.KeyEncryptionKey, out var unwrapped));
        Assert.Equal(roomKey, unwrapped);
        Assert.False(RoomCrypto.TryUnwrapRoomKey(wrapped, wrongKeys.KeyEncryptionKey, out _));
    }

    [Fact]
    public void Rewrap_PreservesRoomKey_AcrossPassphraseChange()
    {
        // Simulates a passphrase change: unwrap with old KEK, wrap with new KEK.
        var roomKey = RoomCrypto.GenerateRoomKey();

        var oldSalt = RoomCrypto.GenerateSalt();
        var oldKeys = RoomCrypto.DeriveKeys("old-passphrase", oldSalt);
        var wrappedOld = RoomCrypto.WrapRoomKey(roomKey, oldKeys.KeyEncryptionKey);

        Assert.True(RoomCrypto.TryUnwrapRoomKey(wrappedOld, oldKeys.KeyEncryptionKey, out var recovered));

        var newSalt = RoomCrypto.GenerateSalt();
        var newKeys = RoomCrypto.DeriveKeys("new-passphrase", newSalt);
        var wrappedNew = RoomCrypto.WrapRoomKey(recovered, newKeys.KeyEncryptionKey);

        Assert.True(RoomCrypto.TryUnwrapRoomKey(wrappedNew, newKeys.KeyEncryptionKey, out var final));
        Assert.Equal(roomKey, final);

        // Old messages encrypted before the change still decrypt with the unwrapped key
        var oldMessage = RoomCrypto.EncryptText("written before rekey", roomKey);
        Assert.True(RoomCrypto.TryDecryptText(oldMessage, final, out var plaintext));
        Assert.Equal("written before rekey", plaintext);
    }
}
