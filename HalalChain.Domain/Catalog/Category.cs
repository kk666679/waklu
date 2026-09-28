namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

public sealed class Category : AggregateRoot<CategoryId>
{
    public string Name { get; private set; } = string.Empty;
    public CategoryId? ParentId { get; private set; }

    private Category() { }

    public static Category Create(string name, CategoryId? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { Id = CategoryId.New(), Name = name, ParentId = parentId };
    }
}
