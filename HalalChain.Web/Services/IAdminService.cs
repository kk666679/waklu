using HalalChain.Models;
using HalalChain.Models.Admin;

namespace HalalChain.Services;

public interface IAdminService
{
    Task<IReadOnlyList<AdminRole>> GetRolesAsync(CancellationToken ct = default);
    Task<AdminRole> CreateRoleAsync(AdminRole role, CancellationToken ct = default);
    Task<AdminRole> UpdateRoleAsync(AdminRole role, CancellationToken ct = default);
    Task DeleteRoleAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AdminMenuItem>> GetMenuAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> GetSettingsAsync(CancellationToken ct = default);
    Task<SystemSetting> UpdateSettingAsync(SystemSetting setting, CancellationToken ct = default);
}
