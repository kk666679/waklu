namespace HalalChain.Domain.Halal;

/// <summary>Strongly-typed Product identifier.</summary>
public record ProductId(string Value)
{
    public override string ToString() => Value;
    public static ProductId Parse(string value) => new(value);
}

/// <summary>Strongly-typed Certificate identifier.</summary>
public record CertificateId(string Value)
{
    public override string ToString() => Value;
    public static CertificateId Parse(string value) => new(value);
}

/// <summary>Product compliance verdict state.</summary>
public enum VerdictState
{
    Unverified = 0,
    Verified = 1,
    ManualReview = 2,
    Hold = 3,
    NonCompliant = 4,
    Expired = 5
}

/// <summary>Product lifecycle status.</summary>
public enum ProductStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Published = 5,
    Suspended = 6,
    Archived = 7
}

/// <summary>Binding between a product and a compliance verdict.</summary>
public sealed record VerdictBinding(
    Guid Id,
    ProductId ProductId,
    CertificateId CertificateId,
    VerdictState State,
    DateTimeOffset BoundAt,
    DateTimeOffset? ExpiresAt,
    string PolicyVersion)
{
    public VerdictBinding() : this(
        Guid.NewGuid(),
        new ProductId(""),
        new CertificateId(""),
        VerdictState.Unverified,
        DateTimeOffset.UtcNow,
        null,
        "1.0") { }

    /// <summary>
    /// Factory method to create a new VerdictBinding with a verdict decision.
    /// </summary>
    public static VerdictBinding Issue(
        ProductId productId,
        CertificateId certificateId,
        VerdictState state,
        string policyVersion,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? boundAt = null)
    {
        return new VerdictBinding(
            Id: Guid.NewGuid(),
            ProductId: productId,
            CertificateId: certificateId,
            State: state,
            BoundAt: boundAt ?? DateTimeOffset.UtcNow,
            ExpiresAt: expiresAt,
            PolicyVersion: policyVersion);
    }
}
