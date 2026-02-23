namespace EchoHub.Core.DTOs;

public record ApiResponse(bool Success, string? Message = null, List<string>? Errors = null);

public record ApiResponse<T>(bool Success, string? Message = null, List<string>? Errors = null, T? Data = default);

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

public enum UserError
{
    ValidationFailed,
    AlreadyExists,
    NotFound,
    InvalidCredentials,
    Banned
}

public record UserOperationResult(UserProfileDto? User, UserError? Error, string? ErrorMessage)
{
    public bool IsSuccess => Error is null;

    public static UserOperationResult Success(UserProfileDto user) => new(user, null, null);
    public static UserOperationResult Fail(UserError error, string message) => new(null, error, message);
}
