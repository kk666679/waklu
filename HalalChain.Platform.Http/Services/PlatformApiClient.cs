using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Halal.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Http.Models;
using Microsoft.Extensions.Logging;

namespace HalalChain.Platform.Http.Services;

/// <summary>
/// Single concrete implementation of <see cref="IPlatformApiClient"/>. This
/// class is a pass-through over <see cref="PlatformApiSender"/>: it does not
/// cache, mock, or fabricate any data. Callers must inspect
/// <see cref="ApiResult{T}.IsSuccess"/>, <see cref="ApiResult{T}.Status"/>,
/// and <see cref="ApiResult{T}.Error"/> and render appropriate loading,
/// empty, or error UI states.
/// </summary>
public sealed class PlatformApiClient : IPlatformApiClient
{
    private readonly PlatformApiSender _sender;
    private readonly ILogger<PlatformApiClient> _logger;

    public PlatformApiClient(PlatformApiSender sender, ILogger<PlatformApiClient> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    private const string Base = "api/v1";

    private static string Qs(string key, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $"&{key}={Uri.EscapeDataString(value)}";

    private void LogIfFailure<T>(string operation, ApiResult<T> result)
    {
        if (!result.IsSuccess && result.Status != ApiStatus.Unauthorized && result.Status != ApiStatus.Forbidden)
        {
            _logger.LogWarning(
                "Platform API call failed: {Operation} -> {Status} {Error}",
                operation,
                result.Status,
                result.Error);
        }
    }

    // ── Catalog ─────────────────────────────────────────────────────

    public async Task<ApiResult<ProductDto[]>> GetProductsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(category)) qs += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrEmpty(search)) qs += $"&search={Uri.EscapeDataString(search)}";

        var result = await _sender.GetAsync<PagedResult<ProductDto>>($"{Base}/catalog/products?{qs}", ct);
        LogIfFailure(nameof(GetProductsAsync), result);
        if (!result.IsSuccess)
        {
            return ApiResult<ProductDto[]>.Fail(result.Status, result.Error ?? "Unknown error");
        }
        return ApiResult<ProductDto[]>.Ok(result.Data?.Items ?? []);
    }

    public async Task<ApiResult<ProductDto?>> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<ProductDto?>($"{Base}/catalog/products/{id}", ct);
        LogIfFailure(nameof(GetProductByIdAsync), result);
        return result;
    }

    public async Task<ApiResult<CategoryDto[]>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<CategoryDto[]>($"{Base}/catalog/categories", ct);
        LogIfFailure(nameof(GetCategoriesAsync), result);
        return result;
    }

    public async Task<ApiResult<DepartmentDto[]>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<DepartmentDto[]>($"{Base}/catalog/departments", ct);
        LogIfFailure(nameof(GetDepartmentsAsync), result);
        return result;
    }

    public async Task<ApiResult<TaxonomyCategoryDto[]>> GetCategoriesForDepartmentAsync(
        string departmentSlug,
        CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<TaxonomyCategoryDto[]>(
            $"{Base}/catalog/departments/{Uri.EscapeDataString(departmentSlug)}/categories", ct);
        LogIfFailure(nameof(GetCategoriesForDepartmentAsync), result);
        return result;
    }

    public Task<ApiResult<PagedResult<EnrichedProductDto>>> BrowseAsync(
        ProductQuery q,
        CancellationToken ct = default)
    {
        var parts = new List<string>
        {
            $"page={q.Page}", $"pageSize={q.PageSize}", $"sortBy={Uri.EscapeDataString(q.SortBy ?? "newest")}"
        };
        parts.Add(Qs("path", q.Path).TrimStart('&'));
        parts.Add(Qs("department", q.Department).TrimStart('&'));
        parts.Add(Qs("category", q.Category).TrimStart('&'));
        parts.Add(Qs("subcategory", q.Subcategory).TrimStart('&'));
        parts.Add(Qs("type", q.Type).TrimStart('&'));
        parts.Add(Qs("country", q.Country).TrimStart('&'));
        parts.Add(Qs("search", q.Search).TrimStart('&'));
        if (q.Status.HasValue) parts.Add($"status={q.Status}");
        if (q.Dietary.HasValue && q.Dietary != 0) parts.Add($"dietary={q.Dietary}");
        if (q.Channel.HasValue) parts.Add($"channel={q.Channel}");
        if (q.MinPrice.HasValue) parts.Add($"minPrice={q.MinPrice}");
        if (q.MaxPrice.HasValue) parts.Add($"maxPrice={q.MaxPrice}");
        var url = $"{Base}/catalog/products/filter?" + string.Join("&", parts.Where(p => !string.IsNullOrEmpty(p)));
        return _sender.GetAsync<PagedResult<EnrichedProductDto>>(url, ct);
    }

    public Task<ApiResult<EnrichedProductDto?>> GetEnrichedProductAsync(Guid id, CancellationToken ct = default) =>
        _sender.GetAsync<EnrichedProductDto?>($"{Base}/catalog/products/{id}/enriched", ct);

    public async Task<ApiResult<PagedResult<EnrichedProductDto>>> GetFilteredProductsAsync(
        string? path = null, string? type = null, string? department = null,
        string? category = null, string? subcategory = null,
        int? status = null, int? dietary = null, string? country = null,
        int? channel = null, decimal? minPrice = null, decimal? maxPrice = null,
        string? search = null, string? sortBy = "newest",
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        void Add(string k, string? v) { if (!string.IsNullOrEmpty(v)) parts.Add($"{k}={Uri.EscapeDataString(v)}"); }
        Add("path", path); Add("type", type); Add("department", department);
        Add("category", category); Add("subcategory", subcategory);
        if (status.HasValue) parts.Add($"status={status.Value}");
        if (dietary.HasValue && dietary.Value != 0) parts.Add($"dietary={dietary.Value}");
        Add("country", country);
        if (channel.HasValue) parts.Add($"channel={channel.Value}");
        if (minPrice.HasValue) parts.Add($"minPrice={minPrice.Value}");
        if (maxPrice.HasValue) parts.Add($"maxPrice={maxPrice.Value}");
        Add("search", search);
        Add("sortBy", sortBy);

        var result = await _sender.GetAsync<PagedResult<EnrichedProductDto>>(
            $"{Base}/catalog/products/filter?{string.Join("&", parts)}", ct);
        LogIfFailure(nameof(GetFilteredProductsAsync), result);
        return result;
    }

    public Task<ApiResult<CategoryFiltersDto?>> GetFiltersAsync(CancellationToken ct = default) =>
        _sender.GetAsync<CategoryFiltersDto?>($"{Base}/catalog/filters", ct);

    // ── Vendors ──────────────────────────────────────────────────────

    public async Task<ApiResult<VendorDto[]>> GetVendorsAsync(CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<VendorDto[]>($"{Base}/vendors", ct);
        LogIfFailure(nameof(GetVendorsAsync), result);
        return result;
    }

    public async Task<ApiResult<VendorDto?>> GetVendorByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<VendorDto?>($"{Base}/vendors/{id}", ct);
        LogIfFailure(nameof(GetVendorByIdAsync), result);
        return result;
    }

    public Task<ApiResult<PagedResult<ProductDto>>> GetVendorProductsAsync(
        string vendor, int page = 1, int pageSize = 24, CancellationToken ct = default) =>
        _sender.GetAsync<PagedResult<ProductDto>>(
            $"{Base}/catalog/products?vendor={Uri.EscapeDataString(vendor)}&page={page}&pageSize={pageSize}", ct);

    // ── Product writes (vendor) ──────────────────────────────────────

    public Task<ApiResult<ProductDto?>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default) =>
        _sender.PostAsync<ProductDto?>($"{Base}/catalog/products", request, ct);

    public Task<ApiResult<ProductDto?>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default) =>
        _sender.PatchAsync<ProductDto?>($"{Base}/catalog/products/{id}", request, ct);

    // ── Cart / Checkout ─────────────────────────────────────────────

    public Task<ApiResult<CartDto?>> GetCartAsync(CancellationToken ct = default) =>
        _sender.GetAsync<CartDto?>($"{Base}/commerce/cart", ct);

    public Task<ApiResult<CartDto?>> AddToCartAsync(AddToCartRequest request, CancellationToken ct = default) =>
        _sender.PostAsync<CartDto?>($"{Base}/commerce/cart/items", request, ct);

    public Task<ApiResult<CartDto?>> UpdateCartItemAsync(UpdateCartItemRequest request, CancellationToken ct = default) =>
        _sender.PatchAsync<CartDto?>($"{Base}/commerce/cart/items", request, ct);

    public Task<ApiResult<OrderDto?>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default) =>
        _sender.PostAsync<OrderDto?>($"{Base}/commerce/checkout", request, ct);

    public async Task<ApiResult<OrderDto[]>> GetOrdersAsync(CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<OrderDto[]>($"{Base}/commerce/orders", ct);
        LogIfFailure(nameof(GetOrdersAsync), result);
        return result;
    }

    public async Task<ApiResult<OrderDto?>> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<OrderDto?>($"{Base}/commerce/orders/{id}", ct);
        LogIfFailure(nameof(GetOrderByIdAsync), result);
        return result;
    }

    // ── Halal verification ──────────────────────────────────────────

    public Task<ApiResult<HalalCertificateDto?>> SubmitCertificateAsync(
        Guid productId, SubmitCertificateRequest request, CancellationToken ct = default) =>
        _sender.PostAsync<HalalCertificateDto?>(
            $"{Base}/halal/products/{productId}/certificates", request, ct);

    public Task<ApiResult<VerificationDto?>> TriggerVerificationAsync(
        Guid productId, TriggerVerificationRequest request, CancellationToken ct = default) =>
        _sender.PostAsync<VerificationDto?>(
            $"{Base}/halal/products/{productId}/verify", request, ct);

    public Task<ApiResult<VerificationDto?>> GetVerificationStatusAsync(
        Guid productId, CancellationToken ct = default) =>
        _sender.GetAsync<VerificationDto?>($"{Base}/halal/products/{productId}/verification", ct);

    public Task<ApiResult<System.Text.Json.JsonElement?>> GetPublicVerificationAsync(
        string productId, CancellationToken ct = default) =>
        _sender.GetAsync<System.Text.Json.JsonElement?>($"{Base}/verify/{productId}", ct);

    // ── Auth ────────────────────────────────────────────────────────

    public Task<ApiResult<LoginResponse?>> LoginAsync(string email, string password, CancellationToken ct = default) =>
        _sender.PostAsync<LoginResponse?>("api/auth/login", new { email, password }, ct);

    public Task<ApiResult<LoginResponse?>> RegisterAsync(
        string email, string password, string name, CancellationToken ct = default) =>
        _sender.PostAsync<LoginResponse?>("api/auth/register", new { email, password, name }, ct);

    // ── Search ──────────────────────────────────────────────────────

    public async Task<ApiResult<SemanticSearchResult[]>> SemanticSearchAsync(
        string query, int topK = 10, CancellationToken ct = default)
    {
        var url = $"{Base}/search/semantic?q={Uri.EscapeDataString(query)}&topK={topK}";
        var result = await _sender.GetAsync<SemanticSearchResult[]>(url, ct);
        LogIfFailure(nameof(SemanticSearchAsync), result);
        if (!result.IsSuccess)
        {
            return ApiResult<SemanticSearchResult[]>.Fail(result.Status, result.Error ?? "Unknown error");
        }
        return ApiResult<SemanticSearchResult[]>.Ok(result.Data ?? []);
    }

    public async Task<ApiResult<SearchSuggestionDto[]>> GetSearchSuggestionsAsync(
        string query, int limit = 8, CancellationToken ct = default)
    {
        var result = await _sender.GetAsync<SearchSuggestionDto[]>(
            $"{Base}/search/suggestions?q={Uri.EscapeDataString(query)}&limit={limit}", ct);
        LogIfFailure(nameof(GetSearchSuggestionsAsync), result);
        if (!result.IsSuccess)
        {
            return ApiResult<SearchSuggestionDto[]>.Fail(result.Status, result.Error ?? "Unknown error");
        }
        return ApiResult<SearchSuggestionDto[]>.Ok(result.Data ?? []);
    }
}
