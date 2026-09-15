using HalalChain.Marketplace.State.Abstractions;
using HalalChain.Platform.Contracts.Commerce.Dto;
using Microsoft.JSInterop;

namespace HalalChain.Marketplace.State.Cart;

public sealed class CartStateInMemory : ICartState
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "hc-marketplace-cart";
    private CartDto? _cart;

    public CartStateInMemory(IJSRuntime js) => _js = js;

    public event Action? OnChange;
    public CartDto? Current => _cart;
    public int ItemCount => _cart?.Items.Sum(i => i.Quantity) ?? 0;
    public decimal Subtotal => _cart?.Subtotal ?? 0m;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", ct, StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _cart = System.Text.Json.JsonSerializer.Deserialize<CartDto>(json);
            }
        }
        catch { /* ignore */ }
        OnChange?.Invoke();
    }

    private async Task PersistAsync(CancellationToken ct = default)
    {
        if (_cart != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_cart);
            await _js.InvokeVoidAsync("localStorage.setItem", ct, StorageKey, json);
        }
        else
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", ct, StorageKey);
        }
    }

    public Task AddAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        // LocalStorage-based add — simplified
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Guid productId, int qty, CancellationToken ct = default)
    {
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid productId, CancellationToken ct = default)
    {
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _cart = null;
        OnChange?.Invoke();
        return PersistAsync(ct);
    }

    public Task MergeLocalIntoServerAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        _cart = null;
        OnChange = null;
        return ValueTask.CompletedTask;
    }
}