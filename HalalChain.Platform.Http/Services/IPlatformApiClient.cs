using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Platform.Http.Services;

/// <summary>
/// Strongly-typed client for the canonical HalalChain Platform API.
/// This surface is contract-first: it returns DTOs from
/// <c>HalalChain.Platform.Contracts</c> and never substitutes seed or
/// fabricated data when the upstream request fails. Hosts are expected
/// to react to <see cref="ApiResult{T}.IsSuccess"/>,
/// <see cref="ApiResult{T}.Status"/>, and <see cref="ApiResult{T}.Error"/>
/// with explicit loading/empty/error UI states.
/// </summary>
public interface IPlatformApiClient
{
    // ── Catalog ────────────────────────────────────────────────────────
    Task<ApiResult<ProductDto[]>> GetProductsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<ProductDto?>> GetProductByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Alias of <see cref="GetProductByIdAsync"/> kept for hosts
    /// that previously used a different method name.</summary>
    Task<ApiResult<ProductDto?>> GetProductAsync(Guid id, CancellationToken ct = default) =>
        GetProductByIdAsync(id, ct);

    Task<ApiResult<CategoryDto[]>> GetCategoriesAsync(CancellationToken ct = default);

    Task<ApiResult<DepartmentDto[]>> GetDepartmentsAsync(CancellationToken ct = default);

    Task<ApiResult<TaxonomyCategoryDto[]>> GetCategoriesForDepartmentAsync(
        string departmentSlug,
        CancellationToken ct = default);

    Task<ApiResult<PagedResult<EnrichedProductDto>>> BrowseAsync(
        ProductQuery query,
        CancellationToken ct = default);

    Task<ApiResult<EnrichedProductDto?>> GetEnrichedProductAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ApiResult<PagedResult<EnrichedProductDto>>> GetFilteredProductsAsync(
        string? path = null,
        string? type = null,
        string? department = null,
        string? category = null,
        string? subcategory = null,
        int? status = null,
        int? dietary = null,
        string? country = null,
        int? channel = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? search = null,
        string? sortBy = "newest",
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<CategoryFiltersDto?>> GetFiltersAsync(CancellationToken ct = default);

    // ── Vendors ────────────────────────────────────────────────────────
    Task<ApiResult<VendorDto[]>> GetVendorsAsync(CancellationToken ct = default);

    Task<ApiResult<VendorDto?>> GetVendorByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Alias of <see cref="GetVendorByIdAsync"/> kept for hosts
    /// that previously used a different method name.</summary>
    Task<ApiResult<VendorDto?>> GetVendorAsync(Guid id, CancellationToken ct = default) =>
        GetVendorByIdAsync(id, ct);

    Task<ApiResult<PagedResult<ProductDto>>> GetVendorProductsAsync(
        string vendor,
        int page = 1,
        int pageSize = 24,
        CancellationToken ct = default);

    // ── Product writes (vendor) ────────────────────────────────────────
    Task<ApiResult<ProductDto?>> CreateProductAsync(
        HalalChain.Platform.Contracts.Catalog.Requests.CreateProductRequest request,
        CancellationToken ct = default);

    Task<ApiResult<ProductDto?>> UpdateProductAsync(
        Guid id,
        HalalChain.Platform.Contracts.Catalog.Requests.UpdateProductRequest request,
        CancellationToken ct = default);

    // ── Cart / Checkout ───────────────────────────────────────────────
    Task<ApiResult<CartDto?>> GetCartAsync(CancellationToken ct = default);

    Task<ApiResult<CartDto?>> AddToCartAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.AddToCartRequest request,
        CancellationToken ct = default);

    /// <summary>Convenience overload that wraps <see cref="AddToCartAsync"/>
    /// for hosts that only have a product id and quantity available.</summary>
    Task<ApiResult<CartDto?>> AddToCartAsync(
        Guid productId,
        int quantity,
        CancellationToken ct = default) =>
        AddToCartAsync(
            new HalalChain.Platform.Contracts.Commerce.Requests.AddToCartRequest(productId, quantity),
            ct);

    Task<ApiResult<CartDto?>> UpdateCartItemAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.UpdateCartItemRequest request,
        CancellationToken ct = default);

    /// <summary>Convenience overload that wraps <see cref="UpdateCartItemAsync"/>
    /// for hosts that only have a product id and quantity available.</summary>
    Task<ApiResult<CartDto?>> UpdateCartAsync(
        Guid productId,
        int quantity,
        CancellationToken ct = default) =>
        UpdateCartItemAsync(
            new HalalChain.Platform.Contracts.Commerce.Requests.UpdateCartItemRequest(productId, quantity),
            ct);

    Task<ApiResult<OrderDto?>> CheckoutAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.CheckoutRequest request,
        CancellationToken ct = default);

    /// <summary>Convenience overload of <see cref="CheckoutAsync"/> for hosts
    /// that compose the shipping address inline and supply an optional
    /// payment token.</summary>
    Task<ApiResult<OrderDto?>> CheckoutAsync(
        string shippingAddress,
        string? paymentToken,
        CancellationToken ct = default) =>
        CheckoutAsync(
            new HalalChain.Platform.Contracts.Commerce.Requests.CheckoutRequest(
                shippingAddress,
                paymentToken ?? "demo-token",
                Guid.NewGuid().ToString()),
            ct);

    Task<ApiResult<OrderDto[]>> GetOrdersAsync(CancellationToken ct = default);

    Task<ApiResult<OrderDto?>> GetOrderByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Alias of <see cref="GetOrderByIdAsync"/> kept for hosts
    /// that previously used a different method name.</summary>
    Task<ApiResult<OrderDto?>> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        GetOrderByIdAsync(id, ct);

    // ── Halal verification ────────────────────────────────────────────
    Task<ApiResult<HalalCertificateDto?>> SubmitCertificateAsync(
        Guid productId,
        HalalChain.Platform.Contracts.Halal.Requests.SubmitCertificateRequest request,
        CancellationToken ct = default);

    Task<ApiResult<VerificationDto?>> TriggerVerificationAsync(
        Guid productId,
        HalalChain.Platform.Contracts.Halal.Requests.TriggerVerificationRequest request,
        CancellationToken ct = default);

    Task<ApiResult<VerificationDto?>> GetVerificationStatusAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>Alias of <see cref="GetVerificationStatusAsync"/> kept for
    /// hosts that previously used a different method name.</summary>
    Task<ApiResult<VerificationDto?>> GetVerificationAsync(
        Guid productId,
        CancellationToken ct = default) =>
        GetVerificationStatusAsync(productId, ct);

    Task<ApiResult<System.Text.Json.JsonElement?>> GetPublicVerificationAsync(
        string productId,
        CancellationToken ct = default);

    // ── Auth ──────────────────────────────────────────────────────────
    Task<ApiResult<LoginResponse?>> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default);

    Task<ApiResult<LoginResponse?>> RegisterAsync(
        string email,
        string password,
        string name,
        CancellationToken ct = default);

    // ── Search ────────────────────────────────────────────────────────
    Task<ApiResult<SemanticSearchResult[]>> SemanticSearchAsync(
        string query,
        int topK = 10,
        CancellationToken ct = default);

    /// <summary>Alias of <see cref="SemanticSearchAsync"/> kept for hosts
    /// that previously used a different method name.</summary>
    Task<ApiResult<SemanticSearchResult[]>> GetRecommendationsAsync(
        string query,
        int topK = 6,
        CancellationToken ct = default) =>
        SemanticSearchAsync(query, topK, ct);

    Task<ApiResult<SearchSuggestionDto[]>> GetSearchSuggestionsAsync(
        string query,
        int limit = 8,
        CancellationToken ct = default);
}
