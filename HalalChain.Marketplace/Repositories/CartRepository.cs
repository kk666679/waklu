using HalalChain.Domain.Commerce;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of ICartRepository using EF Core against PlatformDbContext.</summary>
public class CartRepository : ICartRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<CartRepository> _logger;

    public CartRepository(PlatformDbContext context, ILogger<CartRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart item by ID: {CartItemId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CartItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems
                .Where(c => c.CustomerId == customerId)
                .Include(c => c.Product)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart items for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CartItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart item for customer {CustomerId} and product {ProductId}", customerId, productId);
            throw;
        }
    }

    public async Task<IEnumerable<CartItem>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all cart items");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting cart items");
            throw;
        }
    }

    public async Task<int> GetCountByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems.CountAsync(c => c.CustomerId == customerId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting cart items for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task AddAsync(CartItem cartItem, CancellationToken ct = default)
    {
        try
        {
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Cart item added: {CartItemId}", cartItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cart item: {CartItemId}", cartItem.Id);
            throw;
        }
    }

    public async Task UpdateAsync(CartItem cartItem, CancellationToken ct = default)
    {
        try
        {
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Cart item updated: {CartItemId}", cartItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {CartItemId}", cartItem.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Cart item deleted: {CartItemId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cart item: {CartItemId}", id);
            throw;
        }
    }

    public async Task ClearAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var items = await _context.CartItems
                .Where(c => c.CustomerId == customerId)
                .ToListAsync(ct);

            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Cleared cart for customer: {CustomerId}", customerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems.AnyAsync(c => c.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cart item existence: {CartItemId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsInCartAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.CartItems
                .AnyAsync(c => c.CustomerId == customerId && c.ProductId == productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product exists in cart for customer: {CustomerId}, Product: {ProductId}", customerId, productId);
            throw;
        }
    }
}

/// <summary>Implementation of IWishlistRepository using EF Core against PlatformDbContext.</summary>
public class WishlistRepository : IWishlistRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<WishlistRepository> _logger;

    public WishlistRepository(PlatformDbContext context, ILogger<WishlistRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WishlistItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems.FirstOrDefaultAsync(w => w.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist item by ID: {WishlistItemId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<WishlistItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems
                .Where(w => w.CustomerId == customerId)
                .Include(w => w.Product)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist items for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<WishlistItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.ProductId == productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist item for customer {CustomerId} and product {ProductId}", customerId, productId);
            throw;
        }
    }

    public async Task<IEnumerable<WishlistItem>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                .OrderByDescending(w => w.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all wishlist items");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting wishlist items");
            throw;
        }
    }

    public async Task<int> GetCountByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems.CountAsync(w => w.CustomerId == customerId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting wishlist items for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task AddAsync(WishlistItem wishlistItem, CancellationToken ct = default)
    {
        try
        {
            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Wishlist item added: {WishlistItemId}", wishlistItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding wishlist item: {WishlistItemId}", wishlistItem.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.Id == id, ct);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Wishlist item deleted: {WishlistItemId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting wishlist item: {WishlistItemId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems.AnyAsync(w => w.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist item existence: {WishlistItemId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsInWishlistAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.CustomerId == customerId && w.ProductId == productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product exists in wishlist for customer: {CustomerId}, Product: {ProductId}", customerId, productId);
            throw;
        }
    }
}
