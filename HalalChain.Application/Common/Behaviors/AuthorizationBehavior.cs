using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HalalChain.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (httpContextAccessor.HttpContext is null)
            return await next();

        var authorizeAttribute = typeof(TRequest).GetCustomAttributes(typeof(IAuthorizeData), true).FirstOrDefault() as IAuthorizeData;
        if (authorizeAttribute is null)
            return await next();

        var policyName = authorizeAttribute.Policy ?? authorizeAttribute.Roles ?? authorizeAttribute.AuthenticationSchemes;
        if (string.IsNullOrEmpty(policyName))
            return await next();

        var authorizationResult = await authorizationService.AuthorizeAsync(httpContextAccessor.HttpContext.User, request, policyName);
        if (!authorizationResult.Succeeded)
            throw new ForbiddenException("You are not authorized to perform this action.");

        return await next();
    }
}
