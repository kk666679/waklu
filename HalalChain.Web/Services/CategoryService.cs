using HalalChain.Models;

namespace HalalChain.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category> CreateAsync(Category c, CancellationToken ct = default);
    Task<Category> UpdateAsync(Category c, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly List<Category> _items = new();
    private int _nextId = 1;

    public CategoryService()
    {
        _items.Add(new Category { Id = _nextId++, Name = "Meat & Poultry", DisplayOrder = 1 });
        _items.Add(new Category { Id = _nextId++, Name = "Spices", DisplayOrder = 2 });
        _items.Add(new Category { Id = _nextId++, Name = "Dates", DisplayOrder = 3 });
        _items.Add(new Category { Id = _nextId++, Name = "Dairy", DisplayOrder = 4 });
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Category>>(_items.ToList());

    public Task<Category> CreateAsync(Category c, CancellationToken ct = default)
    {
        c.Id = _nextId++; _items.Add(c); return Task.FromResult(c);
    }
    public Task<Category> UpdateAsync(Category c, CancellationToken ct = default)
    {
        var ex = _items.FirstOrDefault(x => x.Id == c.Id);
        if (ex is null) return Task.FromResult(c);
        ex.Name = c.Name; ex.DisplayOrder = c.DisplayOrder; ex.IsActive = c.IsActive;
        return Task.FromResult(ex);
    }
    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }
}
