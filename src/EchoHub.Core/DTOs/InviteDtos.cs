namespace EchoHub.Core.DTOs;

public record CreateInviteRequest(int? MaxUses = null, int? ExpiresInHours = null);

public record InviteDto(
    string Code,
    string CreatedByUsername,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int MaxUses,
    int UseCount);
