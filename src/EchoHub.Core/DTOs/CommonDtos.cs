namespace EchoHub.Core.DTOs;

public record ErrorResponse(string Error, string? Detail = null);

public record PaginatedResponse<T>(List<T> Items, int Total, int Offset, int Limit);
