using HalalChain.Models;

namespace HalalChain.Services;

public interface IWishlistService
{
    bool IsInWishlist(Guid productId);
    void AddToWishlist(Guid productId, string title, decimal price, string currency);
    void RemoveFromWishlist(Guid productId);
    IReadOnlyList<Guid> ProductIds { get; }
}
