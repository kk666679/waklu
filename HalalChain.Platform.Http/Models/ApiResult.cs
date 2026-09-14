namespace HalalChain.Platform.Http.Models;

public enum ApiStatus
{
    Ok = 200,
    Empty = 204,
    Error = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    ValidationError = 422,
    Offline = 0,
    ServerError = 500
}

public sealed record ApiResult<T>(bool IsSuccess, T? Data, ApiStatus Status, string? Error = null)
{
    public static ApiResult<T> Ok(T data) => new(true, data, ApiStatus.Ok, null);
    public static ApiResult<T> Empty() => new(true, default, ApiStatus.Empty, null);
    public static ApiResult<T> Empty(T data) => new(true, data, ApiStatus.Empty, null);
    public static ApiResult<T> Fail(ApiStatus status, string error) => new(false, default, status, error);
}

public sealed record ApiError(string? Field, string? Code, string? Message);

public sealed record ValidationErrorResponse(string? Message, IReadOnlyList<ApiError>? Errors);
