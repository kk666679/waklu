using Microsoft.AspNetCore.Authorization;

namespace HalalChain.Application.Policies;

public static class VendorPolicies
{
    public const string AdminOnly = "AdminOnly";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
        {
            policy.RequireRole("admin");
        });
    }
}
