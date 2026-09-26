using MediatR;

namespace HalalChain.Application.Catalog.Queries;

/// <summary>
/// Query to get available facets (filters) for catalog search.
/// Returns all available filter options with result counts.
/// </summary>
public record GetCatalogFacetsQuery : IRequest<CatalogFacets>
{
    /// <summary>
    /// Apply current filters to get conditional facet counts.
    /// </summary>
    public Dictionary<string, List<string>>? CurrentFilters { get; set; }
}

public record CatalogFacets
{
    public List<FacetOption> Categories { get; set; } = [];
    public List<FacetOption> PriceRanges { get; set; } = [];
    public List<FacetOption> Ratings { get; set; } = [];
    public List<FacetOption> Tags { get; set; } = [];
    public List<FacetOption> Attributes { get; set; } = [];
    public List<FacetOption> EcoRatings { get; set; } = [];
    public FacetOption? HalalCertified { get; set; }
    public FacetOption? InStock { get; set; }
}

public record FacetOption
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? Icon { get; set; }
}
