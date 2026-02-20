using System.Security.Cryptography;
using EchoHub.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoHub.Tests;

public class MessageEncryptionServiceTests
{
    private static MessageEncryptionService CreateService(
        string? key = null, bool encryptDatabase = false)
    {
        key ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = key,
                ["Encryption:EncryptDatabase"] = encryptDatabase.ToString(),
            })
            .Build();

        var logger = NullLoggerFactory.Instance.CreateLogger<MessageEncryptionService>();
        return new MessageEncryptionService(config, logger);
    }

    // ── Constructor validation ───────────────────────────────────────

    [Fact]
    public void Constructor_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = NullLoggerFactory.Instance.CreateLogger<MessageEncryptionService>();

        Assert.Throws<InvalidOperationException>(() =>
            new MessageEncryptionService(config, logger));
    }

    [Fact]
    public void Constructor_WrongKeyLength_Throws()
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        Assert.Throws<InvalidOperationException>(() => CreateService(shortKey));
    }

    [Fact]
    public void Constructor_ValidKey_Succeeds()
    {
        var service = CreateService();
        Assert.NotNull(service);
    }

    // ── EncryptDatabaseEnabled ───────────────────────────────────────

    [Fact]
    public void EncryptDatabaseEnabled_DefaultFalse()
    {
        var service = CreateService();
        Assert.False(service.EncryptDatabaseEnabled);
    }

    [Fact]
    public void EncryptDatabaseEnabled_WhenConfiguredTrue()
    {
        var service = CreateService(encryptDatabase: true);
        Assert.True(service.EncryptDatabaseEnabled);
    }

    // ── Encrypt ──────────────────────────────────────────────────────

    [Fact]
    public void Encrypt_ProducesEncryptedFormat()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("Hello, world!");

        Assert.StartsWith("$ENC$v1$", encrypted);
    }

    [Fact]
    public void Encrypt_DifferentNonceEachTime()
    {
        var service = CreateService();
        var a = service.Encrypt("same message");
        var b = service.Encrypt("same message");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Encrypt_EmptyString_Works()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("");

        Assert.StartsWith("$ENC$v1$", encrypted);
    }

    [Fact]
    public void Encrypt_Unicode_Works()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("Hello 🌍 世界 مرحبا");

        Assert.StartsWith("$ENC$v1$", encrypted);
    }

    // ── Decrypt ──────────────────────────────────────────────────────

    [Fact]
    public void Decrypt_RoundTrip()
    {
        var service = CreateService();
        var original = "Hello, world!";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_EmptyString_RoundTrip()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("");
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal("", decrypted);
    }

    [Fact]
    public void Decrypt_Unicode_RoundTrip()
    {
        var service = CreateService();
        var original = "Hello 🌍 世界 مرحبا";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_LongMessage_RoundTrip()
    {
        var service = CreateService();
        var original = new string('A', 5000);

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_PlaintextPassthrough()
    {
        var service = CreateService();
        var plaintext = "This is just plain text";

        var result = service.Decrypt(plaintext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Decrypt_WrongKey_ReturnsFailureMessage()
    {
        var key1 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var key2 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var encryptor = CreateService(key1);
        var decryptor = CreateService(key2);

        var encrypted = encryptor.Encrypt("secret message");
        var result = decryptor.Decrypt(encrypted);

        Assert.Contains("decryption failed", result);
    }

    [Fact]
    public void Decrypt_MalformedContent_MissingSeparator_ReturnsFailure()
    {
        var service = CreateService();
        var malformed = "$ENC$v1$noseperatorhere";

        var result = service.Decrypt(malformed);

        Assert.Contains("decryption failed", result);
    }

    [Fact]
    public void Decrypt_MalformedContent_TooShort_ReturnsFailure()
    {
        var service = CreateService();
        var malformed = "$ENC$v1$AAAA$BBBB";

        var result = service.Decrypt(malformed);

        Assert.Contains("decryption failed", result);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ReturnsFailure()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("original message");

        // Tamper with the ciphertext by flipping a character
        var tampered = encrypted[..^5] + "XXXXX";
        var result = service.Decrypt(tampered);

        Assert.Contains("decryption failed", result);
    }

    // ── Nullable helpers ─────────────────────────────────────────────

    [Fact]
    public void EncryptNullable_Null_ReturnsNull()
    {
        var service = CreateService();
        Assert.Null(service.EncryptNullable(null));
    }

    [Fact]
    public void EncryptNullable_Value_Encrypts()
    {
        var service = CreateService();
        var result = service.EncryptNullable("test");

        Assert.NotNull(result);
        Assert.StartsWith("$ENC$v1$", result);
    }

    [Fact]
    public void DecryptNullable_Null_ReturnsNull()
    {
        var service = CreateService();
        Assert.Null(service.DecryptNullable(null));
    }

    [Fact]
    public void DecryptNullable_EncryptedValue_Decrypts()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("test");

        var result = service.DecryptNullable(encrypted);

        Assert.Equal("test", result);
    }
}
