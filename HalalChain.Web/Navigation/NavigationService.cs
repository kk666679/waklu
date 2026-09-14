using HalalChain.Platform.Contracts.Auth;
using HalalChain.Services;

namespace HalalChain.Navigation;

public interface IPermissionNavigationFilter
{
    bool CanAccess(NavigationItem item);
}

public class PermissionNavigationFilter : IPermissionNavigationFilter
{
    private readonly IAuthService _auth;
    private readonly IUserContext _user;

    public PermissionNavigationFilter(IAuthService auth, IUserContext user)
    {
        _auth = auth;
        _user = user;
    }

    public bool CanAccess(NavigationItem item)
    {
        if (!item.Visible) return false;

        if (!string.IsNullOrEmpty(item.RequiredRole) && !_user.Roles.Contains(item.RequiredRole))
            return false;

        if (!string.IsNullOrEmpty(item.RequiredPermission) &&
            !_user.Permissions.Contains(item.RequiredPermission, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }
}

public class NavigationService
{
    private readonly IPermissionNavigationFilter _filter;
    private readonly List<NavigationItem> _allItems = new();

    public NavigationService(IPermissionNavigationFilter filter)
    {
        _filter = filter;
    }

    public void RegisterItems(IEnumerable<NavigationItem> items)
    {
        _allItems.Clear();
        _allItems.AddRange(items);
    }

    public List<NavigationItem> GetVisibleItems() =>
        _allItems.Where(_filter.CanAccess).ToList();

    public List<NavigationItem> GetItemsForRole(string role)
    {
        var items = new List<NavigationItem>();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(item.RequiredRole) || item.RequiredRole == role)
                items.Add(item);
        }
        return items;
    }
}
