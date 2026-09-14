namespace HalalChain.Navigation;

public sealed record NavigationItem(
    string Route,
    string Label,
    string? Icon,
    string? RequiredPermission,
    string? RequiredRole,
    bool FeatureFlag,
    List<NavigationItem>? Children = null,
    bool Visible = true);
