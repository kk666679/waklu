namespace HalalChain.Models.Admin;

public class AdminRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<AdminPermission> Permissions { get; set; } = new();
}
