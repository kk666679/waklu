using System.Net;
using Microsoft.AspNetCore.Mvc;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Platform.Http.Resilience;

public static class ApiErrorMapper
{
    public static Error From(HttpStatusCode status, ProblemDetails? problem)
    {
        var code = problem?.Extensions?.TryGetValue("code", out var c) == true
            ? c?.ToString() ?? DefaultCode(status)
            : DefaultCode(status);
        var msg = problem?.Detail ?? problem?.Title ?? DefaultMessage(status);

        return new Error(code, msg, status switch
        {
            HttpStatusCode.BadRequest          => ErrorKind.Validation,
            HttpStatusCode.Unauthorized        => ErrorKind.Unauthorized,
            HttpStatusCode.Forbidden           => ErrorKind.Forbidden,
            HttpStatusCode.NotFound            => ErrorKind.NotFound,
            HttpStatusCode.Conflict            => ErrorKind.Conflict,
            HttpStatusCode.UnprocessableEntity => ErrorKind.Unprocessable,
            HttpStatusCode.TooManyRequests     => ErrorKind.RateLimited,
            _ when (int)status >= 500          => ErrorKind.Upstream,
            _                                  => ErrorKind.Unknown
        });
    }

    private static string DefaultCode(HttpStatusCode s) => s switch
    {
        HttpStatusCode.Unauthorized => "auth.unauthorized",
        HttpStatusCode.Forbidden    => "auth.forbidden",
        HttpStatusCode.NotFound     => "resource.not_found",
        _                           => "server.error"
    };

    private static string DefaultMessage(HttpStatusCode s) => s switch
    {
        HttpStatusCode.Unauthorized => "Please sign in to continue.",
        HttpStatusCode.Forbidden    => "You don't have access to this resource.",
        HttpStatusCode.NotFound     => "The requested item was not found.",
        _                           => "Something went wrong. Please retry."
    };
}