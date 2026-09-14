namespace HalalChain.Models;

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
