namespace HalalChain.Domain.Catalog;

/// <summary>
/// The binding between a product and a tawheed verdict. This is the ONLY
/// place a halal status lives. There is no IsHalal boolean on Product.
///
/// Once issued, a binding is immutable. A new verdict produces a new
/// binding, which supersedes the prior one via the state machine.
///
/// The binding is a projection. It records what tawheed decided, when, and
/// under which policy version. It does not evaluate anything.
/// </summary>
public sealed record VerdictBinding
{
    public VerdictState State { get; private init; }
    public string PolicyVersion { get; private init; } = string.Empty;
    public string? CertificateNumber { get; private init; }
    public DateTimeOffset CertificateExpiresAt { get; private init; }
    public string? TraceHash { get; private init; }
    public DateTimeOffset DecidedAt { get; private init; }

    public static VerdictBinding Unbound => new()
    {
        State = VerdictState.InsufficientEvidence,
        PolicyVersion = "unbound",
        CertificateExpiresAt = DateTimeOffset.MinValue,
        DecidedAt = DateTimeOffset.MinValue,
    };

    public static VerdictBinding Issue(
        VerdictState state,
        string policyVersion,
        string? certificateNumber,
        DateTimeOffset certificateExpiresAt,
        string? traceHash,
        DateTimeOffset decidedAt) => new()
    {
        State = state,
        PolicyVersion = policyVersion,
        CertificateNumber = certificateNumber,
        CertificateExpiresAt = certificateExpiresAt,
        TraceHash = traceHash,
        DecidedAt = decidedAt,
    };

    /// <summary>True if the certificate this binding references has expired.</summary>
    public bool IsExpired(DateTimeOffset now) =>
        State == VerdictState.Halal && CertificateExpiresAt <= now;

    /// <summary>True if the certificate is within the given warning window.</summary>
    public bool IsExpiringSoon(DateTimeOffset now, TimeSpan window) =>
        State == VerdictState.Halal
        && CertificateExpiresAt > now
        && CertificateExpiresAt - now <= window;
}

public enum VerdictState
{
    Halal,
    NotHalal,
    InsufficientEvidence,
}
