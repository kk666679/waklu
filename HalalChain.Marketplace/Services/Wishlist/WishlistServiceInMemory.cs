using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Marketplace.Services.Wishlist;

public sealed class WishlistServiceInMemory : IWishlistService
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

    public Task<Result<IReadOnlyList<ProductDto>>> ListAsync(CancellationToken ct = default)
        => Task.FromResult(Result<IReadOnlyList<ProductDto>>.Ok(Array.Empty<ProductDto>()));

    public Task<Result> AddAsync(Guid productId, CancellationToken ct = default)
    {
        _items.Add(productId);
        OnChange?.Invoke();
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default)
    {
        _items.Remove(productId);
        OnChange?.Invoke();
        return Task.FromResult(Result.Ok());
    }

    public Task<bool> ContainsAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult(_items.Contains(productId));
}