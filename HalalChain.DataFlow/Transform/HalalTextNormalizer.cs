using System.Globalization;
using System.Text.RegularExpressions;

namespace HalalChain.DataFlow.Transform;

/// <summary>
/// Cleansing rules shared by every halal supply-chain entity. Enterprise
/// systems spell, case, and pad identifiers inconsistently; the same
/// organization can appear as "Acme  Foods", "ACME FOODS" and "Acme-Foods".
/// These helpers collapse those variants so de-duplication is deterministic.
/// </summary>
public static class HalalTextNormalizer
{
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex NonAlphanumeric = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Collapses whitespace and trims. The display form of a name.
    /// </summary>
    public static string? CollapseWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return WhitespaceRun.Replace(value, " ").Trim();
    }

    /// <summary>
    /// Reduces a name to a comparison key: alphanumerics only, upper-cased.
    /// Two names with the same key are treated as the same entity.
    /// </summary>
    public static string ComparisonKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var stripped = NonAlphanumeric.Replace(value, string.Empty);
        return stripped.ToUpperInvariant();
    }

    /// <summary>
    /// Canonical identifier form: trimmed, whitespace-collapsed, upper-cased.
    /// </summary>
    public static string? CanonicalizeIdentifier(string? value) =>
        CollapseWhitespace(value)?.ToUpperInvariant();

    /// <summary>
    /// Normalizes an ISO country to upper-case alpha-2, or null when the
    /// value is not a plausible country code. Halal certification records are
    /// jurisdiction-scoped, so an unparseable country must not silently
    /// become a default.
    /// </summary>
    public static string? NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length == 2 && trimmed.All(char.IsLetter) ? trimmed : null;
    }

    /// <summary>
    /// Normalizes a free-text date to ISO-8601. Returns null rather than
    /// guessing when the value is unparseable, so the caller can raise a
    /// validation error instead of anchoring a wrong date to the chain.
    /// </summary>
    public static string? NormalizeDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            // Normalize to UTC and emit the round-trip form so a value
            // written here and one written by a later pipeline version
            // compare byte-for-byte.
            return parsed.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        }

        return null;
    }

    /// <summary>
    /// Parses a decimal from an enterprise export. Two shapes are supported:
    /// the invariant form ("1,234.56") and the comma-decimal form
    /// ("1234,56") used across much of Europe and Asia.
    ///
    /// The comma-decimal case matters for correctness, not just tolerance:
    /// under invariant rules a bare "1234,56" parses as 123456, silently
    /// inflating a quantity a thousandfold. A comma is therefore only
    /// treated as a grouping separator when a dot is also present, or when
    /// the group after it is exactly three digits and a dot precedes it.
    /// </summary>
    public static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.Trim();
        var hasDot = trimmed.Contains('.');
        var hasComma = trimmed.Contains(',');

        if (LooksLikeCommaDecimal(trimmed, hasDot, hasComma))
        {
            return decimal.TryParse(
                trimmed.Replace(",", ".", StringComparison.Ordinal),
                NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Decides whether the comma is a decimal separator. True when the comma
    /// is the last separator and is followed by one or two digits — a
    /// fractional part. "1,23" and "1234,56" qualify; "1,234" and
    /// "1,234.56" do not, because their comma introduces a group of three.
    /// </summary>
    private static bool LooksLikeCommaDecimal(string value, bool hasDot, bool hasComma)
    {
        if (!hasComma) return false;

        var lastComma = value.LastIndexOf(',');

        // A dot after the comma means the dot is the decimal separator.
        if (hasDot && value.IndexOf('.') > lastComma) return false;

        var tail = value[(lastComma + 1)..];
        return tail.Length is 1 or 2 && tail.All(char.IsDigit);
    }

    /// <summary>
    /// Normalizes a free-text status to a canonical token, matching
    /// case-insensitively against the known set. Unknown values return
    /// null so the caller can dead-letter rather than guess a status that
    /// would change compliance behaviour.
    /// </summary>
    public static string? NormalizeStatus(string? value, IReadOnlyCollection<string> known)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = CollapseWhitespace(value)!;
        var match = known.FirstOrDefault(k => string.Equals(k, trimmed, StringComparison.OrdinalIgnoreCase));

        return match ?? NormalizeStatusLoose(trimmed, known);
    }

    /// <summary>
    /// Second-pass status match that tolerates separators, so "in-progress",
    /// "In Progress" and "IN_PROGRESS" all resolve to the same token.
    /// </summary>
    private static string? NormalizeStatusLoose(string value, IReadOnlyCollection<string> known)
    {
        var key = ComparisonKey(value);
        if (key.Length == 0) return null;

        return known.FirstOrDefault(k => ComparisonKey(k) == key);
    }
}
