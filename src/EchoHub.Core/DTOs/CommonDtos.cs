namespace EchoHub.Core.DTOs;

public record ErrorResponse(string Error, string? Detail = null);

public record PaginatedResponse<T>(List<T> Items, int Total, int Offset, int Limit);

public enum ChannelError
{
    ValidationFailed,
    AlreadyExists,
    NotFound,
    Forbidden,
    Protected
}

public record ChannelOperationResult(ChannelDto? Channel, ChannelError? Error, string? ErrorMessage)
{
    public bool IsSuccess => Error is null;

    public static ChannelOperationResult Success(ChannelDto channel) => new(channel, null, null);
    public static ChannelOperationResult Fail(ChannelError error, string message) => new(null, error, message);
}
