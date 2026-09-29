namespace HalalChain.Marketplace.Services.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Tenancy;

/// <summary>
/// Service for managing tenant-aware navigation and breadcrumbs.
/// Builds navigation paths based on current route, tenant context, and user capabilities.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Builds breadcrumb trail for current page.
    /// Returns list of breadcrumb items ordered from root to current page.
    /// </summary>
    Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(
        ITenantContext tenantContext,
        string currentPath);

    /// <summary>
    /// Resolves full URL for a dashboard page within the tenant context.
    /// </summary>
    Task<string> ResolvePageUrlAsync(
        ITenantContext tenantContext,
        string pageId,
        Dictionary<string, string>? parameters = null);

    /// <summary>
    /// Gets active navigation item for current route.
    /// </summary>
    Task<NavigationItem?> GetActiveNavigationItemAsync(
        ITenantContext tenantContext,
        string currentPath);

    /// <summary>
    /// Gets previous page in navigation history (for back button).
    /// </summary>
    Task<BreadcrumbItem?> GetPreviousPageAsync(
        ITenantContext tenantContext,
        string currentPath);

    /// <summary>
    /// Builds full navigation tree filtered by user capabilities.
    /// </summary>
    Task<List<NavigationItem>> BuildNavigationTreeAsync(
        ITenantContext tenantContext);

    /// <summary>
    /// Resolves page title based on navigation item and optional custom title.
    /// </summary>
    Task<string> ResolvePageTitleAsync(
        ITenantContext tenantContext,
        string pageId,
        string? customTitle = null);
}

/// <summary>Breadcrumb navigation item.</summary>
public class BreadcrumbItem
{
    /// <summary>Display label for breadcrumb.</summary>
    public string Label { get; set; } = null!;

    /// <summary>URL/route for breadcrumb link.</summary>
    public string? Href { get; set; }

    /// <summary>Whether this is the current page (last breadcrumb).</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Icon class (optional).</summary>
    public string? IconClass { get; set; }

    /// <summary>Order in breadcrumb trail.</summary>
    public int Order { get; set; }
}

/// <summary>Navigation context for a request.</summary>
public class NavigationContext
{
    /// <summary>Current route path.</summary>
    public string CurrentPath { get; set; } = null!;

    /// <summary>Page ID if mapped from route.</summary>
    public string? PageId { get; set; }

    /// <summary>Active navigation item.</summary>
    public NavigationItem? ActiveItem { get; set; }

    /// <summary>Breadcrumb trail.</summary>
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    /// <summary>Page title resolved from navigation or custom source.</summary>
    public string? PageTitle { get; set; }

    /// <summary>Navigation parent (for subpages).</summary>
    public NavigationItem? ParentItem { get; set; }
}
