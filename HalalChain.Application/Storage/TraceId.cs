namespace HalalChain.Application.Storage;

public readonly record struct TraceId(Guid Value)
{
    public static TraceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
