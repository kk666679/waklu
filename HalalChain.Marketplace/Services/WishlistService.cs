namespace HalalChain.Marketplace.Services;

public interface IWishlistService
{
    HashSet<Guid> Items { get; }
    event Action? OnChange;
    void Toggle(Guid productId);
    bool Contains(Guid productId);
}

public sealed class WishlistService : IWishlistService
{
    private readonly HashSet<Guid> _items = new();
    public HashSet<Guid> Items => _items;
    public event Action? OnChange;

    public void Toggle(Guid productId)
    {
        if (!_items.Remove(productId)) _items.Add(productId);
        OnChange?.Invoke();
    }

    public bool Contains(Guid productId) => _items.Contains(productId);
}
