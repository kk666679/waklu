namespace HalalChain.Models.Admin;

public class AdminMenuItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public int? RequiredPermissionId { get; set; }
    public List<AdminMenuItem> Children { get; set; } = new();
}
