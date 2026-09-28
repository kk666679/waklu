namespace HalalChain.Domain.Catalog;

/// <summary>
/// Halal-related enums that belong to the Catalog domain's view of a
/// product. Verdict-related types live in VerdictBinding.cs. Certificate
/// types live in Halal/Certificate.cs.
/// </summary>
public enum HalalStatus
{
    Unknown,
    Pending,
    Certified,
    Expired,
    Revoked,
}

public enum CertificationBody
{
    JAKIM,
    MUI,
    MUIS,
    IFANCA,
    HFA,
    Other,
}
