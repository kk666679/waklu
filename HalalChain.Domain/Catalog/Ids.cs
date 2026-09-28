namespace HalalChain.Domain.Catalog;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct ProductVariantId(Guid Value)
{
    public static ProductVariantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct BrandId(Guid Value)
{
    public static BrandId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct ProductMediaAssetId(Guid Value)
{
    public static ProductMediaAssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
