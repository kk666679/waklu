namespace HalalChain.DataFlow.Models;

/// <summary>
/// A canonical halal supply-chain record. This is the neutral shape that
/// enterprise SQL Server and PostgreSQL sources are normalized into before
/// reaching the HalalChain data layer, so one validator and one set of
/// mappings serves every upstream system.
/// </summary>
public sealed class CanonicalHalalRecord
{
    public CanonicalHalalRecord() { }

    public string? EntityType { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceKey { get; set; }

    public string? CanonicalId { get; set; }
    public string? DisplayName { get; set; }

    public string? OrganizationName { get; set; }
    public string? OrganizationType { get; set; }
    public string? FacilityName { get; set; }
    public string? FacilityCountry { get; set; }

    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? BatchNumber { get; set; }
    public string? LotNumber { get; set; }

    public string? IngredientName { get; set; }
    public string? SupplierName { get; set; }

    public string? CertificateNumber { get; set; }
    public string? CertificationBody { get; set; }
    public string? CertificateStatus { get; set; }
    public string? CertificateIssuedAt { get; set; }
    public string? CertificateExpiresAt { get; set; }

    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }

    public string? OccurredAt { get; set; }
    public string? RecordedAt { get; set; }

    public string? Status { get; set; }
    public string? BlockchainTransactionReference { get; set; }
    public string? ProvenanceHash { get; set; }

    public Dictionary<string, object?> Attributes { get; set; } = new();

    /// <summary>
    /// The field name carrying this record's business key, chosen by
    /// entity type. The validator asserts it is present.
    /// </summary>
    public string? BusinessKeyField => NormalizedEntityType switch
    {
        "CERTIFICATE" => nameof(CertificateNumber),
        "PRODUCT" => nameof(ProductSku),
        "BATCH" => nameof(BatchNumber),
        "LOT" => nameof(LotNumber),
        "INGREDIENT" => nameof(IngredientName),
        "FACILITY" => nameof(FacilityName),
        "SUPPLIER" => nameof(SupplierName),
        "ORGANIZATION" => nameof(OrganizationName),
        _ => nameof(CanonicalId)
    };

    /// <summary>
    /// The entity type reduced to its canonical token. Trimming and
    /// upper-casing here keeps the key selection in step with the
    /// normalizer the validator uses to accept the type.
    /// </summary>
    public string? NormalizedEntityType =>
        EntityType?.Trim().ToUpper(System.Globalization.CultureInfo.InvariantCulture);
}
