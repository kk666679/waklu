using HalalChain.Models;

namespace HalalChain.Services;

public class ProductService : IProductService
{
    private readonly List<Product> _products = new();
    private int _nextId = 1;

    public ProductService()
    {
        for (var i = 1; i <= 12; i++)
        {
            _products.Add(new Product
            {
                Id = _nextId++,
                Name = $"Sample Halal Product {i}",
                Sku = $"SKU-{1000 + i}",
                Description = "Premium halal-certified product.",
                Price = 9.99m + i,
                StockQuantity = 50,
                CategoryId = (i % 4) + 1,
                VendorId = (i % 3) + 1,
                IsHalal = true,
                IsActive = true
            });
        }
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Product>>(_products.ToList());

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> SearchAsync(string? query, int? categoryId, CancellationToken ct = default)
    {
        IEnumerable<Product> q = _products;
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);
        return Task.FromResult<IReadOnlyList<Product>>(q.ToList());
    }

    public Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        product.Id = _nextId++;
        product.CreatedAt = DateTime.UtcNow;
        _products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing is null) return Task.FromResult(product);
        existing.Name = product.Name;
        existing.Sku = product.Sku;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.SalePrice = product.SalePrice;
        existing.StockQuantity = product.StockQuantity;
        existing.CategoryId = product.CategoryId;
        existing.IsActive = product.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(existing);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _products.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}
