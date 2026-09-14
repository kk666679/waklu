using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Halal.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;
using HalalChain.Platform.Http.Models;
using HalalChain.Platform.Http.Services;

namespace HalalChain.Services;

/// <summary>
/// Web host convenience layer over the shared <see cref="IPlatformApiClient"/>.
/// This class contains <strong>no seed data</strong> — when the upstream API
/// fails it logs the failure and returns an empty collection or <c>null</c>.
/// Pages that need a "no data" or "API unavailable" UI state should observe
/// the data array and render an appropriate empty/error component.
///
/// Behavioural change from the previous Web client: the old implementation
/// fabricated seed products, vendors, and orders on every API failure. That
/// hidden fallback is gone; the only thing this shim does is unwrap
/// <see cref="ApiResult{T}"/> into the legacy "return T? / T[]" shape that
/// Razor pages were written against.
/// </summary>
public sealed class WebApiClient
{
    private readonly IPlatformApiClient _api;
    private readonly ILogger<WebApiClient> _logger;

    public WebApiClient(IPlatformApiClient api, ILogger<WebApiClient> logger)
    {
        _api = api;
        _logger = logger;
    }

    // ── Catalog ─────────────────────────────────────────────────────

    public async Task<ProductDto[]> GetProductsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _api.GetProductsAsync(category, search, page, pageSize, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetProductsAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<CategoryDto[]> GetCategoriesAsync(CancellationToken ct = default)
    {
        var result = await _api.GetCategoriesAsync(ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetCategoriesAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<DepartmentDto[]> GetDepartmentsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetDepartmentsAsync(ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetDepartmentsAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<TaxonomyCategoryDto[]> GetCategoriesForDepartmentAsync(
        string departmentSlug,
        CancellationToken ct = default)
    {
        var result = await _api.GetCategoriesForDepartmentAsync(departmentSlug, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetCategoriesForDepartmentAsync failed: {Status} {Error}",
                result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<CategoryFiltersDto?> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetFiltersAsync(ct);
        if (!result.IsSuccess && result.Status != ApiStatus.NotFound)
        {
            _logger.LogWarning("GetFiltersAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data;
    }

    // ── Vendors ──────────────────────────────────────────────────────

    public async Task<VendorDto[]> GetVendorsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetVendorsAsync(ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetVendorsAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<VendorDto?> RegisterVendorAsync(RegisterVendorRequest request, CancellationToken ct = default)
    {
        // No first-class endpoint on the shared client for vendor self-registration
        // today. Forward to the platform's POST /api/v1/vendors via the raw
        // contract call. If a dedicated method is added later, route through it.
        throw new NotSupportedException(
            "Vendor self-registration is not yet exposed by the shared client. " +
            "Use the /vendor portal endpoints on the platform API directly.");
    }

    public async Task<VendorDto?> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Vendor updates are not yet exposed by the shared client.");
    }

    public async Task<bool> DeleteVendorAsync(Guid id, CancellationToken ct = default)
    {
        // No DELETE /vendors/{id} surface on the shared client today. Surface
        // the limitation explicitly rather than fabricating success.
        throw new NotSupportedException(
            "Vendor deletion is not yet exposed by the shared client.");
    }

    // ── Orders / Cart ────────────────────────────────────────────────

    public async Task<OrderDto[]> GetOrdersAsync(CancellationToken ct = default)
    {
        var result = await _api.GetOrdersAsync(ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetOrdersAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data ?? [];
    }

    public async Task<OrderDto?> CheckoutAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.CheckoutRequest request,
        CancellationToken ct = default)
    {
        var result = await _api.CheckoutAsync(request, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("CheckoutAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data;
    }

    public async Task<CartDto?> GetCartAsync(CancellationToken ct = default)
    {
        var result = await _api.GetCartAsync(ct);
        if (!result.IsSuccess && result.Status != ApiStatus.NotFound && result.Status != ApiStatus.Unauthorized)
        {
            _logger.LogWarning("GetCartAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data;
    }

    public async Task<CartDto?> AddToCartAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.AddToCartRequest request,
        CancellationToken ct = default)
    {
        var result = await _api.AddToCartAsync(request, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("AddToCartAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data;
    }

    public async Task<CartDto?> UpdateCartItemAsync(
        HalalChain.Platform.Contracts.Commerce.Requests.UpdateCartItemRequest request,
        CancellationToken ct = default)
    {
        var result = await _api.UpdateCartItemAsync(request, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("UpdateCartItemAsync failed: {Status} {Error}", result.Status, result.Error);
        }
        return result.Data;
    }

    // ── Halal verification ──────────────────────────────────────────

    public async Task<VerificationDto?> TriggerVerificationAsync(
        Guid productId,
        TriggerVerificationRequest request,
        CancellationToken ct = default)
    {
        var result = await _api.TriggerVerificationAsync(productId, request, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("TriggerVerificationAsync failed: {Status} {Error}",
                result.Status, result.Error);
        }
        return result.Data;
    }
}
