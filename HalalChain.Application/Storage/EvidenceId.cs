namespace HalalChain.Application.Storage;

public readonly record struct EvidenceId(Guid Value)
{
    public static EvidenceId New() => new(Guid.NewGuid());
    public static EvidenceId FromString(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("D");
}
