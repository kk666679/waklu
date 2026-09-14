using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Catalog;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Halal.Dto;
using Microsoft.EntityFrameworkCore;
using HalalStatus = HalalChain.Domain.Catalog.HalalStatus;

namespace HalalChain.Platform.Api.Modules.Catalog;

/// <summary>
/// Shared mapping logic that projects a <see cref="Product"/> entity (with its
/// full taxonomy and HalalProfile graph) into the canonical <see cref="EnrichedProductDto"/>.
/// Used by both the product filter endpoint and the single-product enriched endpoint
/// so the mapping lives in exactly one place.
/// </summary>
public static class ProductEnrichmentExtensions
{
    /// <summary>
    /// Builds the full includes chain needed to map a product to an EnrichedProductDto
    /// in a single query (vendor, brand, taxnomy path, certificates).
    /// </summary>
    public static IQueryable<Product> WithEnrichmentIncludes(this IQueryable<Product> query) =>
        query
            .Include(p => p.Vendor)
            .Include(p => p.Brand)
            .Include(p => p.Certificates)
            .Include(p => p.ProductType)
                .ThenInclude(t => t!.Subcategory)
                .ThenInclude(s => s.Category)
                .ThenInclude(c => c.Department);

    public static async Task<Dictionary<Guid, string>> GetCertificationBodyNamesAsync(
        this HalalChainDbContext db, CancellationToken ct = default) =>
        await db.CertificationBodies
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

    /// <summary>
    /// Maps a single loaded product entity to an <see cref="EnrichedProductDto"/>,
    /// resolving certification body names from the supplied dictionary.
    /// </summary>
    public static EnrichedProductDto ToEnrichedDto(this Product p, Dictionary<Guid, string>? certBodyNames = null)
    {
        HalalProfileDto? profile = null;
        if (p.HalalProfile is { } hp)
        {
            var certName = hp.CertificationBodyId.HasValue && certBodyNames != null && certBodyNames.TryGetValue(hp.CertificationBodyId.Value, out var cb)
                ? cb : null;
            profile = new HalalProfileDto(
                Status: (int)hp.Status,
                StatusName: hp.Status.ToString(),
                CertificationBodyId: hp.CertificationBodyId,
                CertificationBodyName: certName,
                CertificateNumber: hp.CertificateNumber,
                CertificateIssued: hp.CertificateIssued,
                CertificateExpires: hp.CertificateExpires,
                CertificationCountry: hp.CertificationCountry,
                CertificationScope: hp.CertificationScope.HasValue ? (int)hp.CertificationScope.Value : null,
                AnimalDerivative: (int)hp.AnimalDerivative,
                GelatinSource: (int)hp.GelatinSource,
                EnzymeSource: (int)hp.EnzymeSource,
                FermentationMedium: (int)hp.FermentationMedium,
                ContainsAlcohol: hp.ContainsAlcohol,
                AlcoholPercentage: hp.AlcoholPercentage,
                Dietary: (int)hp.Dietary,
                DietaryTags: FlagsToNames(hp.Dietary),
                Channel: (int)hp.Channel,
                MinOrderQuantity: hp.MinOrderQuantity,
                LeadTimeDays: hp.LeadTimeDays,
                PrivateLabelAvailable: hp.PrivateLabelAvailable,
                ContractManufacturing: hp.ContractManufacturing,
                WholesaleTiers: hp.WholesaleTiers.Select(w => new WholesalePriceTierDto(w.MinQuantity, w.UnitPrice, w.Currency)).ToArray(),
                CountryOfOrigin: hp.CountryOfOrigin,
                CountryOfManufacture: hp.CountryOfManufacture,
                CountryOfBrand: hp.CountryOfBrand,
                Segments: (int)hp.Segments,
                SegmentNames: FlagsToNames(hp.Segments),
                Tags: hp.Tags.ToArray(),
                Allergens: hp.Allergens.ToArray());
        }
        else if (p.Certificates?.Any() == true)
        {
            // Legacy fallback: derive a minimal HalalProfile from the Certificate
            // table so older seeded products still surface certification trust signals.
            var liveCert = p.Certificates
                .FirstOrDefault(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)
                ?? p.Certificates.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

            var status = liveCert?.Status == CertificateStatus.Verified && liveCert.ExpiryDate > DateTimeOffset.UtcNow
                ? HalalStatus.Certified
                : liveCert?.Status == CertificateStatus.Verified
                    ? HalalStatus.Unknown  // expired
                    : HalalStatus.Pending;

            var certName = liveCert?.CertificationBody;

            profile = new HalalProfileDto(
                Status: (int)status,
                StatusName: status.ToString(),
                CertificationBodyId: null,
                CertificationBodyName: certName,
                CertificateNumber: liveCert?.CertificateNumber,
                CertificateIssued: DateOnly.FromDateTime(liveCert?.IssueDate.UtcDateTime.Date ?? DateTime.UtcNow),
                CertificateExpires: liveCert is null ? null : DateOnly.FromDateTime(liveCert.ExpiryDate.UtcDateTime.Date),
                CertificationCountry: liveCert?.Jurisdiction,
                CertificationScope: null,
                AnimalDerivative: 0,
                GelatinSource: 0,
                EnzymeSource: 0,
                FermentationMedium: 0,
                ContainsAlcohol: false,
                AlcoholPercentage: null,
                Dietary: 0,
                DietaryTags: [],
                Channel: 0,
                MinOrderQuantity: null,
                LeadTimeDays: null,
                PrivateLabelAvailable: false,
                ContractManufacturing: false,
                WholesaleTiers: [],
                CountryOfOrigin: null,
                CountryOfManufacture: null,
                CountryOfBrand: null,
                Segments: 0,
                SegmentNames: [],
                Tags: [],
                Allergens: []);
        }

        TaxonomyPathDto? path = null;
        if (p.ProductType is { } t)
        {
            path = new TaxonomyPathDto(
                t.PathSlug,
                new DepartmentDto(t.Subcategory.Category.Department.Id,
                    t.Subcategory.Category.Department.Name,
                    t.Subcategory.Category.Department.Slug,
                    t.Subcategory.Category.Department.Description,
                    t.Subcategory.Category.Department.IconClass,
                    t.Subcategory.Category.Department.SortOrder, 0, 0),
                new TaxonomyCategoryDto(t.Subcategory.Category.Id,
                    t.Subcategory.Category.DepartmentId,
                    t.Subcategory.Category.Department.Slug,
                    t.Subcategory.Category.Name, t.Subcategory.Category.Slug,
                    t.Subcategory.Category.Description, t.Subcategory.Category.IconClass,
                    t.Subcategory.Category.SortOrder, 0, 0),
                new SubcategoryDto(t.Subcategory.Id, t.Subcategory.CategoryId,
                    t.Subcategory.Category.Slug, t.Subcategory.Name, t.Subcategory.Slug,
                    t.Subcategory.SortOrder, 0, 0),
                new ProductTypeDto(t.Id, t.SubcategoryId, t.Subcategory.Slug,
                    t.Name, t.Slug, t.PathSlug, t.SortOrder, 0));
        }

        return new EnrichedProductDto(
            p.Id, p.Title, p.Slug, p.Description,
            p.Vendor?.Name ?? string.Empty,
            p.Brand?.Name,
            p.Origin, p.Price, p.Currency, p.Inventory, p.CreatedAt,
            path, profile!);
    }

    private static string[] FlagsToNames<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var names = new List<string>();
        foreach (var flag in Enum.GetValues<TEnum>())
        {
            if (flag.Equals(default(TEnum))) continue;
            if (value.HasFlag(flag)) names.Add(flag.ToString());
        }
        return names.ToArray();
    }
}
