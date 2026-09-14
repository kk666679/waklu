namespace HalalChain.Models;

public class Wishlist
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Product? Product { get; set; }
}
