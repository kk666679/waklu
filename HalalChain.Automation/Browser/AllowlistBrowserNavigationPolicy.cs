namespace HalalChain.Automation.Browser;

/// <summary>
/// Deny-by-default allowlist over hosts (ADR-011 D1).
///
/// Design decisions worth being explicit about:
///
///   * <b>Empty allowlist denies everything.</b> A deployment that forgets to
///     configure egress gets a hard failure on the first navigation instead of
///     an engine that can reach anywhere. Failing closed is the whole control.
///
///   * <b>The allowlist is the only gate.</b> Loopback, link-local
///     (169.254.169.254), and RFC-1918 targets are rejected because they are
///     not on the list — not because we enumerated them. An attacker who wants
///     an internal host has to get it past the same list, and if an operator
///     deliberately adds one, that is visible configuration rather than a
///     hidden bypass.
///
///   * <b>Non-web schemes are rejected outright.</b> file://, data:, and
///     javascript: are not navigation, they are local code execution and file
///     disclosure. No allowlist entry can opt in to them.
///
///   * <b>Ports are part of the entry.</b> "portal.example" and
///     "portal.example:8443" are different services with different exposures.
/// </summary>
public sealed class AllowlistBrowserNavigationPolicy : IBrowserNavigationPolicy
{
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { Uri.UriSchemeHttp, Uri.UriSchemeHttps };

    private readonly List<HostEntry> _entries;

    public AllowlistBrowserNavigationPolicy(IEnumerable<string>? allowedHosts)
    {
        _entries = (allowedHosts ?? [])
            .Where(static h => !string.IsNullOrWhiteSpace(h))
            .Select(static h => HostEntry.Parse(h.Trim()))
            .Where(static e => e is not null)
            .Select(static e => e!)
            .ToList();
    }

    public IReadOnlyList<string> AllowedHosts => _entries
        .Select(static e => e.Original)
        .ToArray();

    public bool IsAllowed(Uri? target, out string? reason)
    {
        if (target is null)
        {
            reason = "target is not an absolute URI";
            return false;
        }

        if (!target.IsAbsoluteUri)
        {
            reason = "target is not an absolute URI";
            return false;
        }

        if (!AllowedSchemes.Contains(target.Scheme))
        {
            reason = $"scheme '{target.Scheme}' is not permitted; only http and https may be navigated";
            return false;
        }

        if (_entries.Count == 0)
        {
            reason = "no hosts are allowlisted (Browser:AllowedHosts is empty), so all navigation is denied";
            return false;
        }

        var host = target.Host;
        var port = target.Port;

        foreach (var entry in _entries)
        {
            if (!entry.Matches(host, port))
                continue;

            reason = null;
            return true;
        }

        reason = $"host '{host}' is not in the allowlist";
        return false;
    }

    public void EnsureAllowed(Uri target)
    {
        if (!IsAllowed(target, out var reason))
            throw new BrowserNavigationBlockedException(target, reason ?? "not allowed");
    }

    /// <summary>
    /// One allowlist row: <c>example.com</c>, <c>example.com:8443</c>, or
    /// <c>*.example.com</c> (any subdomain, but not the bare domain).
    /// </summary>
    private sealed class HostEntry
    {
        public required string Original { get; init; }

        private string Host { get; init; } = string.Empty;

        /// <summary>-1 when the entry did not pin a port.</summary>
        private int ExplicitPort { get; init; } = -1;

        private bool IsWildcard { get; init; }

        public static HostEntry? Parse(string raw)
        {
            var port = -1;
            var host = raw;

            // IPv6 literals are bracketed: [::1]:8443
            var bracketClose = raw.LastIndexOf(']');
            if (bracketClose >= 0 && bracketClose < raw.Length - 1 && raw[bracketClose + 1] == ':')
            {
                host = raw[..(bracketClose + 1)];
                var portText = raw[(bracketClose + 2)..];
                if (!int.TryParse(portText, out port))
                    return null;
            }
            else
            {
                // Only a single colon means "host:port". More than one means an
                // unbracketed IPv6 literal, which must not be split on its
                // last colon — doing so would fabricate a port out of "::1".
                var colon = raw.LastIndexOf(':');
                if (colon > 0 && raw.IndexOf(':') == colon)
                {
                    var portText = raw[(colon + 1)..];
                    if (int.TryParse(portText, out var parsed) && parsed is > 0 and <= 65535)
                    {
                        host = raw[..colon];
                        port = parsed;
                    }
                }
            }

            var wildcard = host.StartsWith("*.", StringComparison.Ordinal);
            if (wildcard)
                host = host[2..];

            if (string.IsNullOrWhiteSpace(host))
                return null;

            return new HostEntry
            {
                Original = raw,
                Host = host.TrimEnd('.'),
                ExplicitPort = port,
                IsWildcard = wildcard,
            };
        }

        public bool Matches(string host, int actualPort)
        {
            host = host.TrimEnd('.');

            var hostOk = IsWildcard
                ? host.EndsWith("." + Host, StringComparison.OrdinalIgnoreCase)
                : string.Equals(host, Host, StringComparison.OrdinalIgnoreCase);

            if (!hostOk)
                return false;

            // No port pinned in the entry ⇒ any port on that host is in scope.
            return ExplicitPort == -1 || actualPort == ExplicitPort;
        }
    }
}
