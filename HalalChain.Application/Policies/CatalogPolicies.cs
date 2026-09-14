using Microsoft.AspNetCore.Authorization;

namespace HalalChain.Application.Policies;

public static class CatalogPolicies
{
    public const string VendorOnly = "VendorOnly";
    public const string VendorOrAdmin = "VendorOrAdmin";
    public const string AdminOnly = "AdminOnly";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(VendorOnly, policy =>
        {
            policy.RequireRole("vendor");
        });

        options.AddPolicy(VendorOrAdmin, policy =>
        {
            policy.RequireRole("vendor", "admin");
        });

        options.AddPolicy(AdminOnly, policy =>
        {
            policy.RequireRole("admin");
        });
    }
}
