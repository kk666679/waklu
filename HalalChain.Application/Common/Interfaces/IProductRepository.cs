using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;

namespace HalalChain.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, Guid vendorId, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid vendorId, bool isAdmin, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> ListAsync(string? category, string? halalStatus, decimal? minPrice, decimal? maxPrice, string? vendor, string? search, string? sortBy, string? sortOrder, int page, int pageSize, CancellationToken ct = default);
    Task<EnrichedProductDto?> GetEnrichedAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsOwnedByVendorAsync(Guid productId, Guid vendorId, CancellationToken ct = default);
}
