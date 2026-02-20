using System.Security.Cryptography;
using System.Text;
using EchoHub.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoHub.Server.Services;

public class MessageEncryptionService : IMessageEncryptionService
{
    private const string EncryptionPrefix = "$ENC$v1$";
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;
    private readonly ILogger<MessageEncryptionService> _logger;

    public bool EncryptDatabaseEnabled { get; }

    public MessageEncryptionService(IConfiguration configuration, ILogger<MessageEncryptionService> logger)
    {
        _logger = logger;

        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key must be configured in appsettings.json.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException($"Encryption:Key must be exactly 32 bytes (256-bit). Got {_key.Length} bytes.");

        EncryptDatabaseEnabled = configuration.GetValue<bool>("Encryption:EncryptDatabase");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine ciphertext + tag for storage
        var combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);

        return $"{EncryptionPrefix}{Convert.ToBase64String(nonce)}${Convert.ToBase64String(combined)}";
    }

    public string Decrypt(string content)
    {
        if (!content.StartsWith(EncryptionPrefix))
            return content; // Legacy plaintext

        try
        {
            var payload = content[EncryptionPrefix.Length..];
            var separatorIndex = payload.IndexOf('$');
            if (separatorIndex < 0)
            {
                _logger.LogWarning("Malformed encrypted content: missing separator");
                return "[encrypted message — decryption failed]";
            }

            var nonceBase64 = payload[..separatorIndex];
            var combinedBase64 = payload[(separatorIndex + 1)..];

            var nonce = Convert.FromBase64String(nonceBase64);
            var combined = Convert.FromBase64String(combinedBase64);

            if (combined.Length < TagSizeBytes)
            {
                _logger.LogWarning("Malformed encrypted content: data too short");
                return "[encrypted message — decryption failed]";
            }

            var ciphertextLength = combined.Length - TagSizeBytes;
            var ciphertext = combined.AsSpan(0, ciphertextLength);
            var tag = combined.AsSpan(ciphertextLength, TagSizeBytes);
            var plaintext = new byte[ciphertextLength];

            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt message content");
            return "[encrypted message — decryption failed]";
        }
    }

    public string? EncryptNullable(string? value)
        => value is null ? null : Encrypt(value);

    public string? DecryptNullable(string? value)
        => value is null ? null : Decrypt(value);
}
