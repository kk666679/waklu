using HalalChain.Domain.Commerce;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for CartItem queries and persistence.
/// Manages customer shopping carts.
/// </summary>
public interface ICartRepository
{
    /// <summary>Get a cart item by ID.</summary>
    Task<CartItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get all cart items for a customer.</summary>
    Task<IEnumerable<CartItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Get a specific cart item for a customer and product (unique constraint).</summary>
    Task<CartItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Get all cart items (with pagination).</summary>
    Task<IEnumerable<CartItem>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

    /// <summary>Get cart item count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Get cart item count for a customer.</summary>
    Task<int> GetCountByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Save a new cart item.</summary>
    Task AddAsync(CartItem cartItem, CancellationToken ct = default);

    /// <summary>Update an existing cart item (e.g., quantity change).</summary>
    Task UpdateAsync(CartItem cartItem, CancellationToken ct = default);

    /// <summary>Delete a cart item.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Clear all cart items for a customer (used after checkout).</summary>
    Task ClearAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Check if a cart item exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a product is in a customer's cart.</summary>
    Task<bool> ExistsInCartAsync(Guid customerId, Guid productId, CancellationToken ct = default);
}

/// <summary>
/// Repository for WishlistItem queries and persistence.
/// Manages customer wishlist/favorites.
/// </summary>
public interface IWishlistRepository
{
    /// <summary>Get a wishlist item by ID.</summary>
    Task<WishlistItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get all wishlist items for a customer.</summary>
    Task<IEnumerable<WishlistItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Get a specific wishlist item for a customer and product (unique constraint).</summary>
    Task<WishlistItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    /// <summary>Get all wishlist items (with pagination).</summary>
    Task<IEnumerable<WishlistItem>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

    /// <summary>Get wishlist item count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Get wishlist item count for a customer.</summary>
    Task<int> GetCountByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Save a new wishlist item.</summary>
    Task AddAsync(WishlistItem wishlistItem, CancellationToken ct = default);

    /// <summary>Delete a wishlist item.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a wishlist item exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a product is in a customer's wishlist.</summary>
    Task<bool> ExistsInWishlistAsync(Guid customerId, Guid productId, CancellationToken ct = default);
}
