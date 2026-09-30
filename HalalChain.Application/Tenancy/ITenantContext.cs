namespace HalalChain.Application.Tenancy;

/// <summary>
/// Ambient tenant context for the current request.
/// Tenant isolation is mandatory (P6): every tenant-owned resource read or write
/// must be scoped through this context rather than a caller-supplied identifier.
/// </summary>
public interface ITenantContext
{
    /// <summary>Identifier of the tenant owning the current unit of work.</summary>
    string TenantId { get; }

    /// <summary>Capabilities granted to the caller within this tenant.</summary>
    IReadOnlySet<string> Capabilities { get; }

    /// <summary>True when the request has an authenticated tenant binding.</summary>
    bool IsResolved { get; }
}
