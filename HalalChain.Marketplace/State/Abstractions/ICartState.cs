using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Marketplace.State.Abstractions;

public interface ICartState : IAsyncDisposable
{
    event Action? OnChange;
    CartDto? Current { get; }
    int ItemCount { get; }
    decimal Subtotal { get; }

    Task InitializeAsync(CancellationToken ct = default);
    Task AddAsync(Guid productId, int qty, CancellationToken ct = default);
    Task UpdateAsync(Guid productId, int qty, CancellationToken ct = default);
    Task RemoveAsync(Guid productId, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    Task MergeLocalIntoServerAsync(CancellationToken ct = default);
}