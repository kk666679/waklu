namespace HalalChain.Services;

public class WishlistService : IWishlistService
{
    private readonly List<Guid> _productIds = new();
    public IReadOnlyList<Guid> ProductIds => _productIds.AsReadOnly();
    public bool IsInWishlist(Guid productId) => _productIds.Contains(productId);
    public void AddToWishlist(Guid productId, string title, decimal price, string currency)
    {
        if (!_productIds.Contains(productId)) _productIds.Add(productId);
    }
    public void RemoveFromWishlist(Guid productId) => _productIds.Remove(productId);
}
