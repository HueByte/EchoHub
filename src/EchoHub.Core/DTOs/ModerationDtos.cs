using EchoHub.Core.Models;

namespace EchoHub.Core.DTOs;

public record AssignRoleRequest(string Username, ServerRole Role);
public record MuteRequest(string? Reason = null, int? DurationMinutes = null);
public record BanRequest(string? Reason = null);
public record KickRequest(string? Reason = null);
