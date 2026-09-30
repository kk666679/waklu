using HalalChain.Domain.Commerce;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository for Order aggregate root queries and persistence.
/// Manages customer orders and their vendor order splits.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Get an order by ID with all vendor orders and items.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get all orders for a customer.</summary>
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get orders by status (Pending, Paid, Shipped, Delivered, Cancelled).</summary>
    Task<IEnumerable<Order>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get recent orders (newest first).</summary>
    Task<IEnumerable<Order>> GetRecentAsync(int take = 20, CancellationToken ct = default);

    /// <summary>Get all orders (with pagination).</summary>
    Task<IEnumerable<Order>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get total order count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Get order count by status.</summary>
    Task<int> GetCountByStatusAsync(string status, CancellationToken ct = default);

    /// <summary>Save a new order.</summary>
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Update an existing order.</summary>
    Task UpdateAsync(Order order, CancellationToken ct = default);

    /// <summary>Delete an order (soft delete recommended).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if an order exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get total revenue (sum of all order totals).</summary>
    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);

    /// <summary>Get total revenue for a date range.</summary>
    Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>Get orders within a date range.</summary>
    Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

/// <summary>
/// Repository for VendorOrder queries and persistence.
/// Manages order splits by vendor (one VendorOrder per vendor per Order).
/// </summary>
public interface IVendorOrderRepository
{
    /// <summary>Get a vendor order by ID with all items.</summary>
    Task<VendorOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get all vendor orders for a specific order.</summary>
    Task<IEnumerable<VendorOrder>> GetByOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Get all vendor orders for a specific vendor.</summary>
    Task<IEnumerable<VendorOrder>> GetByVendorAsync(Guid vendorId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get vendor orders by status (Pending, Paid, Shipped, etc.).</summary>
    Task<IEnumerable<VendorOrder>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get pending vendor orders awaiting fulfillment.</summary>
    Task<IEnumerable<VendorOrder>> GetPendingAsync(int take = 50, CancellationToken ct = default);

    /// <summary>Get all vendor orders (with pagination).</summary>
    Task<IEnumerable<VendorOrder>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get vendor order count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Save a new vendor order.</summary>
    Task AddAsync(VendorOrder vendorOrder, CancellationToken ct = default);

    /// <summary>Update an existing vendor order.</summary>
    Task UpdateAsync(VendorOrder vendorOrder, CancellationToken ct = default);

    /// <summary>Delete a vendor order.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Check if a vendor order exists by ID.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
