namespace EchoHub.Core.Contracts;

public interface IMessageEncryptionService
{
    /// <summary>
    /// Whether database content should be encrypted at rest (server setting).
    /// </summary>
    bool EncryptDatabaseEnabled { get; }

    string Encrypt(string plaintext);
    string Decrypt(string content);
    string? EncryptNullable(string? value);
    string? DecryptNullable(string? value);
}
