namespace EchoHub.Core.Models;

public class Channel
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Topic { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? PasswordHash { get; set; }

    // End-to-end encryption envelope (client-generated; server cannot decrypt room content).
    // EncryptionSalt: PBKDF2 salt for passphrase-derived keys. WrappedRoomKey: the room
    // content key encrypted under the passphrase-derived key-encryption key.
    public string? EncryptionSalt { get; set; }
    public string? WrappedRoomKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public List<Message> Messages { get; set; } = [];
}
