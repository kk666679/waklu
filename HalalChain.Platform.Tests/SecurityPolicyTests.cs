using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HalalChain.Platform.Tests;

public sealed class SecurityPolicyTests
{
    [Fact]
    public void ProtectedApiEndpoints_RequireExplicitPolicyOrRoles()
    {
        var methods = typeof(Program).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes(true).OfType<HttpMethodAttribute>().Any())
            .ToArray();

        var violations = methods
            .Where(m =>
            {
                var controller = m.DeclaringType;
                var classAllowAnonymous = controller?.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                var methodAllowAnonymous = m.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                if (classAllowAnonymous || methodAllowAnonymous)
                    return false;

                var classAuthorize = controller?.GetCustomAttribute<AuthorizeAttribute>();
                var methodAuthorize = m.GetCustomAttribute<AuthorizeAttribute>();
                var authorize = methodAuthorize ?? classAuthorize;
                return authorize is null ||
                       (string.IsNullOrWhiteSpace(authorize.Policy) && string.IsNullOrWhiteSpace(authorize.Roles));
            })
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToArray();

        violations.Should().BeEmpty();
    }
}
