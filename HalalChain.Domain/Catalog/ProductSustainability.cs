namespace HalalChain.Domain.Catalog;

public sealed record ProductSustainability(
    bool RecyclablePackaging,
    bool CarbonNeutralShipping,
    string? CertificationBody,
    string? CertificationNumber);
