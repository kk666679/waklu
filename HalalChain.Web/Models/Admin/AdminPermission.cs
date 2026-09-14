namespace HalalChain.Models.Admin;

public class AdminPermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Granted { get; set; } = true;
    public AdminRole? Role { get; set; }
}
