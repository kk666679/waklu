using HalalChain.Models;

namespace HalalChain.Services;

public class ReviewService : IReviewService
{
    private readonly List<Review> _reviews = new();
    private int _nextId = 1;

    public async Task<IReadOnlyList<Review>> GetForProductAsync(int productId, CancellationToken ct = default)
        => await Task.FromResult<IReadOnlyList<Review>>(_reviews.Where(r => r.ProductId == productId).ToList());

    public async Task<Review> CreateAsync(Review review, CancellationToken ct = default)
    {
        review.Id = _nextId++;
        _reviews.Add(review);
        return await Task.FromResult(review);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _reviews.RemoveAll(r => r.Id == id);
        await Task.CompletedTask;
    }

    public async Task ApproveAsync(int id, CancellationToken ct = default)
    {
        var r = _reviews.FirstOrDefault(x => x.Id == id);
        if (r is not null) r.IsApproved = true;
        await Task.CompletedTask;
    }
}
