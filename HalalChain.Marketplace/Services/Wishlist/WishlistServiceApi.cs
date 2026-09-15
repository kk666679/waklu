using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Marketplace.Services.Wishlist;

public sealed class WishlistServiceApi(IApiClient api) : IWishlistService
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
        => api.GetAsync<IReadOnlyList<ProductDto>>("/api/v1/wishlist", ct);

    public Task<Result> AddAsync(Guid productId, CancellationToken ct = default)
    {
        _items.Add(productId);
        OnChange?.Invoke();
        return api.PostAsync($"/api/v1/wishlist/{productId}", new { }, ct: ct);
    }

    public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default)
    {
        _items.Remove(productId);
        OnChange?.Invoke();
        return api.DeleteAsync($"/api/v1/wishlist/{productId}", ct);
    }

    public async Task<bool> ContainsAsync(Guid productId, CancellationToken ct = default)
    {
        var r = await ListAsync(ct);
        return r.IsSuccess && r.Value!.Any(p => p.Id == productId);
    }
}