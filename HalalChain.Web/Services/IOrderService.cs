using HalalChain.Models;

namespace HalalChain.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
    Task<Order> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
