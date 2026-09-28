namespace HalalChain.Domain.Halal;

public readonly record struct CertificateId(Guid Value)
{
    public static CertificateId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct HalalVerificationId(Guid Value)
{
    public static HalalVerificationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
