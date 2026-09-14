using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Http.Services;

namespace HalalChain.Marketplace.Models;

public sealed record HomeViewModel(
    DepartmentDto[] Departments,
    EnrichedProductDto[] Featured,
    EnrichedProductDto[] Verified,
    EnrichedProductDto[] Fresh,
    VendorDto[] Vendors,
    bool ApiUnavailable);

public sealed record CartVendorGroup(string VendorName, CartItemDto[] Items, decimal Subtotal, string Currency);
public sealed record CartViewModel(CartVendorGroup[] Groups, decimal Total, string Currency);

public sealed record BrowseViewModel(
    EnrichedProductDto[] Products,
    CategoryFiltersDto? Filters,
    DepartmentDto[] Departments,
    ProductQuery Query,
    int TotalCount,
    int TotalPages);

public sealed record ProductDetailViewModel(
    EnrichedProductDto Product,
    VerificationDto? Verification,
    SemanticSearchResult[] Recommendations,
    EnrichedProductDto[] Related);

public sealed record VendorStoreViewModel(VendorDto Vendor, ProductDto[] Products, int TotalProducts);

public sealed record VendorDashboardViewModel(
    VendorDto Vendor, int TotalProducts, int LowStock, int OutOfStock,
    int Verified, int PendingVerification, ProductDto[] Products);

public sealed record ProductEditorViewModel(Guid? Id, CreateProductRequest Request, ProductDto? Existing);

public sealed record OnChainInfo(string Source, string Status, string[] Warnings, string? CertificateId, string? Certifier);
public sealed record VerifyViewModel(ProductDto? Product, VerificationDto? Verification, OnChainInfo? OnChain, bool OnChainAvailable);

public sealed record AdminViewModel(
    VendorDto[] Vendors,
    EnrichedProductDto[] Products,
    int TotalProducts,
    int VerifiedProducts,
    int TotalCategories);
