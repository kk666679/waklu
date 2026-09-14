using HalalChain.Models;

namespace HalalChain.Services;

public interface IReviewService
{
    Task<IReadOnlyList<Review>> GetForProductAsync(int productId, CancellationToken ct = default);
    Task<Review> CreateAsync(Review review, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ApproveAsync(int id, CancellationToken ct = default);
}
