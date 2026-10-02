namespace HalalChain.Automation.Browser;

/// <summary>
/// Resolves a credential reference into a username/password at execution time
/// (ADR-011 D2).
///
/// Why a port instead of calling a repository directly from a node: a workflow
/// only ever contains <c>CredentialId</c>. Something has to turn that into a
/// secret, and the something must be server-side, tenant-checked, and
/// swappable between environments (vault, KMS, config store). Making it a port
/// keeps all three properties testable with a fake.
///
/// There is deliberately no <c>NullBrowserCredentialResolver</c> that returns
/// empty strings: a missing resolver must fail the action, because a login
/// that silently submits blank fields is a phantom success (ADR-011 D2).
/// </summary>
public interface IBrowserCredentialResolver
{
    Task<ResolvedCredential> ResolveAsync(
        string credentialId,
        string tenantId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Plaintext secret material. Never logged, never placed in workflow output,
/// never written to evidence — it exists only for the duration of the fill
/// that consumed it (ADR-011 D2).
/// </summary>
public sealed record ResolvedCredential(string Username, string Secret)
{
    public override string ToString() => "[redacted credential]";
}

public sealed class CredentialUnavailableException(string credentialId, string detail, Exception? inner = null)
    : Exception($"Credential '{credentialId}' unavailable: {detail}", inner)
{
    public string CredentialId { get; } = credentialId;

    public AutomationErrorKind ErrorKind => AutomationErrorKind.CredentialUnavailable;
}
