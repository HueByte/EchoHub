using System.Security.Cryptography;
using System.Text;

namespace EchoHub.Core.Security;

/// <summary>
/// Client-side envelope encryption for private (end-to-end encrypted) channels.
///
/// Design: at creation the client generates a random 256-bit room content key (RCK)
/// that encrypts all room content. The RCK is stored on the server *wrapped*
/// (AES-GCM encrypted) by a key derived from the passphrase, next to a BCrypt hash
/// of a separately derived auth key used as the join gate. The passphrase, the
/// key-encryption key, and the RCK never leave the client, so the server can gate
/// joins and count/measure content without being able to read it. Changing the
/// passphrase only re-wraps the RCK — history is never re-encrypted.
///
/// Derivation: PBKDF2-SHA256(passphrase, salt, 210000 iterations) → 64 bytes;
/// first 32 bytes are the auth key (sent to the server as lowercase hex),
/// last 32 bytes are the key-encryption key (never sent).
/// </summary>
public static class RoomCrypto
{
    public const string CiphertextPrefix = "$RC1$";

    private const int Pbkdf2Iterations = 210_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public sealed record DerivedKeys(string AuthKeyHex, byte[] KeyEncryptionKey);

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSizeBytes);

    public static byte[] GenerateRoomKey() => RandomNumberGenerator.GetBytes(KeySizeBytes);

    /// <summary>
    /// Derives the auth key (join gate credential) and key-encryption key from a passphrase.
    /// </summary>
    public static DerivedKeys DeriveKeys(string passphrase, byte[] salt)
    {
        var okm = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase), salt, Pbkdf2Iterations,
            HashAlgorithmName.SHA256, KeySizeBytes * 2);

        var authKey = Convert.ToHexString(okm.AsSpan(0, KeySizeBytes)).ToLowerInvariant();
        var kek = okm[KeySizeBytes..];
        CryptographicOperations.ZeroMemory(okm.AsSpan(0, KeySizeBytes));
        return new DerivedKeys(authKey, kek);
    }

    /// <summary>Encrypts UTF-8 text with the room key. Output: $RC1$base64(nonce||tag||ciphertext).</summary>
    public static string EncryptText(string plaintext, byte[] key) =>
        CiphertextPrefix + Convert.ToBase64String(EncryptBytes(Encoding.UTF8.GetBytes(plaintext), key));

    /// <summary>
    /// Decrypts text produced by <see cref="EncryptText"/>. Returns false when the input
    /// is not room ciphertext or the key does not match.
    /// </summary>
    public static bool TryDecryptText(string content, byte[] key, out string plaintext)
    {
        plaintext = string.Empty;
        if (!IsRoomCiphertext(content))
            return false;

        try
        {
            var blob = Convert.FromBase64String(content[CiphertextPrefix.Length..]);
            plaintext = Encoding.UTF8.GetString(DecryptBytes(blob, key));
            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    public static bool IsRoomCiphertext(string? content) =>
        content is not null && content.StartsWith(CiphertextPrefix, StringComparison.Ordinal);

    /// <summary>Encrypts a binary blob (file contents) with the room key: nonce||tag||ciphertext.</summary>
    public static byte[] EncryptBytes(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var blob = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, NonceSizeBytes);
        ciphertext.CopyTo(blob, NonceSizeBytes + TagSizeBytes);
        return blob;
    }

    /// <summary>Decrypts a blob produced by <see cref="EncryptBytes"/>. Throws <see cref="CryptographicException"/> on key mismatch.</summary>
    public static byte[] DecryptBytes(byte[] blob, byte[] key)
    {
        if (blob.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Ciphertext blob is too short.");

        var nonce = blob.AsSpan(0, NonceSizeBytes);
        var tag = blob.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = blob.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    /// <summary>Wraps the room content key under the key-encryption key for server storage.</summary>
    public static string WrapRoomKey(byte[] roomKey, byte[] kek) =>
        Convert.ToBase64String(EncryptBytes(roomKey, kek));

    /// <summary>Unwraps the stored room content key. Returns false when the KEK (passphrase) is wrong.</summary>
    public static bool TryUnwrapRoomKey(string wrappedRoomKey, byte[] kek, out byte[] roomKey)
    {
        roomKey = [];
        try
        {
            roomKey = DecryptBytes(Convert.FromBase64String(wrappedRoomKey), kek);
            return roomKey.Length == KeySizeBytes;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }
}
