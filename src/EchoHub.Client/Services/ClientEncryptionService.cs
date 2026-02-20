using System.Security.Cryptography;
using System.Text;
using EchoHub.Core.Contracts;

namespace EchoHub.Client.Services;

/// <summary>
/// Client-side encryption service. Uses the same AES-256-GCM format as the server
/// so messages are encrypted end-to-end between client and server.
/// </summary>
public sealed class ClientEncryptionService : IMessageEncryptionService
{
    private const string EncryptionPrefix = "$ENC$v1$";
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private byte[]? _key;

    public bool IsInitialized => _key is not null;
    public bool EncryptDatabaseEnabled => false; // Not relevant for client

    /// <summary>
    /// Initialize with the server's encryption key (fetched after login).
    /// </summary>
    public void SetKey(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new InvalidOperationException($"Encryption key must be exactly 32 bytes (256-bit). Got {_key.Length} bytes.");
    }

    public string Encrypt(string plaintext)
    {
        if (_key is null)
            return plaintext; // Not initialized — pass through

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);

        return $"{EncryptionPrefix}{Convert.ToBase64String(nonce)}${Convert.ToBase64String(combined)}";
    }

    public string Decrypt(string content)
    {
        if (_key is null || !content.StartsWith(EncryptionPrefix))
            return content; // Not initialized or legacy plaintext

        try
        {
            var payload = content[EncryptionPrefix.Length..];
            var separatorIndex = payload.IndexOf('$');
            if (separatorIndex < 0)
                return content;

            var nonceBase64 = payload[..separatorIndex];
            var combinedBase64 = payload[(separatorIndex + 1)..];

            var nonce = Convert.FromBase64String(nonceBase64);
            var combined = Convert.FromBase64String(combinedBase64);

            if (combined.Length < TagSizeBytes)
                return content;

            var ciphertextLength = combined.Length - TagSizeBytes;
            var ciphertext = combined.AsSpan(0, ciphertextLength);
            var tag = combined.AsSpan(ciphertextLength, TagSizeBytes);
            var plaintext = new byte[ciphertextLength];

            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return "[encrypted message — decryption failed, try re-logging to fetch the latest key]";
        }
    }

    public string? EncryptNullable(string? value)
        => value is null ? null : Encrypt(value);

    public string? DecryptNullable(string? value)
        => value is null ? null : Decrypt(value);
}
