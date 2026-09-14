using System.Text.Json;
using System.Text.Json.Serialization;

namespace HalalChain.Platform.Contracts.Catalog.Dto;

// ── Taxonomy navigation ─────────────────────────────────────────

/// <summary>Top-level department, used for the main nav.</summary>
public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconClass,
    int SortOrder,
    int CategoryCount,
    int ProductCount);

/// <summary>2nd-level category inside a department.</summary>
public sealed record TaxonomyCategoryDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentSlug,
    string Name,
    string Slug,
    string? Description,
    string? IconClass,
    int SortOrder,
    int SubcategoryCount,
    int ProductCount);

/// <summary>3rd-level subcategory (the "filter chip" tier).</summary>
public sealed record SubcategoryDto(
    Guid Id,
    Guid CategoryId,
    string CategorySlug,
    string Name,
    string Slug,
    int SortOrder,
    int ProductTypeCount,
    int ProductCount);

/// <summary>4th-level leaf — the actual product kind.</summary>
public sealed record ProductTypeDto(
    Guid Id,
    Guid SubcategoryId,
    string SubcategorySlug,
    string Name,
    string Slug,
    string PathSlug,
    int SortOrder,
    int ProductCount);

/// <summary>Full breadcrumb path (for breadcrumbs and SEO).</summary>
public sealed record TaxonomyPathDto(
    string FullSlug,
    DepartmentDto Department,
    TaxonomyCategoryDto Category,
    SubcategoryDto Subcategory,
    ProductTypeDto ProductType);

// ── Reference data ──────────────────────────────────────────────

public sealed record CertificationBodyDto(
    Guid Id,
    string Name,
    string Slug,
    string? Acronym,
    string Country,
    int TrustTier,
    string? LogoUrl,
    string? Description);

public sealed record CountryDto(
    string Iso2,
    string Iso3,
    string Name,
    string Region);

// ── Halal profile (the compliance/commercial surface) ────────────

public sealed record HalalProfileDto(
    int Status,
    string StatusName,
    Guid? CertificationBodyId,
    string? CertificationBodyName,
    string? CertificateNumber,
    DateOnly? CertificateIssued,
    DateOnly? CertificateExpires,
    string? CertificationCountry,
    int? CertificationScope,
    int AnimalDerivative,
    int GelatinSource,
    int EnzymeSource,
    int FermentationMedium,
    bool ContainsAlcohol,
    decimal? AlcoholPercentage,
    int Dietary,
    [property: JsonConverter(typeof(JsonStringArrayConverter))] string[] DietaryTags,
    int Channel,
    int? MinOrderQuantity,
    int? LeadTimeDays,
    bool PrivateLabelAvailable,
    bool ContractManufacturing,
    WholesalePriceTierDto[] WholesaleTiers,
    string? CountryOfOrigin,
    string? CountryOfManufacture,
    string? CountryOfBrand,
    int Segments,
    [property: JsonConverter(typeof(JsonStringArrayConverter))] string[] SegmentNames,
    [property: JsonConverter(typeof(JsonStringArrayConverter))] string[] Tags,
    [property: JsonConverter(typeof(JsonStringArrayConverter))] string[] Allergens);

public sealed record WholesalePriceTierDto(int MinQuantity, decimal UnitPrice, string Currency);

// ── Enhanced product DTO (uses the new taxonomy + profile) ───────

public sealed record EnrichedProductDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string VendorName,
    string? BrandName,
    string Origin,
    decimal Price,
    string Currency,
    int Inventory,
    DateTimeOffset CreatedAt,
    TaxonomyPathDto? Path,
    HalalProfileDto Profile);

// ── Filter rail (returned with category pages) ──────────────────

public sealed record CategoryFiltersDto(
    CertificationBodyDto[] CertificationBodies,
    CountryDto[] Countries,
    string[] AllDietaryTags,
    string[] AllStatuses);

// ── JSON converters for [Flags] enums on the wire ────────────────

internal sealed class JsonStringArrayConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Accept both ["a","b"] and single-string forms
        if (reader.TokenType == JsonTokenType.String)
            return new[] { reader.GetString() ?? string.Empty };
        if (reader.TokenType != JsonTokenType.StartArray) return [];
        var list = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            if (reader.TokenType == JsonTokenType.String) list.Add(reader.GetString() ?? string.Empty);
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var v in value) writer.WriteStringValue(v);
        writer.WriteEndArray();
    }
}
