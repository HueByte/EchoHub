using System.Security.Cryptography;
using EchoHub.Client.Services;
using Xunit;

namespace EchoHub.Tests;

public class ClientEncryptionServiceTests
{
    private static string GenerateKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static ClientEncryptionService CreateInitialized(string? key = null)
    {
        var service = new ClientEncryptionService();
        service.SetKey(key ?? GenerateKey());
        return service;
    }

    // ── Initialization ───────────────────────────────────────────────

    [Fact]
    public void IsInitialized_DefaultFalse()
    {
        var service = new ClientEncryptionService();
        Assert.False(service.IsInitialized);
    }

    [Fact]
    public void IsInitialized_TrueAfterSetKey()
    {
        var service = CreateInitialized();
        Assert.True(service.IsInitialized);
    }

    [Fact]
    public void EncryptDatabaseEnabled_AlwaysFalse()
    {
        var service = CreateInitialized();
        Assert.False(service.EncryptDatabaseEnabled);
    }

    [Fact]
    public void SetKey_WrongLength_Throws()
    {
        var service = new ClientEncryptionService();
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        Assert.Throws<InvalidOperationException>(() => service.SetKey(shortKey));
    }

    [Fact]
    public void SetKey_InvalidBase64_Throws()
    {
        var service = new ClientEncryptionService();
        Assert.Throws<FormatException>(() => service.SetKey("not-valid-base64!!!"));
    }

    // ── Encrypt before initialization ────────────────────────────────

    [Fact]
    public void Encrypt_NotInitialized_PassesThrough()
    {
        var service = new ClientEncryptionService();
        var result = service.Encrypt("hello");

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Decrypt_NotInitialized_PassesThrough()
    {
        var service = new ClientEncryptionService();
        var result = service.Decrypt("$ENC$v1$something$else");

        Assert.Equal("$ENC$v1$something$else", result);
    }

    // ── Encrypt after initialization ─────────────────────────────────

    [Fact]
    public void Encrypt_ProducesEncryptedFormat()
    {
        var service = CreateInitialized();
        var encrypted = service.Encrypt("Hello, world!");

        Assert.StartsWith("$ENC$v1$", encrypted);
    }

    [Fact]
    public void Encrypt_DifferentNonceEachTime()
    {
        var service = CreateInitialized();
        var a = service.Encrypt("same message");
        var b = service.Encrypt("same message");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Encrypt_EmptyString_Works()
    {
        var service = CreateInitialized();
        var encrypted = service.Encrypt("");

        Assert.StartsWith("$ENC$v1$", encrypted);
    }

    // ── Decrypt ──────────────────────────────────────────────────────

    [Fact]
    public void Decrypt_RoundTrip()
    {
        var service = CreateInitialized();
        var original = "Hello, world!";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_EmptyString_RoundTrip()
    {
        var service = CreateInitialized();
        var encrypted = service.Encrypt("");
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal("", decrypted);
    }

    [Fact]
    public void Decrypt_Unicode_RoundTrip()
    {
        var service = CreateInitialized();
        var original = "Hello 🌍 世界 مرحبا";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_PlaintextPassthrough()
    {
        var service = CreateInitialized();
        var plaintext = "This is just plain text";

        var result = service.Decrypt(plaintext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Decrypt_WrongKey_ReturnsFailureMessage()
    {
        var encryptor = CreateInitialized();
        var decryptor = CreateInitialized(); // different key

        var encrypted = encryptor.Encrypt("secret message");
        var result = decryptor.Decrypt(encrypted);

        Assert.Contains("decryption failed", result);
    }

    [Fact]
    public void Decrypt_MalformedContent_ReturnsOriginal()
    {
        var service = CreateInitialized();

        // Malformed: prefix present but no valid separator after nonce
        var result = service.Decrypt("$ENC$v1$noseperatorhere");

        Assert.Equal("$ENC$v1$noseperatorhere", result);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ReturnsFailure()
    {
        var service = CreateInitialized();
        var encrypted = service.Encrypt("original message");

        var tampered = encrypted[..^5] + "XXXXX";
        var result = service.Decrypt(tampered);

        Assert.Contains("decryption failed", result);
    }

    // ── Nullable helpers ─────────────────────────────────────────────

    [Fact]
    public void EncryptNullable_Null_ReturnsNull()
    {
        var service = CreateInitialized();
        Assert.Null(service.EncryptNullable(null));
    }

    [Fact]
    public void DecryptNullable_Null_ReturnsNull()
    {
        var service = CreateInitialized();
        Assert.Null(service.DecryptNullable(null));
    }

    // ── Key replacement ──────────────────────────────────────────────

    [Fact]
    public void SetKey_CanBeCalledMultipleTimes()
    {
        var service = new ClientEncryptionService();

        var key1 = GenerateKey();
        var key2 = GenerateKey();

        service.SetKey(key1);
        var encrypted1 = service.Encrypt("test");

        service.SetKey(key2);
        var encrypted2 = service.Encrypt("test");

        // Both produce encrypted content
        Assert.StartsWith("$ENC$v1$", encrypted1);
        Assert.StartsWith("$ENC$v1$", encrypted2);

        // Old key's content can't be decrypted with new key
        var result = service.Decrypt(encrypted1);
        Assert.Contains("decryption failed", result);

        // New key's content decrypts fine
        Assert.Equal("test", service.Decrypt(encrypted2));
    }
}
