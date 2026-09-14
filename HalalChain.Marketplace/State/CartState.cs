using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Http.Models;
using HalalChain.Platform.Http.Services;

namespace HalalChain.Marketplace.State;

public sealed class CartState
{
    private readonly IPlatformApiClient _api;

    public CartState(IPlatformApiClient api) => _api = api;

    public event Action? OnChange;

    public CartDto? Cart { get; private set; }
    public bool IsLoading { get; private set; }
    public int ItemCount => Cart?.Items.Sum(i => i.Quantity) ?? 0;
    public decimal Subtotal => Cart?.Subtotal ?? 0m;
    public string Currency => Cart?.Currency ?? "MYR";

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Notify();
        try
        {
            var result = await _api.GetCartAsync(ct);
            Cart = result.IsSuccess ? result.Data : null;
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    public async Task<bool> AddAsync(Guid productId, int quantity = 1, CancellationToken ct = default)
    {
        var result = await _api.AddToCartAsync(productId, quantity, ct);
        if (result.IsSuccess) await LoadAsync(ct);
        return result.IsSuccess;
    }

    public async Task UpdateAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var result = await _api.UpdateCartAsync(productId, quantity, ct);
        if (result.IsSuccess) await LoadAsync(ct);
    }

    public async Task RemoveAsync(Guid productId, CancellationToken ct = default)
    {
        var result = await _api.UpdateCartAsync(productId, 0, ct);
        if (result.IsSuccess) await LoadAsync(ct);
    }

    private void Notify() => OnChange?.Invoke();
}
