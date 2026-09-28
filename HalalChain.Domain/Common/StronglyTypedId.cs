namespace HalalChain.Domain.Common;

/// <summary>
/// Base for strongly-typed IDs. Prevents passing a VendorId where a
/// ProductId is expected.
/// </summary>
public readonly record struct StronglyTypedId<T>(Guid Value) where T : struct
{
    public override string ToString() => Value.ToString("D");
}
