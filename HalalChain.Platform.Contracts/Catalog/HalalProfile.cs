namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// The structured compliance &amp; commercial attribute set that every product carries.
/// This is the core of the redesigned taxonomy: it separates "where a product sits
/// in the commercial tree" (Department → Category → Subcategory → ProductType)
/// from "what a product is" (this profile). A product lives in ONE commercial path
/// and carries ONE HalalProfile (plus per-variant data).
///
/// Stored as a JSONB column on Product for fast GIN-indexed filtering.
/// </summary>
public sealed record HalalProfile
{
    // ── Trust & Certification ───────────────────────────────────────
    public HalalStatus Status { get; init; } = HalalStatus.Unknown;
    public Guid? CertificationBodyId { get; init; }
    public string? CertificateNumber { get; init; }
    public DateOnly? CertificateIssued { get; init; }
    public DateOnly? CertificateExpires { get; init; }
    public string? CertificationCountry { get; init; }
    public CertificationScope? CertificationScope { get; init; }
    public string? CertificateUrl { get; init; }

    // ── Composition Disclosure (critical for F&B, supplements, cosmetics) ──
    public AnimalDerivative AnimalDerivative { get; init; } = AnimalDerivative.None;
    public GelatinSource GelatinSource { get; init; } = GelatinSource.None;
    public EnzymeSource EnzymeSource { get; init; } = EnzymeSource.None;
    public FermentationMedium FermentationMedium { get; init; } = FermentationMedium.NotApplicable;
    public bool ContainsAlcohol { get; init; }
    public decimal? AlcoholPercentage { get; init; }
    public string? IngredientNotes { get; init; }

    // ── Dietary Attributes (multi-select via flags) ────────────────
    public DietaryTags Dietary { get; init; } = DietaryTags.None;

    // ── Commercial Terms (B2B-aware) ────────────────────────────────
    public CommercialChannel Channel { get; init; } = CommercialChannel.Retail;
    public int? MinOrderQuantity { get; init; }
    public int? LeadTimeDays { get; init; }
    public bool PrivateLabelAvailable { get; init; }
    public bool ContractManufacturing { get; init; }
    public List<WholesalePriceTier> WholesaleTiers { get; init; } = [];

    // ── Origin ─────────────────────────────────────────────────────
    public string? CountryOfOrigin { get; init; }
    public string? CountryOfManufacture { get; init; }
    public string? CountryOfBrand { get; init; }
    public Guid? ManufacturerId { get; init; }
    public Guid? FacilityId { get; init; }

    // ── Audience Segments (multi-select) ───────────────────────────
    public AudienceSegment Segments { get; init; } = AudienceSegment.None;

    // ── Free-form search boosts ────────────────────────────────────
    public List<string> Tags { get; init; } = [];
    public List<string> Allergens { get; init; } = [];
}

/// <summary>Wholesale tier pricing — a quantity break.</summary>
public sealed record WholesalePriceTier(int MinQuantity, decimal UnitPrice, string Currency);
