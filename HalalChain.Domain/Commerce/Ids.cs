namespace HalalChain.Domain.Commerce;

using HalalChain.Domain.Vendors;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct VendorOrderId(Guid Value)
{
    public static VendorOrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct CartItemId(Guid Value)
{
    public static CartItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
