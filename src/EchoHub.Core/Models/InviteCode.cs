namespace EchoHub.Core.Models;

/// <summary>
/// A registration invite code. When the server runs with <c>Server:Registration = "invite"</c>,
/// new accounts (REST and IRC alike) require a valid, unexpired, not-fully-used code.
/// </summary>
public class InviteCode
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Null means the code never expires.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public int MaxUses { get; set; } = 1;
    public int UseCount { get; set; }
}
