namespace EchoHub.Core.Contracts;

public interface IMessageEncryptionService
{
    /// <summary>
    /// Prefix marking transport/at-rest encrypted content. <see cref="Decrypt"/> is a
    /// pass-through for values without it.
    /// </summary>
    const string CiphertextPrefix = "$ENC$v1$";

    /// <summary>
    /// Whether database content should be encrypted at rest (server setting).
    /// </summary>
    bool EncryptDatabaseEnabled { get; }

    string Encrypt(string plaintext);
    string Decrypt(string content);
    string? EncryptNullable(string? value);
    string? DecryptNullable(string? value);
}
