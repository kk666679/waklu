namespace HalalChain.Platform.Contracts.Catalog.Dto;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? ParentId, int ProductCount);

public sealed record ProductDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string CategoryName,
    string VendorName,
    string Origin,
    decimal Price,
    string Currency,
    int Inventory,
    HalalStatusDto HalalStatus,
    DateTimeOffset CreatedAt);

public sealed record HalalStatusDto(
    string Status,
    string? CertificateNumber,
    string? CertificationBody,
    DateTimeOffset? ExpiresAt);

public sealed record PagedResult<T>(
    T[] Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record SemanticSearchResult(
    Guid ProductId,
    string Title,
    string Slug,
    string? Description,
    string VendorName,
    string CategoryName,
    decimal Price,
    string Currency,
    string Origin,
    double Similarity);

public sealed record SearchSuggestionDto(
    string Text,
    string Href,
    string? Category = null);
