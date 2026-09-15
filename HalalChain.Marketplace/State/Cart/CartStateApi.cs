using HalalChain.Marketplace.State.Abstractions;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.Marketplace.State.Cart;

public sealed class CartStateApi(IApiClient api, ILogger<CartStateApi> log) : ICartState
{
    public event Action? OnChange;
    public CartDto? Current { get; private set; }
    public int ItemCount => Current?.Items.Sum(i => i.Quantity) ?? 0;
    public decimal Subtotal => Current?.Subtotal ?? 0m;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var r = await api.GetAsync<CartDto>("/api/v1/commerce/cart", ct);
        if (r.IsSuccess) { Current = r.Value; OnChange?.Invoke(); }
        else log.LogWarning("Cart init failed: {Code}", r.Error!.Code);
    }

    public async Task AddAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        var r = await api.PostAsync<CartDto>("/api/v1/commerce/cart/items",
            new { productId, quantity = qty }, ct: ct);
        if (r.IsSuccess) { Current = r.Value; OnChange?.Invoke(); }
    }

    public async Task UpdateAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        var r = await api.PatchAsync<CartDto>("/api/v1/commerce/cart/items",
            new { productId, quantity = qty }, ct);
        if (r.IsSuccess) { Current = r.Value; OnChange?.Invoke(); }
    }

    public async Task RemoveAsync(Guid productId, CancellationToken ct = default)
    {
        var r = await api.DeleteAsync($"/api/v1/commerce/cart/items?productId={productId}", ct);
        if (r.IsSuccess) await InitializeAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        var r = await api.DeleteAsync("/api/v1/commerce/cart", ct);
        if (r.IsSuccess) { Current = null; OnChange?.Invoke(); }
    }

    public Task MergeLocalIntoServerAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}