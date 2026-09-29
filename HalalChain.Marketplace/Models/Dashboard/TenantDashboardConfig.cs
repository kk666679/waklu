namespace HalalChain.Marketplace.Models.Dashboard;

using System.Collections.Generic;

/// <summary>
/// Tenant-specific dashboard configuration.
/// Defines branding, layout regions, navigation, feature flags, and data source bindings.
/// Can be stored in database, appsettings.json, or cached with TTL.
/// </summary>
public class TenantDashboardConfig
{
    /// <summary>Unique identifier for this tenant's configuration.</summary>
    public string TenantId { get; set; } = null!;

    /// <summary>Tenant organization name displayed in branding.</summary>
    public string OrganizationName { get; set; } = null!;

    /// <summary>Tenant logo URL for header branding.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Tenant favicon URL.</summary>
    public string? FaviconUrl { get; set; }

    /// <summary>Primary brand color (hex code).</summary>
    public string PrimaryColor { get; set; } = "#0066CC";

    /// <summary>Secondary brand color (hex code).</summary>
    public string SecondaryColor { get; set; } = "#F5F5F5";

    /// <summary>Dark mode primary color (hex code).</summary>
    public string DarkPrimaryColor { get; set; } = "#00A8FF";

    /// <summary>Dark mode background color (hex code).</summary>
    public string DarkBackgroundColor { get; set; } = "#1A1A1A";

    /// <summary>Theme name: "light", "dark", "auto".</summary>
    public string Theme { get; set; } = "auto";

    /// <summary>Sidebar configuration for this tenant.</summary>
    public SidebarConfig Sidebar { get; set; } = new();

    /// <summary>Header configuration for this tenant.</summary>
    public HeaderConfig Header { get; set; } = new();

    /// <summary>Footer configuration for this tenant.</summary>
    public FooterConfig Footer { get; set; } = new();

    /// <summary>Available dashboard regions and their configurations.</summary>
    public Dictionary<string, DashboardRegionConfig> Regions { get; set; } = new();

    /// <summary>Navigation menu items specific to this tenant.</summary>
    public List<NavigationItem> NavigationItems { get; set; } = new();

    /// <summary>Feature flags for this tenant (e.g., enable_audit_tab, show_compliance_dashboard).</summary>
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();

    /// <summary>Data source bindings for this tenant's APIs and services.</summary>
    public Dictionary<string, string> DataSources { get; set; } = new();

    /// <summary>Roles and their assigned dashboard permissions.</summary>
    public Dictionary<string, DashboardRolePermissions> RolePermissions { get; set; } = new();

    /// <summary>Cache duration in seconds (0 = no cache, -1 = indefinite).</summary>
    public int CacheDurationSeconds { get; set; } = 300;

    /// <summary>Indicates if this is the default tenant configuration.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Timestamp of last update.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Sidebar configuration for a tenant.</summary>
public class SidebarConfig
{
    /// <summary>Sidebar width in pixels or CSS units.</summary>
    public string Width { get; set; } = "250px";

    /// <summary>Background color (inherits from tenant theme if not set).</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Text color for sidebar items.</summary>
    public string? TextColor { get; set; }

    /// <summary>Whether sidebar is collapsible.</summary>
    public bool Collapsible { get; set; } = true;

    /// <summary>Default collapsed state on first load.</summary>
    public bool DefaultCollapsed { get; set; } = false;

    /// <summary>Show/hide tenant switcher in sidebar.</summary>
    public bool ShowTenantSwitcher { get; set; } = false;

    /// <summary>Show/hide user profile section.</summary>
    public bool ShowUserProfile { get; set; } = true;

    /// <summary>Custom sidebar items (beyond navigation).</summary>
    public List<SidebarItem> CustomItems { get; set; } = new();
}

/// <summary>Custom sidebar item (e.g., help widget, notifications).</summary>
public class SidebarItem
{
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? IconClass { get; set; }
    public string? ContentUrl { get; set; }
    public string? ComponentName { get; set; }
    public int Order { get; set; }
}

/// <summary>Header configuration for a tenant.</summary>
public class HeaderConfig
{
    /// <summary>Header height in pixels or CSS units.</summary>
    public string Height { get; set; } = "60px";

    /// <summary>Background color (inherits from tenant theme if not set).</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Show/hide search bar in header.</summary>
    public bool ShowSearchBar { get; set; } = true;

    /// <summary>Show/hide notifications icon.</summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>Show/hide theme toggle.</summary>
    public bool ShowThemeToggle { get; set; } = true;

    /// <summary>Custom header items (breadcrumbs, status indicators).</summary>
    public List<HeaderItem> CustomItems { get; set; } = new();
}

/// <summary>Custom header item.</summary>
public class HeaderItem
{
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? IconClass { get; set; }
    public string? ComponentName { get; set; }
    public int Order { get; set; }
}

/// <summary>Footer configuration for a tenant.</summary>
public class FooterConfig
{
    /// <summary>Show/hide footer.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Copyright text.</summary>
    public string? CopyrightText { get; set; }

    /// <summary>Footer links (privacy, terms, support).</summary>
    public List<FooterLink> Links { get; set; } = new();

    /// <summary>Footer background color.</summary>
    public string? BackgroundColor { get; set; }
}

/// <summary>Footer link.</summary>
public class FooterLink
{
    public string Label { get; set; } = null!;
    public string Url { get; set; } = null!;
    public bool OpenInNewTab { get; set; } = false;
}

/// <summary>Dashboard region configuration.</summary>
public class DashboardRegionConfig
{
    /// <summary>Unique region identifier (e.g., "main", "sidebar", "topbar").</summary>
    public string Id { get; set; } = null!;

    /// <summary>Display name for this region.</summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>CSS grid or flex layout properties.</summary>
    public string LayoutClass { get; set; } = "region-default";

    /// <summary>Razor component name to render in this region.</summary>
    public string? ComponentName { get; set; }

    /// <summary>Available widget types for this region.</summary>
    public List<string> AllowedWidgets { get; set; } = new();

    /// <summary>Default widgets for this region (populated on first load).</summary>
    public List<WidgetConfig> DefaultWidgets { get; set; } = new();

    /// <summary>Whether this region is editable by tenants.</summary>
    public bool Editable { get; set; } = false;
}

/// <summary>Widget configuration for a dashboard region.</summary>
public class WidgetConfig
{
    /// <summary>Unique widget identifier.</summary>
    public string Id { get; set; } = null!;

    /// <summary>Widget type (e.g., "metric_card", "chart", "table").</summary>
    public string Type { get; set; } = null!;

    /// <summary>Widget display name.</summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>Razor component name to render.</summary>
    public string ComponentName { get; set; } = null!;

    /// <summary>Data source binding (maps to TenantDashboardConfig.DataSources).</summary>
    public string? DataSourceKey { get; set; }

    /// <summary>Widget size/grid span.</summary>
    public string Size { get; set; } = "1x1";

    /// <summary>Order within region.</summary>
    public int Order { get; set; }

    /// <summary>Custom configuration JSON for the widget.</summary>
    public Dictionary<string, object>? CustomConfig { get; set; }

    /// <summary>Whether this widget is visible by default.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Required capabilities to view this widget.</summary>
    public List<string> RequiredCapabilities { get; set; } = new();
}

/// <summary>Navigation menu item.</summary>
public class NavigationItem
{
    public string Id { get; set; } = null!;

    public string Label { get; set; } = null!;

    public string? Href { get; set; }

    public string? IconClass { get; set; }

    public int Order { get; set; }

    /// <summary>Required capabilities to view this menu item.</summary>
    public List<string> RequiredCapabilities { get; set; } = new();

    /// <summary>Child menu items for submenus.</summary>
    public List<NavigationItem> Children { get; set; } = new();

    /// <summary>Feature flag that must be enabled to show this item.</summary>
    public string? FeatureFlag { get; set; }
}

/// <summary>Dashboard role permissions.</summary>
public class DashboardRolePermissions
{
    /// <summary>Role name.</summary>
    public string Role { get; set; } = null!;

    /// <summary>Accessible dashboard pages for this role.</summary>
    public List<string> AccessiblePages { get; set; } = new();

    /// <summary>Accessible widgets for this role.</summary>
    public List<string> AccessibleWidgets { get; set; } = new();

    /// <summary>Editable widgets for this role.</summary>
    public List<string> EditableWidgets { get; set; } = new();

    /// <summary>Can this role personalize the dashboard (reorder, hide widgets).</summary>
    public bool CanPersonalize { get; set; } = true;

    /// <summary>Can this role export data from widgets.</summary>
    public bool CanExport { get; set; } = false;

    /// <summary>Can this role access audit logs.</summary>
    public bool CanAccessAuditLogs { get; set; } = false;
}
