namespace HalalChain.Automation.Browser;

/// <summary>
/// Egress control for every browser navigation (ADR-011 D1).
///
/// Enforced inside the adapters, immediately before each navigation rather
/// than once when the session opens — because a page can redirect, and because
/// an allowlist checked at session start is a claim about the next ten minutes
/// rather than a check on the request being made right now.
/// </summary>
public interface IBrowserNavigationPolicy
{
    /// <summary>Returns false when the target must not be reached, with a reason.</summary>
    bool IsAllowed(Uri target, out string? reason);

    /// <summary>Throws <see cref="BrowserNavigationBlockedException"/> when not allowed.</summary>
    void EnsureAllowed(Uri target);
}

public sealed class BrowserNavigationBlockedException(Uri target, string reason)
    : Exception($"Navigation to '{target}' blocked: {reason}")
{
    public Uri Target { get; } = target;
    public string Reason { get; } = reason;
}
