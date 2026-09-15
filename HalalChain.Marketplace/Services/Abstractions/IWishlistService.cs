using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Marketplace.Services.Abstractions;

public interface IWishlistService
{
    // Legacy sync members (for backward compatibility with existing components)
    HashSet<Guid> Items { get; }
    event Action? OnChange;
    void Toggle(Guid productId);
    bool Contains(Guid productId);

    // New async API-backed members
    Task<Result<IReadOnlyList<ProductDto>>> ListAsync(CancellationToken ct = default);
    Task<Result> AddAsync(Guid productId, CancellationToken ct = default);
    Task<Result> RemoveAsync(Guid productId, CancellationToken ct = default);
    Task<bool> ContainsAsync(Guid productId, CancellationToken ct = default);
}