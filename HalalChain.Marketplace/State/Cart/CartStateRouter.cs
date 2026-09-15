using HalalChain.Marketplace.State.Abstractions;
using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Contracts.Commerce.Dto;
using Microsoft.Extensions.Logging;

namespace HalalChain.Marketplace.State.Cart;

public sealed class CartStateRouter(
    ICurrentUserAccessor user,
    CartStateInMemory local,
    CartStateApi server,
    ILogger<CartStateRouter> log) : ICartState
{
    public event Action? OnChange
    {
        add    { local.OnChange += value; server.OnChange += value; }
        remove { local.OnChange -= value; server.OnChange -= value; }
    }

    private ICartState Active => user.UserId is null ? local : server;

    public CartDto? Current => Active.Current;
    public int ItemCount => Active.ItemCount;
    public decimal Subtotal => Active.Subtotal;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await Active.InitializeAsync(ct);
        log.LogDebug("Cart initialized → {Backend}",
            user.UserId is null ? "local" : "server");
    }

    public Task AddAsync(Guid productId, int qty, CancellationToken ct = default)
        => Active.AddAsync(productId, qty, ct);

    public Task UpdateAsync(Guid productId, int qty, CancellationToken ct = default)
        => Active.UpdateAsync(productId, qty, ct);

    public Task RemoveAsync(Guid productId, CancellationToken ct = default)
        => Active.RemoveAsync(productId, ct);

    public Task ClearAsync(CancellationToken ct = default)
        => Active.ClearAsync(ct);

    public async Task MergeLocalIntoServerAsync(CancellationToken ct = default)
    {
        if (user.UserId is null) return;
        var guestItems = local.Current?.Items ?? [];
        if (guestItems.Length == 0) return;

        foreach (var item in guestItems)
            await server.AddAsync(item.ProductId, item.Quantity, ct);

        await local.ClearAsync(ct);
        log.LogInformation("Merged {Count} guest cart items into server cart", guestItems.Length);
    }

    public ValueTask DisposeAsync() => local.DisposeAsync();
}