namespace HalalChain.Domain.Vendors;

public readonly record struct VendorId(Guid Value)
{
    public static VendorId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
