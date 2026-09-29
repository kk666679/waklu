namespace HalalChain.Marketplace.Services.Dashboard;

using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Tenancy;

/// <summary>
/// Implementation of INavigationService.
/// Handles tenant-aware routing, breadcrumbs, and navigation context resolution.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IDashboardConfigService _configService;
    private readonly ILogger<NavigationService> _logger;

    // Route-to-page mapping for quick lookups
    private readonly Dictionary<string, string> _routeToPageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/dashboard", "dashboard" },
        { "/dashboard/admin", "admin" },
        { "/dashboard/admin/vendors", "admin-vendors" },
        { "/dashboard/admin/products", "admin-products" },
        { "/dashboard/admin/orders", "admin-orders" },
        { "/dashboard/admin/audit", "admin-audit" },
        { "/dashboard/vendor", "vendor" },
        { "/dashboard/vendor/sales", "vendor-sales" },
        { "/dashboard/vendor/inventory", "vendor-inventory" },
        { "/dashboard/vendor/orders", "vendor-orders" },
        { "/dashboard/auditor", "auditor" },
        { "/dashboard/auditor/compliance", "auditor-compliance" },
        { "/dashboard/auditor/evidence", "auditor-evidence" }
    };

    // Page ID-to-title mapping
    private readonly Dictionary<string, string> _pageTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dashboard", "Dashboard" },
        { "admin", "Admin Dashboard" },
        { "admin-vendors", "Vendor Management" },
        { "admin-products", "Product Management" },
        { "admin-orders", "Order Management" },
        { "admin-audit", "Audit Logs" },
        { "vendor", "Vendor Dashboard" },
        { "vendor-sales", "Sales Analysis" },
        { "vendor-inventory", "Inventory" },
        { "vendor-orders", "Order History" },
        { "auditor", "Auditor Dashboard" },
        { "auditor-compliance", "Compliance Reports" },
        { "auditor-evidence", "Evidence Review" }
    };

    public NavigationService(
        IDashboardConfigService configService,
        ILogger<NavigationService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(
        ITenantContext tenantContext,
        string currentPath)
    {
        var breadcrumbs = new List<BreadcrumbItem>();

        try
        {
            // Always include home breadcrumb
            breadcrumbs.Add(new BreadcrumbItem
            {
                Label = "Home",
                Href = $"/tenant/{tenantContext.TenantId}/dashboard",
                Order = 0
            });

            // Parse path segments
            var segments = currentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var pathBuilt = "";

            for (int i = 0; i < segments.Length; i++)
            {
                pathBuilt += "/" + segments[i];

                // Skip tenant ID segment
                if (segments[i] == tenantContext.TenantId)
                    continue;

                var pageId = ResolvePageIdFromRoute(pathBuilt);
                if (pageId != null)
                {
                    var title = await ResolvePageTitleAsync(tenantContext, pageId);
                    var isCurrent = i == segments.Length - 1;

                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Label = title,
                        Href = isCurrent ? null : $"/tenant/{tenantContext.TenantId}{pathBuilt}",
                        IsCurrent = isCurrent,
                        Order = breadcrumbs.Count
                    });
                }
            }

            _logger.LogDebug(
                "Breadcrumbs built for tenant {TenantId}, path {Path}: {BreadcrumbCount} items",
                tenantContext.TenantId, currentPath, breadcrumbs.Count);

            return breadcrumbs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building breadcrumbs for path {Path}", currentPath);
            return breadcrumbs;
        }
    }

    public async Task<string> ResolvePageUrlAsync(
        ITenantContext tenantContext,
        string pageId,
        Dictionary<string, string>? parameters = null)
    {
        // Get navigation items and find matching page
        var navItems = await _configService.GetNavigationAsync(tenantContext);
        var page = FindNavigationItemByPageId(navItems, pageId);

        if (page?.Href == null)
        {
            _logger.LogWarning("Page URL not resolved for page ID: {PageId}", pageId);
            return $"/tenant/{tenantContext.TenantId}/dashboard";
        }

        var url = $"/tenant/{tenantContext.TenantId}{page.Href}";

        // Append parameters if provided
        if (parameters?.Count > 0)
        {
            var queryParams = string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            url = $"{url}?{queryParams}";
        }

        return url;
    }

    public async Task<NavigationItem?> GetActiveNavigationItemAsync(
        ITenantContext tenantContext,
        string currentPath)
    {
        var pageId = ResolvePageIdFromRoute(currentPath);
        if (pageId == null) return null;

        var navItems = await _configService.GetNavigationAsync(tenantContext);
        return FindNavigationItemByPageId(navItems, pageId);
    }

    public async Task<BreadcrumbItem?> GetPreviousPageAsync(
        ITenantContext tenantContext,
        string currentPath)
    {
        var breadcrumbs = await GetBreadcrumbsAsync(tenantContext, currentPath);
        
        // Find the breadcrumb before the current one
        var currentIndex = breadcrumbs.FindIndex(b => b.IsCurrent);
        if (currentIndex > 0)
        {
            return breadcrumbs[currentIndex - 1];
        }

        return null;
    }

    public async Task<List<NavigationItem>> BuildNavigationTreeAsync(
        ITenantContext tenantContext)
    {
        var navItems = await _configService.GetNavigationAsync(tenantContext);
        return navItems.OrderBy(x => x.Order).ToList();
    }

    public async Task<string> ResolvePageTitleAsync(
        ITenantContext tenantContext,
        string pageId,
        string? customTitle = null)
    {
        if (!string.IsNullOrEmpty(customTitle))
            return customTitle;

        if (_pageTitles.TryGetValue(pageId, out var title))
            return title;

        // Try to find in navigation items
        var navItems = await _configService.GetNavigationAsync(tenantContext);
        var navItem = FindNavigationItemByPageId(navItems, pageId);
        
        return navItem?.Label ?? pageId;
    }

    // Private helper methods

    private string? ResolvePageIdFromRoute(string path)
    {
        // Normalize path
        var normalizedPath = path.TrimStart('/').ToLowerInvariant();

        // Try exact match first
        foreach (var (route, pageId) in _routeToPageMap)
        {
            if (normalizedPath.Equals(route.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
                return pageId;
        }

        // Try prefix match for routes with parameters
        var pathSegments = normalizedPath.Split('/');
        var basePath = $"/{string.Join("/", pathSegments.Take(Math.Max(1, pathSegments.Length - 1)))}";

        foreach (var (route, pageId) in _routeToPageMap)
        {
            if (basePath.StartsWith(route, StringComparison.OrdinalIgnoreCase))
                return pageId;
        }

        return null;
    }

    private NavigationItem? FindNavigationItemByPageId(
        List<NavigationItem> items,
        string pageId)
    {
        foreach (var item in items)
        {
            if (item.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase))
                return item;

            if (item.Children?.Count > 0)
            {
                var found = FindNavigationItemByPageId(item.Children, pageId);
                if (found != null) return found;
            }
        }

        return null;
    }
}
