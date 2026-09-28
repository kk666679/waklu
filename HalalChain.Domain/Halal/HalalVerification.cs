namespace HalalChain.Domain.Halal;

using HalalChain.Domain.Common;

/// <summary>
/// A record of an evidence review performed against a certificate.
///
/// This is the platform's record of what the agents collected and what
/// tawheed concluded. It is the join between evidence and verdict, and
/// it is auditable.
///
/// HalalVerification does NOT compute a verdict. It records one.
/// </summary>
public sealed class HalalVerification : AggregateRoot<HalalVerificationId>
{
    public CertificateId CertificateId { get; private init; }
    public Guid VendorId { get; private init; }
    public string PolicyVersion { get; private init; } = string.Empty;
    public VerdictState Result { get; private init; }
    public IReadOnlyList<Guid> EvidenceIds { get; private init; } = [];
    public string? TraceHash { get; private init; }
    public DateTimeOffset VerifiedAt { get; private init; }

    private HalalVerification() { }

    public static HalalVerification Record(
        CertificateId certificateId,
        Guid vendorId,
        string policyVersion,
        VerdictState result,
        IReadOnlyList<Guid> evidenceIds,
        string? traceHash,
        DateTimeOffset now) => new()
    {
        Id = HalalVerificationId.New(),
        CertificateId = certificateId,
        VendorId = vendorId,
        PolicyVersion = policyVersion,
        Result = result,
        EvidenceIds = evidenceIds,
        TraceHash = traceHash,
        VerifiedAt = now,
    };
}
