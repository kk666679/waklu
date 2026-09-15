namespace HalalChain.Platform.Http.Models;

public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error)
{
    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(Error e) => new(false, default, e);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public readonly record struct Result(bool IsSuccess, Error? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(Error e) => new(false, e);
}

public sealed record Error(string Code, string Message, ErrorKind Kind,
    IReadOnlyDictionary<string, object?>? Meta = null)
{
    public static Error NotFound(string entity, object id)
        => new($"{entity}.not_found", $"{entity} '{id}' was not found.", ErrorKind.NotFound);
    public static Error Validation(string code, string msg,
        IReadOnlyDictionary<string, object?>? meta = null)
        => new(code, msg, ErrorKind.Validation, meta);
}

public enum ErrorKind
{
    Validation, NotFound, Conflict, Forbidden, Unauthorized,
    Unprocessable, RateLimited, Upstream, Cancelled, Unknown
}