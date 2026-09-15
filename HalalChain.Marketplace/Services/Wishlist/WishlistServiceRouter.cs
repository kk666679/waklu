using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Flags;
using HalalChain.Platform.Http.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.Marketplace.Services.Wishlist;

public sealed class WishlistServiceRouter(
    IFeatureFlag flags,
    WishlistServiceInMemory inMemory,
    WishlistServiceApi api,
    ILogger<WishlistServiceRouter> log) : IWishlistService
{
    private readonly IWishlistService _inMemory = inMemory;
    private readonly IWishlistService _api = api;

    public HashSet<Guid> Items => Active.Items;
    public event Action? OnChange
    {
        add { _inMemory.OnChange += value; _api.OnChange += value; }
        remove { _inMemory.OnChange -= value; _api.OnChange -= value; }
    }

    public void Toggle(Guid productId) => Active.Toggle(productId);
    public bool Contains(Guid productId) => Active.Contains(productId);

    public Task<Result<IReadOnlyList<ProductDto>>> ListAsync(CancellationToken ct = default)
        => Route(s => s.ListAsync(ct), ct);

    public Task<Result> AddAsync(Guid productId, CancellationToken ct = default)
        => Route(s => s.AddAsync(productId, ct), ct);

    public Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default)
        => Route(s => s.RemoveAsync(productId, ct), ct);

    public Task<bool> ContainsAsync(Guid productId, CancellationToken ct = default)
        => Route(s => s.ContainsAsync(productId, ct), ct);

    private IWishlistService Active => 
        flags.IsEnabled(FlagNames.WishlistApiBacked) ? _api : _inMemory;

    private async Task<T> Route<T>(Func<IWishlistService, Task<T>> work, CancellationToken ct)
    {
        var useApi = await flags.IsEnabledAsync(FlagNames.WishlistApiBacked, ct);
        log.LogDebug("Wishlist → {Backend}", useApi ? "api" : "memory");
        return useApi ? await work(_api) : await work(_inMemory);
    }
}