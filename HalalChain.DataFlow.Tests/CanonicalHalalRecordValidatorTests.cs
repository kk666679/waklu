using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Validation;
using Xunit;

namespace HalalChain.DataFlow.Tests;

public sealed class CanonicalHalalRecordValidatorTests
{
    private readonly CanonicalHalalRecordValidator _validator = new();

    private static CanonicalHalalRecord ValidCertificate() => new()
    {
        EntityType = "CERTIFICATE",
        SourceSystem = "erp-prod",
        SourceKey = "CERT-0001",
        CertificateNumber = "MY-2026-0001",
        CertificationBody = "JAKIM",
        CertificateIssuedAt = "2026-01-01T00:00:00Z",
        CertificateExpiresAt = "2027-01-01T00:00:00Z"
    };

    [Fact]
    public void ValidCertificate_Passes()
    {
        var outcome = _validator.Validate(ValidCertificate());

        Assert.True(outcome.IsValid);
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public void MissingSourceSystem_IsAnError()
    {
        var record = ValidCertificate();
        record.SourceSystem = null;

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "SOURCE_SYSTEM_MISSING");
    }

    [Fact]
    public void MissingSourceKey_IsAnError()
    {
        var record = ValidCertificate();
        record.SourceKey = "   ";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "SOURCE_KEY_MISSING");
    }

    [Fact]
    public void UnknownEntityType_IsRejectedRatherThanDefaulted()
    {
        var record = ValidCertificate();
        record.EntityType = "Spaceship";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "ENTITY_TYPE_UNKNOWN");
    }

    [Theory]
    [InlineData("certificate")]
    [InlineData("CERTIFICATE")]
    [InlineData(" Certificate ")]
    public void EntityType_MatchingIsCaseAndWhitespaceInsensitive(string entityType)
    {
        var record = ValidCertificate();
        record.EntityType = entityType;

        Assert.True(_validator.Validate(record).IsValid);
    }

    [Fact]
    public void CertificateWithoutNumber_IsRejected()
    {
        var record = ValidCertificate();
        record.CertificateNumber = null;

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "BUSINESS_KEY_MISSING");
    }

    [Fact]
    public void InvertedCertificateWindow_IsCritical()
    {
        var record = ValidCertificate();
        record.CertificateExpiresAt = "2025-01-01T00:00:00Z";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "CERTIFICATE_WINDOW_INVERTED"
                                            && e.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void UnparseableOccurrenceDate_IsRejected()
    {
        var record = ValidCertificate();
        record.OccurredAt = "the day before yesterday";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "OCCURRED_AT_INVALID");
    }

    [Fact]
    public void UnparseableRecordedAt_IsOnlyAWarning()
    {
        var record = ValidCertificate();
        record.RecordedAt = "yesterday";

        var outcome = _validator.Validate(record);

        // A bad audit timestamp must not block an otherwise sound record.
        Assert.True(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "RECORDED_AT_INVALID"
                                            && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void NegativeQuantity_IsRejected()
    {
        var record = ValidCertificate();
        record.Quantity = -1m;
        record.QuantityUnit = "kg";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "QUANTITY_NEGATIVE");
    }

    [Fact]
    public void QuantityWithoutUnit_WarnsButPasses()
    {
        var record = ValidCertificate();
        record.Quantity = 5m;

        var outcome = _validator.Validate(record);

        Assert.True(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "QUANTITY_UNIT_MISSING"
                                            && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void InvalidCountryCode_IsRejected()
    {
        var record = ValidCertificate();
        record.FacilityCountry = "Malaysia";

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "COUNTRY_CODE_INVALID");
    }

    [Fact]
    public void OverlongName_IsRejected()
    {
        var record = ValidCertificate();
        record.OrganizationName = new string('x', 513);

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "NAME_TOO_LONG");
    }

    [Fact]
    public void ProductRecord_RequiresSku()
    {
        var record = new CanonicalHalalRecord
        {
            EntityType = "PRODUCT",
            SourceSystem = "erp",
            SourceKey = "P-1"
        };

        var outcome = _validator.Validate(record);

        Assert.False(outcome.IsValid);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == "BUSINESS_KEY_MISSING"
                                            && e.FieldName == nameof(CanonicalHalalRecord.ProductSku));
    }

    [Fact]
    public void Validator_NeverAssignsAHalalVerdict()
    {
        // Guards the architectural principle: the Policy Engine decides
        // compliance. This gate is a data-integrity check only.
        var record = ValidCertificate();
        record.CertificateStatus = "HALAL";

        var outcome = _validator.Validate(record);

        Assert.True(outcome.IsValid);
        Assert.DoesNotContain(outcome.Errors, e => e.Severity == ValidationSeverity.Critical);
        Assert.DoesNotContain(outcome.Errors,
            e => e.ErrorCode.Contains("VERDICT", StringComparison.OrdinalIgnoreCase)
                 || e.ErrorCode.Contains("HALAL_STATUS", StringComparison.OrdinalIgnoreCase));
    }
}
