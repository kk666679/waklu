namespace HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Standardized failure categories for agent evaluation.
///
/// A failure category answers "what went wrong" — never "what should the
/// verdict be". Scoring a workflow node against a golden expectation produces
/// a score, a pass flag, and one of these categories. It does not produce a
/// compliance outcome, and this type must never gain one.
///
/// Ported from .halalchain/agents/app/eval/dag/taxonomy.py. The two must
/// stay in lockstep: a category emitted by the Python shadow runner and one
/// emitted here are compared by name in the eval report.
/// </summary>
public enum FailureCategory
{
    None = 0,

    // Collector failures
    CollectorMissedSource,
    CollectorTimeout,
    CollectorAuthFailed,
    CollectorRateLimited,

    // Classifier failures
    ClassifierMislabel,
    ClassifierLowConfidence,
    ClassifierUnknownLabel,

    // Verifier failures
    VerifierValidityDisagreement,
    VerifierExpiredEvidence,
    VerifierMalformedEvidence,

    // Handoff failures
    HandoffContractViolation,
    HandoffMissingRequiredField,
    HandoffSchemaMismatch,

    // Gap agent failures
    GapMissedRequirement,
    GapFalsePositive,
    GapIncompleteRequest,

    // Recollection failures
    RecollectionFailedToClose,
    RecollectionMaxRetries,

    // Terminal node failures
    TerminalOutcomeMismatch,

    // Infrastructure
    InfraBlobReadFailed,
    InfraLlmUnavailable,
}

/// <summary>
/// Wire-name mapping for <see cref="FailureCategory"/>.
///
/// The Python side emits dotted strings ("collector.missed_source"). Eval
/// reports are diffed across both runtimes, so the wire form is part of the
/// contract, not a display detail.
/// </summary>
public static class FailureCategories
{
    private static readonly IReadOnlyDictionary<FailureCategory, string> WireNames =
        new Dictionary<FailureCategory, string>
        {
            [FailureCategory.None] = "",

            [FailureCategory.CollectorMissedSource] = "collector.missed_source",
            [FailureCategory.CollectorTimeout] = "collector.timeout",
            [FailureCategory.CollectorAuthFailed] = "collector.auth_failed",
            [FailureCategory.CollectorRateLimited] = "collector.rate_limited",

            [FailureCategory.ClassifierMislabel] = "classifier.mislabel",
            [FailureCategory.ClassifierLowConfidence] = "classifier.low_confidence",
            [FailureCategory.ClassifierUnknownLabel] = "classifier.unknown_label",

            [FailureCategory.VerifierValidityDisagreement] = "verifier.validity_disagreement",
            [FailureCategory.VerifierExpiredEvidence] = "verifier.expired_evidence",
            [FailureCategory.VerifierMalformedEvidence] = "verifier.malformed_evidence",

            [FailureCategory.HandoffContractViolation] = "handoff.contract_violation",
            [FailureCategory.HandoffMissingRequiredField] = "handoff.missing_required_field",
            [FailureCategory.HandoffSchemaMismatch] = "handoff.schema_mismatch",

            [FailureCategory.GapMissedRequirement] = "gap.missed_requirement",
            [FailureCategory.GapFalsePositive] = "gap.false_positive",
            [FailureCategory.GapIncompleteRequest] = "gap.incomplete_request",

            [FailureCategory.RecollectionFailedToClose] = "recollection.failed_to_close",
            [FailureCategory.RecollectionMaxRetries] = "recollection.max_retries",

            [FailureCategory.TerminalOutcomeMismatch] = "verdict.mismatch",

            [FailureCategory.InfraBlobReadFailed] = "infra.blob_read_failed",
            [FailureCategory.InfraLlmUnavailable] = "infra.llm_unavailable",
        };

    private static readonly IReadOnlyDictionary<string, FailureCategory> ByWireName =
        WireNames.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>All categories except <see cref="FailureCategory.None"/>.</summary>
    public static IReadOnlyCollection<FailureCategory> All { get; } =
        WireNames.Keys.Where(c => c != FailureCategory.None).ToArray();

    public static string ToWireName(this FailureCategory category) =>
        WireNames.TryGetValue(category, out var name)
            ? name
            : throw new ArgumentOutOfRangeException(
                nameof(category), category, "No wire name is mapped for this category.");

    /// <summary>Parses a wire name. Returns false for unknown input rather than throwing.</summary>
    public static bool TryParseWireName(string? wireName, out FailureCategory category)
    {
        if (string.IsNullOrEmpty(wireName))
        {
            category = FailureCategory.None;
            return false;
        }

        return ByWireName.TryGetValue(wireName, out category);
    }

    /// <summary>
    /// Parses a wire name, throwing on unknown input. Used where an unknown
    /// category is a contract break rather than a data-quality problem.
    /// </summary>
    public static FailureCategory ParseWireName(string wireName) =>
        TryParseWireName(wireName, out var category)
            ? category
            : throw new ArgumentException(
                $"Unknown failure category '{wireName}'. The C# and Python taxonomies " +
                "have diverged; add the category to both.", nameof(wireName));

    public static bool IsCollector(this FailureCategory category) =>
        category is FailureCategory.CollectorMissedSource
            or FailureCategory.CollectorTimeout
            or FailureCategory.CollectorAuthFailed
            or FailureCategory.CollectorRateLimited;

    public static bool IsClassifier(this FailureCategory category) =>
        category is FailureCategory.ClassifierMislabel
            or FailureCategory.ClassifierLowConfidence
            or FailureCategory.ClassifierUnknownLabel;

    public static bool IsVerifier(this FailureCategory category) =>
        category is FailureCategory.VerifierValidityDisagreement
            or FailureCategory.VerifierExpiredEvidence
            or FailureCategory.VerifierMalformedEvidence;

    public static bool IsHandoff(this FailureCategory category) =>
        category is FailureCategory.HandoffContractViolation
            or FailureCategory.HandoffMissingRequiredField
            or FailureCategory.HandoffSchemaMismatch;

    public static bool IsGap(this FailureCategory category) =>
        category is FailureCategory.GapMissedRequirement
            or FailureCategory.GapFalsePositive
            or FailureCategory.GapIncompleteRequest;

    public static bool IsRecollection(this FailureCategory category) =>
        category is FailureCategory.RecollectionFailedToClose
            or FailureCategory.RecollectionMaxRetries;

    public static bool IsTerminal(this FailureCategory category) =>
        category is FailureCategory.TerminalOutcomeMismatch;

    public static bool IsInfrastructure(this FailureCategory category) =>
        category is FailureCategory.InfraBlobReadFailed
            or FailureCategory.InfraLlmUnavailable;
}
