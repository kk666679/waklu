using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HalalChain.Platform.Api.Infrastructure.CodeGen;

public sealed class GenerateAdminUiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.DeclaringType?
            .GetCustomAttribute<GenerateAdminUiAttribute>();
        if (attr is null) return;

        operation.Extensions["x-admin-ui"] = new OpenApiObject
        {
            ["displayName"] = new OpenApiString(attr.DisplayName ?? context.MethodInfo.DeclaringType!.Name),
            ["routeSegment"] = new OpenApiString(attr.RouteSegment ?? DeriveSegment(context.MethodInfo.DeclaringType)),
            ["order"] = new OpenApiInteger(attr.Order),
            ["icon"] = new OpenApiString(attr.Icon ?? "widgets"),
            ["policy"] = new OpenApiString(attr.Policy ?? ""),
            ["roles"] = new OpenApiArray { attr.Roles.Select(r => (IOpenApiAny)new OpenApiString(r)) }
        };
    }

    private static string DeriveSegment(Type controller) =>
        controller.Name.Replace("Controller", "").ToLowerInvariant();
}