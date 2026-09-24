using HalalChain.Platform.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;

namespace HalalChain.Application.Policies;

public static class CatalogPolicies
{
    public const string VendorOnly = "VendorOnly";
    public const string VendorOrAdmin = "VendorOrAdmin";
    public const string AdminOnly = "AdminOnly";
    public const string MarketplaceUserOnly = "MarketplaceUserOnly";
    public const string VerificationOfficerOnly = "VerificationOfficerOnly";
    public const string AdminOrVerificationOfficer = "AdminOrVerificationOfficer";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(VendorOnly, policy =>
        {
            policy.RequireRole(AuthConstants.RoleVendor);
        });

        options.AddPolicy(VendorOrAdmin, policy =>
        {
            policy.RequireRole(AuthConstants.RoleVendor, AuthConstants.RoleAdmin);
        });

        options.AddPolicy(AdminOnly, policy =>
        {
            policy.RequireRole(AuthConstants.RoleAdmin);
        });

        options.AddPolicy(MarketplaceUserOnly, policy =>
        {
            policy.RequireRole(AuthConstants.RoleMarketplaceUser);
        });

        options.AddPolicy(VerificationOfficerOnly, policy =>
        {
            policy.RequireRole(AuthConstants.RoleVerificationOfficer);
        });

        options.AddPolicy(AdminOrVerificationOfficer, policy =>
        {
            policy.RequireRole(AuthConstants.RoleAdmin, AuthConstants.RoleVerificationOfficer);
        });
    }
}
