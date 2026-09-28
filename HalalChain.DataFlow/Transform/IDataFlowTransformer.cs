using HalalChain.DataFlow.Models;

namespace HalalChain.DataFlow.Transform;

/// <summary>
/// Maps a raw extracted record into a canonical destination record. Keeping
/// this separate from the source/destination lets a pipeline re-point at a
/// different physical system without rewriting the mapping.
/// </summary>
public interface IDataFlowTransformer<TSource, TDestination>
{
    string Name { get; }
    TDestination Transform(TSource source);
}

/// <summary>
/// Validates a record before it is written. Validation failures are recorded
/// on the record rather than thrown, so a batch can route bad rows to the
/// dead-letter path while good rows still land.
/// </summary>
public interface IDataFlowValidator<TRecord>
{
    string Name { get; }
    ValidationOutcome Validate(TRecord record);
}

public sealed class ValidationOutcome
{
    public ValidationOutcome()
    {
        Errors = new List<ValidationError>();
    }

    public List<ValidationError> Errors { get; }
    public bool IsValid => !Errors.Any(e => e.Severity >= ValidationSeverity.Error);

    public bool HasWarnings => Errors.Any(e => e.Severity == ValidationSeverity.Warning);

    public void AddError(string field, string code, string message, object? attempted = null) =>
        Errors.Add(new ValidationError
        {
            FieldName = field,
            ErrorCode = code,
            Message = message,
            AttemptedValue = attempted,
            Severity = ValidationSeverity.Error
        });

    public void AddWarning(string field, string code, string message, object? attempted = null) =>
        Errors.Add(new ValidationError
        {
            FieldName = field,
            ErrorCode = code,
            Message = message,
            AttemptedValue = attempted,
            Severity = ValidationSeverity.Warning
        });

    public void AddCritical(string field, string code, string message, object? attempted = null) =>
        Errors.Add(new ValidationError
        {
            FieldName = field,
            ErrorCode = code,
            Message = message,
            AttemptedValue = attempted,
            Severity = ValidationSeverity.Critical
        });
}

/// <summary>
/// Normalizes free-text identifiers so the same upstream organization or
/// product resolves to one canonical HalalChain id regardless of how the
/// source system spelled or cased it.
/// </summary>
public interface IEntityKeyResolver
{
    string? Resolve(string sourceSystem, string entityName, string sourceKey);
    void Register(string sourceSystem, string entityName, string sourceKey, string canonicalId);
}

public sealed class InMemoryEntityKeyResolver : IEntityKeyResolver
{
    private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public string? Resolve(string sourceSystem, string entityName, string sourceKey)
    {
        var key = BuildKey(sourceSystem, entityName, sourceKey);

        lock (_gate)
        {
            return _map.TryGetValue(key, out var id) ? id : null;
        }
    }

    public void Register(string sourceSystem, string entityName, string sourceKey, string canonicalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalId);

        var key = BuildKey(sourceSystem, entityName, sourceKey);

        lock (_gate)
        {
            _map[key] = canonicalId;
        }
    }

    private static string BuildKey(string sourceSystem, string entityName, string sourceKey) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{sourceSystem}{entityName}{sourceKey.Trim().ToUpperInvariant()}");
}
