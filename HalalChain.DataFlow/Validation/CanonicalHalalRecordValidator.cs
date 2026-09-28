using System.Globalization;
using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Transform;

namespace HalalChain.DataFlow.Validation;

/// <summary>
/// Enforces the structural rules every halal supply-chain record must meet
/// before it is allowed into the HalalChain data layer. This is a
/// data-integrity gate, not a halal verdict — compliance status is decided
/// solely by the tawheed Policy Engine. Nothing here may assign, infer, or
/// override a halal status.
/// </summary>
public sealed class CanonicalHalalRecordValidator : IDataFlowValidator<CanonicalHalalRecord>
{
    private const int MaxNameLength = 512;
    private const int MaxKeyLength = 128;

    private static readonly string[] KnownEntityTypes =
    [
        "ORGANIZATION", "SUPPLIER", "MANUFACTURER", "IMPORTER", "EXPORTER",
        "FACILITY", "PRODUCT", "INGREDIENT", "RAW_MATERIAL", "CERTIFICATE",
        "CERTIFICATION_BODY", "AUDIT", "INSPECTION", "PRODUCTION_BATCH", "LOT",
        "SHIPMENT", "WAREHOUSE", "LOGISTICS_EVENT", "PROVENANCE",
        "COMPLIANCE_RECORD", "TRANSACTION"
    ];

    public string Name => "canonical-halal-record";

    public ValidationOutcome Validate(CanonicalHalalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var outcome = new ValidationOutcome();

        ValidateProvenance(record, outcome);
        ValidateEntityType(record, outcome);
        ValidateBusinessKey(record, outcome);
        ValidateTimestamps(record, outcome);
        ValidateCertificateWindow(record, outcome);
        ValidateQuantity(record, outcome);
        ValidateNames(record, outcome);

        return outcome;
    }

    private static void ValidateProvenance(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        if (string.IsNullOrWhiteSpace(record.SourceSystem))
        {
            outcome.AddError(nameof(record.SourceSystem), "SOURCE_SYSTEM_MISSING",
                "Every imported record must name the system it came from for audit.");
        }

        if (string.IsNullOrWhiteSpace(record.SourceKey))
        {
            outcome.AddError(nameof(record.SourceKey), "SOURCE_KEY_MISSING",
                "Every imported record must carry its source-system key.");
        }
    }

    private static void ValidateEntityType(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        if (string.IsNullOrWhiteSpace(record.EntityType))
        {
            outcome.AddError(nameof(record.EntityType), "ENTITY_TYPE_MISSING",
                "Entity type is required to select the canonical mapping.");
            return;
        }

        var normalized = HalalTextNormalizer.CanonicalizeIdentifier(record.EntityType);

        if (normalized is null || !KnownEntityTypes.Contains(normalized, StringComparer.Ordinal))
        {
            outcome.AddError(nameof(record.EntityType), "ENTITY_TYPE_UNKNOWN",
                $"'{record.EntityType}' is not a recognized halal supply-chain entity type.",
                record.EntityType);
        }
    }

    private static void ValidateBusinessKey(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        var field = record.BusinessKeyField;
        if (field is null) return;

        var value = field switch
        {
            nameof(record.CertificateNumber) => record.CertificateNumber,
            nameof(record.ProductSku) => record.ProductSku,
            nameof(record.BatchNumber) => record.BatchNumber,
            nameof(record.LotNumber) => record.LotNumber,
            nameof(record.IngredientName) => record.IngredientName,
            nameof(record.FacilityName) => record.FacilityName,
            nameof(record.SupplierName) => record.SupplierName,
            nameof(record.OrganizationName) => record.OrganizationName,
            _ => record.CanonicalId
        };

        if (string.IsNullOrWhiteSpace(value))
        {
            outcome.AddError(field, "BUSINESS_KEY_MISSING",
                $"A {record.EntityType} record requires a {field}.");
            return;
        }

        if (value.Trim().Length > MaxKeyLength)
        {
            outcome.AddError(field, "BUSINESS_KEY_TOO_LONG",
                $"{field} exceeds {MaxKeyLength} characters.", value.Length);
        }
    }

    private static void ValidateTimestamps(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        if (record.OccurredAt is not null && !IsParsable(record.OccurredAt))
        {
            outcome.AddError(nameof(record.OccurredAt), "OCCURRED_AT_INVALID",
                "OccurredAt is not a parseable ISO-8601 timestamp.", record.OccurredAt);
        }

        if (record.RecordedAt is not null && !IsParsable(record.RecordedAt))
        {
            outcome.AddWarning(nameof(record.RecordedAt), "RECORDED_AT_INVALID",
                "RecordedAt is not a parseable ISO-8601 timestamp; it was dropped.", record.RecordedAt);
        }
    }

    private static void ValidateCertificateWindow(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        if (record.CertificateIssuedAt is null && record.CertificateExpiresAt is null) return;

        if (record.CertificateIssuedAt is not null && !IsParsable(record.CertificateIssuedAt))
        {
            outcome.AddError(nameof(record.CertificateIssuedAt), "CERTIFICATE_ISSUED_AT_INVALID",
                "Certificate issue date is not parseable.", record.CertificateIssuedAt);
            return;
        }

        if (record.CertificateExpiresAt is not null && !IsParsable(record.CertificateExpiresAt))
        {
            outcome.AddError(nameof(record.CertificateExpiresAt), "CERTIFICATE_EXPIRES_AT_INVALID",
                "Certificate expiry date is not parseable.", record.CertificateExpiresAt);
            return;
        }

        if (record.CertificateIssuedAt is null || record.CertificateExpiresAt is null) return;

        var issued = Parse(record.CertificateIssuedAt!);
        var expires = Parse(record.CertificateExpiresAt!);

        if (expires <= issued)
        {
            outcome.AddCritical(nameof(record.CertificateExpiresAt), "CERTIFICATE_WINDOW_INVERTED",
                "Certificate expiry must be after its issue date.");
        }
    }

    private static void ValidateQuantity(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        if (record.Quantity is null) return;

        if (record.Quantity < 0)
        {
            outcome.AddError(nameof(record.Quantity), "QUANTITY_NEGATIVE",
                "Quantity must not be negative.", record.Quantity);
        }

        if (string.IsNullOrWhiteSpace(record.QuantityUnit))
        {
            outcome.AddWarning(nameof(record.QuantityUnit), "QUANTITY_UNIT_MISSING",
                "Quantity was supplied without a unit of measure.");
        }
    }

    private static void ValidateNames(CanonicalHalalRecord record, ValidationOutcome outcome)
    {
        foreach (var (name, value) in new[]
                 {
                     (nameof(record.OrganizationName), record.OrganizationName),
                     (nameof(record.FacilityName), record.FacilityName),
                     (nameof(record.ProductName), record.ProductName),
                     (nameof(record.DisplayName), record.DisplayName)
                 })
        {
            if (value is not null && value.Trim().Length > MaxNameLength)
            {
                outcome.AddError(name, "NAME_TOO_LONG",
                    $"{name} exceeds {MaxNameLength} characters.", value.Length);
            }
        }

        if (record.FacilityCountry is not null && HalalTextNormalizer.NormalizeCountryCode(record.FacilityCountry) is null)
        {
            outcome.AddError(nameof(record.FacilityCountry), "COUNTRY_CODE_INVALID",
                "Facility country must be a two-letter ISO code.", record.FacilityCountry);
        }
    }

    private static bool IsParsable(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
