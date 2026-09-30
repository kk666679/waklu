using HalalChain.Domain.Commerce;
using HalalChain.Marketplace.Data;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Marketplace.Repositories;

/// <summary>Implementation of IOrderRepository using EF Core against PlatformDbContext.</summary>
public class OrderRepository : IOrderRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(PlatformDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.VendorOrders)
                .ThenInclude(vo => vo.Items)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order by ID: {OrderId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.VendorOrders)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for customer: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.Status == status)
                .Include(o => o.VendorOrders)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetRecentAsync(int take = 20, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.VendorOrders)
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent orders");
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.VendorOrders)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting orders");
            throw;
        }
    }

    public async Task<int> GetCountByStatusAsync(string status, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders.CountAsync(o => o.Status == status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting orders by status: {Status}", status);
            throw;
        }
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        try
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Order added: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding order: {OrderId}", order.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        try
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Order updated: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order: {OrderId}", order.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Order deleted: {OrderId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: {OrderId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders.AnyAsync(o => o.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking order existence: {OrderId}", id);
            throw;
        }
    }

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders.SumAsync(o => o.Total, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total revenue");
            throw;
        }
    }

    public async Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .SumAsync(o => o.Total, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for date range: {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Include(o => o.VendorOrders)
                .ThenInclude(vo => vo.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for date range: {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}

/// <summary>Implementation of IVendorOrderRepository using EF Core against PlatformDbContext.</summary>
public class VendorOrderRepository : IVendorOrderRepository
{
    private readonly PlatformDbContext _context;
    private readonly ILogger<VendorOrderRepository> _logger;

    public VendorOrderRepository(PlatformDbContext context, ILogger<VendorOrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VendorOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Include(vo => vo.Items)
                .FirstOrDefaultAsync(vo => vo.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor order by ID: {VendorOrderId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<VendorOrder>> GetByOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Where(vo => vo.OrderId == orderId)
                .Include(vo => vo.Items)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor orders for order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<IEnumerable<VendorOrder>> GetByVendorAsync(Guid vendorId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Where(vo => vo.VendorId == vendorId)
                .Include(vo => vo.Items)
                .OrderByDescending(vo => vo.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor orders for vendor: {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<IEnumerable<VendorOrder>> GetByStatusAsync(string status, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Where(vo => vo.Status == status)
                .Include(vo => vo.Items)
                .OrderByDescending(vo => vo.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor orders by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<VendorOrder>> GetPendingAsync(int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Where(vo => vo.Status == "Pending")
                .Include(vo => vo.Items)
                .OrderByDescending(vo => vo.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending vendor orders");
            throw;
        }
    }

    public async Task<IEnumerable<VendorOrder>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders
                .Include(vo => vo.Items)
                .OrderByDescending(vo => vo.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all vendor orders");
            throw;
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders.CountAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting vendor orders");
            throw;
        }
    }

    public async Task AddAsync(VendorOrder vendorOrder, CancellationToken ct = default)
    {
        try
        {
            _context.VendorOrders.Add(vendorOrder);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Vendor order added: {VendorOrderId}", vendorOrder.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vendor order: {VendorOrderId}", vendorOrder.Id);
            throw;
        }
    }

    public async Task UpdateAsync(VendorOrder vendorOrder, CancellationToken ct = default)
    {
        try
        {
            _context.VendorOrders.Update(vendorOrder);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Vendor order updated: {VendorOrderId}", vendorOrder.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor order: {VendorOrderId}", vendorOrder.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var vendorOrder = await _context.VendorOrders.FirstOrDefaultAsync(vo => vo.Id == id, ct);
            if (vendorOrder != null)
            {
                _context.VendorOrders.Remove(vendorOrder);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Vendor order deleted: {VendorOrderId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor order: {VendorOrderId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _context.VendorOrders.AnyAsync(vo => vo.Id == id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vendor order existence: {VendorOrderId}", id);
            throw;
        }
    }
}
