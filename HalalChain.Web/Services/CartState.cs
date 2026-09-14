using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Http.Models;
using HalalChain.Platform.Http.Services;

namespace HalalChain.Services;

public class CartState
{
    private readonly IPlatformApiClient _api;
    private readonly ILogger<CartState> _logger;
    public event Action? OnChange;
    public List<CartItem> Items { get; private set; } = [];
    public string Currency { get; private set; } = "MYR";
    public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    public decimal Total => Subtotal;
    public int Count => Items.Sum(i => i.Quantity);

    public CartState(IPlatformApiClient api, ILogger<CartState> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetCartAsync(ct);
            if (result.IsSuccess && result.Data is not null)
            {
                var cart = result.Data;
                Items = cart.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId.ToString(),
                    Title = i.ProductTitle,
                    Price = i.UnitPrice,
                    Currency = i.Currency,
                    Quantity = i.Quantity
                }).ToList();
            }
            else if (!result.IsSuccess &&
                     result.Status != ApiStatus.Unauthorized &&
                     result.Status != ApiStatus.NotFound)
            {
                _logger.LogWarning("RefreshAsync failed: {Status} {Error}", result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error refreshing cart");
        }
        Notify();
    }

    public void AddItem(string productId, string title, decimal price, string currency, int qty = 1)
    {
        _ = AddItemAsync(productId, title, price, currency, qty);
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null) existing.Quantity += qty;
        else Items.Add(new CartItem { ProductId = productId, Title = title, Price = price, Currency = currency, Quantity = qty });
        Notify();
    }

    public async Task AddItemAsync(string productId, string title, decimal price, string currency, int qty = 1, CancellationToken ct = default)
    {
        try
        {
            var request = new AddToCartRequest(Guid.Parse(productId), qty);
            var result = await _api.AddToCartAsync(request, ct);
            if (result.IsSuccess && result.Data is not null)
            {
                var cart = result.Data;
                Items = cart.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId.ToString(),
                    Title = i.ProductTitle,
                    Price = i.UnitPrice,
                    Currency = i.Currency,
                    Quantity = i.Quantity
                }).ToList();
                Notify();
            }
            else
            {
                _logger.LogWarning("AddItemAsync failed: {Status} {Error}", result.Status, result.Error);
                // No silent substitution: keep the optimistic local add so the
                // user gets immediate feedback, but log the failure so the
                // call site can decide to retry or surface an error.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding item to cart");
        }
    }

    public void RemoveItem(string productId)
    {
        _ = RemoveItemAsync(productId);
        Items.RemoveAll(i => i.ProductId == productId);
        Notify();
    }

    public async Task RemoveItemAsync(string productId, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateCartItemRequest(Guid.Parse(productId), 0);
            var result = await _api.UpdateCartItemAsync(request, ct);
            if (result.IsSuccess && result.Data is not null)
            {
                var cart = result.Data;
                Items = cart.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId.ToString(),
                    Title = i.ProductTitle,
                    Price = i.UnitPrice,
                    Currency = i.Currency,
                    Quantity = i.Quantity
                }).ToList();
                Notify();
            }
            else
            {
                _logger.LogWarning("RemoveItemAsync failed: {Status} {Error}", result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error removing cart item");
        }
    }

    public void UpdateQuantity(string productId, int qty)
    {
        _ = UpdateQuantityAsync(productId, qty);
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null) { item.Quantity = Math.Max(0, qty); if (item.Quantity == 0) Items.Remove(item); }
        Notify();
    }

    public async Task UpdateQuantityAsync(string productId, int qty, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateCartItemRequest(Guid.Parse(productId), qty);
            var result = await _api.UpdateCartItemAsync(request, ct);
            if (result.IsSuccess && result.Data is not null)
            {
                var cart = result.Data;
                Items = cart.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId.ToString(),
                    Title = i.ProductTitle,
                    Price = i.UnitPrice,
                    Currency = i.Currency,
                    Quantity = i.Quantity
                }).ToList();
                Notify();
            }
            else
            {
                _logger.LogWarning("UpdateQuantityAsync failed: {Status} {Error}", result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating cart quantity");
        }
    }

    public void Clear()
    {
        _ = ClearAsync();
        Items.Clear();
        Notify();
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            foreach (var item in Items.ToList())
            {
                await RemoveItemAsync(item.ProductId, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error clearing cart");
        }
    }

    public void LoadFromApi(IEnumerable<CartItem> items)
    {
        Items = items.ToList();
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}

public class CartItem
{
    public string ProductId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "MYR";
    public int Quantity { get; set; }
}
