using HalalChain.Models;
using HalalChain.Models.Admin;

namespace HalalChain.Services;

public class AdminService : IAdminService
{
    private readonly List<AdminRole> _roles = new();
    private readonly List<AdminMenuItem> _menu = new();
    private readonly List<SystemSetting> _settings = new();
    private int _nextId = 1;

    public AdminService()
    {
        _roles.Add(new AdminRole { Id = _nextId++, Name = "SuperAdmin", IsSystem = true });
        _roles.Add(new AdminRole { Id = _nextId++, Name = "CatalogManager" });
        _roles.Add(new AdminRole { Id = _nextId++, Name = "SupportAgent" });

        _menu.Add(new AdminMenuItem { Id = 1, Title = "Dashboard", Url = "/admin", Icon = "dashboard" });
        _menu.Add(new AdminMenuItem { Id = 2, Title = "Catalog", Url = "/admin/catalog", Icon = "category" });
        _menu.Add(new AdminMenuItem { Id = 3, Title = "Sales", Url = "/admin/sales", Icon = "shopping_cart" });
        _menu.Add(new AdminMenuItem { Id = 4, Title = "Customers", Url = "/admin/customers", Icon = "people" });
        _menu.Add(new AdminMenuItem { Id = 5, Title = "Marketing", Url = "/admin/marketing", Icon = "local_offer" });
        _menu.Add(new AdminMenuItem { Id = 6, Title = "Reports", Url = "/admin/reports", Icon = "assessment" });
        _menu.Add(new AdminMenuItem { Id = 7, Title = "System", Url = "/admin/system", Icon = "settings" });

        _settings.Add(new SystemSetting { Id = _nextId++, Key = "site.name", Value = "HalalChain", Group = "General" });
        _settings.Add(new SystemSetting { Id = _nextId++, Key = "site.currency", Value = "USD", Group = "General" });
        _settings.Add(new SystemSetting { Id = _nextId++, Key = "tax.rate", Value = "0.08", Group = "Commerce" });
    }

    public Task<IReadOnlyList<AdminRole>> GetRolesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AdminRole>>(_roles.ToList());

    public Task<AdminRole> CreateRoleAsync(AdminRole role, CancellationToken ct = default)
    {
        role.Id = _nextId++;
        _roles.Add(role);
        return Task.FromResult(role);
    }

    public Task<AdminRole> UpdateRoleAsync(AdminRole role, CancellationToken ct = default)
    {
        var existing = _roles.FirstOrDefault(r => r.Id == role.Id);
        if (existing is null) return Task.FromResult(role);
        existing.Name = role.Name;
        existing.Description = role.Description;
        return Task.FromResult(existing);
    }

    public Task DeleteRoleAsync(int id, CancellationToken ct = default)
    {
        _roles.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AdminMenuItem>> GetMenuAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AdminMenuItem>>(_menu.ToList());

    public Task<IReadOnlyList<SystemSetting>> GetSettingsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SystemSetting>>(_settings.ToList());

    public Task<SystemSetting> UpdateSettingAsync(SystemSetting setting, CancellationToken ct = default)
    {
        var existing = _settings.FirstOrDefault(s => s.Key == setting.Key);
        if (existing is not null) { existing.Value = setting.Value; existing.UpdatedAt = DateTime.UtcNow; return Task.FromResult(existing); }
        _settings.Add(setting);
        return Task.FromResult(setting);
    }
}
