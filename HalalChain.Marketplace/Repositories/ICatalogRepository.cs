using HalalChain.Domain.Catalog;

namespace HalalChain.Marketplace.Repositories;

/// <summary>
/// Repository interfaces for catalog reference data.
/// Brand and the 4-level taxonomy contracts live in
/// <see cref="IBrandRepository"/> and <see cref="ITaxonomyRepositories"/>.
/// </summary>
public interface IProductAttributeRepository
{
    /// <summary>Get an attribute by ID with all options.</summary>
    Task<ProductAttribute?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get all attributes for faceted search.</summary>
    Task<IEnumerable<ProductAttribute>> GetFilterableAsync(CancellationToken ct = default);

    /// <summary>Get all attributes for product variants.</summary>
    Task<IEnumerable<ProductAttribute>> GetVariantAttributesAsync(CancellationToken ct = default);

    /// <summary>Get all attributes (with pagination).</summary>
    Task<IEnumerable<ProductAttribute>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

    /// <summary>Get attribute count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>Save a new attribute.</summary>
    Task AddAsync(ProductAttribute attribute, CancellationToken ct = default);

    /// <summary>Update an existing attribute.</summary>
    Task UpdateAsync(ProductAttribute attribute, CancellationToken ct = default);

    /// <summary>Delete an attribute.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository for reference data: CertificationBody, Facility, Country.
/// </summary>
public interface ICertificationBodyRepository
{
    /// <summary>Get a certification body by ID.</summary>
    Task<CertificationBody?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get a certification body by acronym (e.g., "JAKIM").</summary>
    Task<CertificationBody?> GetByAcronymAsync(string acronym, CancellationToken ct = default);

    /// <summary>Get all active certification bodies (ordered by trust tier).</summary>
    Task<IEnumerable<CertificationBody>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Get all certification bodies.</summary>
    Task<IEnumerable<CertificationBody>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get certification bodies by country.</summary>
    Task<IEnumerable<CertificationBody>> GetByCountryAsync(string country, CancellationToken ct = default);

    /// <summary>Get count of certification bodies.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);
}

public interface IFacilityRepository
{
    /// <summary>Get a facility by ID.</summary>
    Task<Facility?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get facilities for a vendor.</summary>
    Task<IEnumerable<Facility>> GetByVendorAsync(Guid vendorId, CancellationToken ct = default);

    /// <summary>Get all facilities (with pagination).</summary>
    Task<IEnumerable<Facility>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

    /// <summary>Get facility count.</summary>
    Task<int> GetCountAsync(CancellationToken ct = default);
}

public interface ICountryRepository
{
    /// <summary>Get a country by ISO2 code (e.g., "MY").</summary>
    Task<Country?> GetByIso2Async(string iso2, CancellationToken ct = default);

    /// <summary>Get all countries (ordered by name).</summary>
    Task<IEnumerable<Country>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Get countries by region.</summary>
    Task<IEnumerable<Country>> GetByRegionAsync(string region, CancellationToken ct = default);
}
