namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

public sealed class Brand : AggregateRoot<BrandId>
{
    public string Name { get; private set; } = string.Empty;
    public string? LogoContentHash { get; private set; }

    private Brand() { }

    public static Brand Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Brand { Id = BrandId.New(), Name = name };
    }
}
