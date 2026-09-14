using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Halal.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;
using HalalChain.Platform.Contracts.Catalog.Requests;
using Microsoft.IdentityModel.Tokens;

namespace HalalChain.Services;

public sealed class PlatformApiClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<PlatformApiClient> _logger;
    private readonly IAuthService _auth;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public PlatformApiClient(IHttpClientFactory f, IConfiguration c, ILogger<PlatformApiClient> logger, IAuthService auth)
    {
        _http = f.CreateClient("PlatformApi");
        _config = c;
        _logger = logger;
        _auth = auth;
    }

    private string BaseUrl => "api/v1";

    public async Task<ProductDto[]> GetProductsAsync(string? category = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/catalog/products?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(category)) url += $"&category={Uri.EscapeDataString(category)}";
            if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
            var r = await AuthedGet(url, ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<PagedResult<ProductDto>>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result?.Items.Length > 0) return result.Items;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "API unreachable, using seed data"); }
        return GetSeedProducts();
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/catalog/products/{id}", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<ProductDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "API unreachable"); }
        return GetSeedProducts().FirstOrDefault(p => p.Id == id);
    }

    public async Task<CategoryDto[]> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/catalog/categories", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<CategoryDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "API unreachable, using seed data"); }
        return GetSeedCategories();
    }

    // ── Redesigned taxonomy API ─────────────────────────────────────

    /// <summary>Fetches the 8 top-level departments for the main nav.
    /// Falls back to a built-in list if the API is unreachable.</summary>
    public async Task<DepartmentDto[]> GetDepartmentsAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/catalog/departments", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<DepartmentDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Departments API unreachable"); }
        return GetSeedDepartments();
    }

    /// <summary>Fetches the 2nd-level categories for a department slug.</summary>
    public async Task<TaxonomyCategoryDto[]> GetCategoriesForDepartmentAsync(string departmentSlug, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/catalog/departments/{Uri.EscapeDataString(departmentSlug)}/categories", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<TaxonomyCategoryDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Categories API unreachable for {Dept}", departmentSlug); }
        return [];
    }

    /// <summary>Filtered product list using the redesigned JSONB-backed endpoint.</summary>
    public async Task<EnrichedProductDto[]> GetFilteredProductsAsync(
        string? path = null, string? type = null, string? department = null,
        string? category = null, string? subcategory = null,
        int? status = null, int? dietary = null, string? country = null,
        int? channel = null, decimal? minPrice = null, decimal? maxPrice = null,
        string? search = null, string? sortBy = "newest",
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            void Add(string k, string? v) { if (!string.IsNullOrEmpty(v)) qs.Add($"{k}={Uri.EscapeDataString(v)}"); }
            Add("path", path); Add("type", type); Add("department", department);
            Add("category", category); Add("subcategory", subcategory);
            if (status.HasValue) qs.Add($"status={status.Value}");
            if (dietary.HasValue && dietary.Value != 0) qs.Add($"dietary={dietary.Value}");
            Add("country", country);
            if (channel.HasValue) qs.Add($"channel={channel.Value}");
            if (minPrice.HasValue) qs.Add($"minPrice={minPrice.Value}");
            if (maxPrice.HasValue) qs.Add($"maxPrice={maxPrice.Value}");
            Add("search", search);
            Add("sortBy", sortBy);
            var url = $"{BaseUrl}/catalog/products/filter?{string.Join("&", qs)}";
            var r = await AuthedGet(url, ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<PagedResult<EnrichedProductDto>>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result?.Items != null) return result.Items;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Filter API unreachable, using seed data"); }
        // Fallback: return empty list (the seed ProductDto has a different shape
        // than EnrichedProductDto, so we don't cast — callers handle empty).
        return [];
    }

    /// <summary>Returns the full filter rail (cert bodies, countries, dietary tags, statuses).</summary>
    public async Task<CategoryFiltersDto?> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/catalog/filters", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<CategoryFiltersDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Filters API unreachable"); }
        return null;
    }

    /// <summary>Fallback department list (used when the API is unreachable so the
    /// storefront mega-menu still renders something useful).</summary>
    private static DepartmentDto[] GetSeedDepartments() =>
    [
        new DepartmentDto(Guid.Parse("D1000000-0000-0000-0000-000000000001"), "Food & Beverage",         "food-beverage",  "Halal-certified food and drink across all categories.", "fa-utensils", 1, 13, 0),
        new DepartmentDto(Guid.Parse("D2000000-0000-0000-0000-000000000002"), "Personal Care & Beauty",  "personal-care",  "Skincare, haircare, fragrance, bath & body, oral care.",    "fa-spray-can-sparkles", 2, 6, 0),
        new DepartmentDto(Guid.Parse("D3000000-0000-0000-0000-000000000003"), "Health, Wellness & Nutrition", "health-wellness", "Vitamins, supplements, herbal products, healthcare.", "fa-heart-pulse", 3, 4, 0),
        new DepartmentDto(Guid.Parse("D4000000-0000-0000-0000-000000000004"), "Household & Home",       "household",      "Cleaning, kitchen, home fragrance, home essentials.",       "fa-house", 4, 4, 0),
        new DepartmentDto(Guid.Parse("D5000000-0000-0000-0000-000000000005"), "Modest Fashion & Lifestyle", "modest-fashion", "Modest clothing, hijab, footwear, accessories.",         "fa-shirt", 5, 5, 0),
        new DepartmentDto(Guid.Parse("D6000000-0000-0000-0000-000000000006"), "Islamic & Religious Essentials", "islamic-religious", "Qurans, prayer, Islamic books, gifts.",            "fa-mosque", 6, 4, 0),
        new DepartmentDto(Guid.Parse("D7000000-0000-0000-0000-000000000007"), "Baby, Mother & Maternity", "baby-mother",    "Baby care, mother & maternity, feeding.",                 "fa-baby-carriage", 7, 4, 0),
        new DepartmentDto(Guid.Parse("D8000000-0000-0000-0000-000000000008"), "Business, B2B & Wholesale", "b2b",            "Bulk ingredients, OEM, private label, HoReCa supplies.", "fa-building", 8, 6, 0),
    ];

    public async Task<VendorDto[]> GetVendorsAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/vendors", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<VendorDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "API unreachable, using seed data"); }
        return GetSeedVendors();
    }

    public async Task<VendorDto?> GetVendorByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/vendors/{id}", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<VendorDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Vendor API unreachable"); }
        return GetSeedVendors().FirstOrDefault(v => v.Id == id);
    }

    public async Task<VendorDto?> RegisterVendorAsync(RegisterVendorRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await _http.PostAsJsonAsync($"{BaseUrl}/vendors", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<VendorDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Vendor registration failed"); }
        return null;
    }

    public async Task<VendorDto?> UpdateVendorAsync(Guid id, UpdateVendorRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPatch($"{BaseUrl}/vendors/{id}", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<VendorDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Vendor update failed"); }
        return null;
    }

    public async Task<bool> DeleteVendorAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedDelete($"{BaseUrl}/vendors/{id}", ct);
            return r.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Vendor delete failed"); }
        return false;
    }

    private async Task<HttpResponseMessage> AuthedDelete(string path, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, path);
        await AddAuthHeaderAsync(req);
        return await _http.SendAsync(req, ct);
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPost($"{BaseUrl}/catalog/products", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<ProductDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Product creation failed"); }
        return null;
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPatch($"{BaseUrl}/catalog/products/{id}", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<ProductDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Product update failed"); }
        return null;
    }

    private async Task<HttpResponseMessage> AuthedGet(string path, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        await AddAuthHeaderAsync(req);
        return await _http.SendAsync(req, ct);
    }

    private async Task<HttpResponseMessage> AuthedPost<T>(string path, T body, CancellationToken ct)
    {
        var content = JsonSerializer.Serialize(body, _jsonOptions);
        var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
        await AddAuthHeaderAsync(req);
        return await _http.SendAsync(req, ct);
    }

    private async Task<HttpResponseMessage> AuthedPatch<T>(string path, T body, CancellationToken ct)
    {
        var content = JsonSerializer.Serialize(body, _jsonOptions);
        var req = new HttpRequestMessage(HttpMethod.Patch, path) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
        await AddAuthHeaderAsync(req);
        return await _http.SendAsync(req, ct);
    }

    private async Task AddAuthHeaderAsync(HttpRequestMessage req)
    {
        try
        {
            var token = _auth.Token;
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to attach auth header");
        }
    }

    public async Task<CartDto?> GetCartAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/commerce/cart", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<CartDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Cart API unreachable"); }
        return null;
    }

    public async Task<CartDto?> AddToCartAsync(AddToCartRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPost($"{BaseUrl}/commerce/cart/items", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<CartDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Add to cart failed"); }
        return null;
    }

    public async Task<CartDto?> UpdateCartItemAsync(UpdateCartItemRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPatch($"{BaseUrl}/commerce/cart/items", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<CartDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Update cart failed"); }
        return null;
    }

    public async Task<OrderDto?> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPost($"{BaseUrl}/commerce/checkout", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<OrderDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Checkout failed"); }
        return null;
    }

    public async Task<OrderDto[]> GetOrdersAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/commerce/orders", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<OrderDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Orders API unreachable, using seed data"); }
        return GetSeedOrders();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/commerce/orders/{id}", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<OrderDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Order API unreachable"); }
        return GetSeedOrders().FirstOrDefault(o => o.Id == id);
    }

    public async Task<HalalCertificateDto?> SubmitCertificateAsync(Guid productId, SubmitCertificateRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPost($"{BaseUrl}/halal/products/{productId}/certificates", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<HalalCertificateDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Certificate submission failed"); }
        return null;
    }

    public async Task<VerificationDto?> TriggerVerificationAsync(Guid productId, TriggerVerificationRequest request, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedPost($"{BaseUrl}/halal/products/{productId}/verify", request, ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<VerificationDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Verification trigger failed"); }
        return null;
    }

    public async Task<VerificationDto?> GetVerificationStatusAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/halal/products/{productId}/verification", ct);
            r.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<VerificationDto>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Verification status API unreachable"); }
        return null;
    }

    public async Task<CustomerDto[]> GetCustomersAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await AuthedGet($"{BaseUrl}/commerce/customers", ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<CustomerDto[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Customers API unreachable, using seed data"); }
        return GetSeedCustomers();
    }

    public async Task<bool> CheckHealthAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var r = await _http.GetAsync(url, ct);
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<SemanticSearchResult[]> SemanticSearchAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/search/semantic?q={Uri.EscapeDataString(query)}&topK={topK}";
            var r = await AuthedGet(url, ct);
            r.EnsureSuccessStatusCode();
            var result = await JsonSerializer.DeserializeAsync<SemanticSearchResult[]>(await r.Content.ReadAsStreamAsync(ct), _jsonOptions, ct);
            if (result != null && result.Length > 0) return result;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Semantic search API unreachable"); }
        return GetSeedSemanticResults(query);
    }

    private static SemanticSearchResult[] GetSeedSemanticResults(string query)
    {
        var allProducts = GetSeedProducts();
        return allProducts
            .Where(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       (p.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(5)
            .Select(p => new SemanticSearchResult(
                p.Id,
                p.Title,
                p.Slug,
                p.Description,
                p.VendorName,
                p.CategoryName,
                p.Price,
                p.Currency,
                p.Origin,
                0.8 + Random.Shared.NextDouble() * 0.2))
            .ToArray();
    }

    private static ProductDto[] GetSeedProducts() =>
    [
        new(Guid.NewGuid(), "Organic Coconut Water", "organic-coconut-water", "Pure organic coconut water from certified halal farms.", "Beverages", "Pacific Harvest Co.", "Malaysia", 8.90m, "MYR", 150, new("Verified", "HC-2024-001", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Premium Chicken Nuggets", "premium-chicken-nuggets", "Crispy halal-certified chicken nuggets.", "Meat & Poultry", "Al-Falah Foods", "Malaysia", 24.90m, "MYR", 85, new("Verified", "HC-2024-002", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Traditional Kuih Lapis", "traditional-kuih-lapis", "Authentic layered rice cake, handmade.", "Snacks", "Heritage Sweets", "Indonesia", 15.50m, "MYR", 200, new("Verified", "HC-2024-003", "MUI", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Mango Fruit Juice", "mango-fruit-juice", "Refreshing pure mango juice, no sugar.", "Beverages", "Tropical Press", "Thailand", 6.50m, "MYR", 300, new("Verified", "HC-2024-004", "MUIS", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Lamb Rendang Paste", "lamb-rendang-paste", "Ready-to-cook rendang spice paste.", "Condiments", "Spice Garden MY", "Malaysia", 12.00m, "MYR", 120, new("Verified", "HC-2024-005", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Basmati Rice 5kg", "basmati-rice-5kg", "Premium long-grain basmati rice.", "Grains & Staples", "Golden Harvest", "Pakistan", 32.00m, "MYR", 400, new("Verified", "HC-2024-006", "ESMA", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Turkish Delight", "turkish-delight", "Handcrafted Turkish delight, rose and pistachio.", "Sweets", "Istanbul Sweets Co.", "Turkey", 18.00m, "MYR", 75, new("Verified", "HC-2024-007", "Halal Food Council", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Chicken Satay Skewers", "chicken-satay-skewers", "Marinated chicken satay, ready to grill.", "Meat & Poultry", "Al-Falah Foods", "Malaysia", 19.90m, "MYR", 60, new("Verified", "HC-2024-008", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Matcha Green Tea", "matcha-green-tea", "Ceremonial grade matcha from Kyoto.", "Beverages", "Tea Culture JP", "Japan", 45.00m, "MYR", 40, new("Unverified", null, null, null), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Mixed Nut Trail Pack", "mixed-nut-trail-pack", "Roasted almonds, cashews, walnuts, pecans.", "Snacks", "NutriBite", "Australia", 22.00m, "MYR", 250, new("Verified", "HC-2024-009", "IFANCA", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Frozen Fish Fillets", "frozen-fish-fillets", "Deep-sea caught fish, flash-frozen.", "Seafood", "OceanFresh MY", "Malaysia", 28.50m, "MYR", 180, new("Verified", "HC-2024-010", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
        new(Guid.NewGuid(), "Cheese Prata Frozen", "cheese-prata-frozen", "Flaky paratha stuffed with mozzarella.", "Frozen Foods", "Mamak Kitchen", "Malaysia", 14.50m, "MYR", 320, new("Verified", "HC-2024-011", "JAKIM", DateTimeOffset.UtcNow.AddYears(1)), DateTimeOffset.UtcNow),
    ];

    private static CategoryDto[] GetSeedCategories() =>
    [
        new(Guid.NewGuid(), "Beverages", "beverages", null, 2),
        new(Guid.NewGuid(), "Meat & Poultry", "meat-poultry", null, 2),
        new(Guid.NewGuid(), "Snacks", "snacks", null, 2),
        new(Guid.NewGuid(), "Grains & Staples", "grains-staples", null, 1),
        new(Guid.NewGuid(), "Condiments", "condiments", null, 1),
        new(Guid.NewGuid(), "Sweets", "sweets", null, 1),
        new(Guid.NewGuid(), "Seafood", "seafood", null, 1),
        new(Guid.NewGuid(), "Frozen Foods", "frozen-foods", null, 1),
    ];

    private static VendorDto[] GetSeedVendors() =>
    [
        MakeVendor("Pacific Harvest Co.", "pacific-harvest-co", "Active", "Malaysia"),
        MakeVendor("Al-Falah Foods", "al-falah-foods", "Active", "Malaysia"),
        MakeVendor("Heritage Sweets", "heritage-sweets", "Active", "Indonesia"),
        MakeVendor("Tropical Press", "tropical-press", "Active", "Thailand"),
        MakeVendor("Spice Garden MY", "spice-garden-my", "Active", "Malaysia"),
        MakeVendor("Golden Harvest", "golden-harvest", "Active", "Pakistan"),
        MakeVendor("Istanbul Sweets Co.", "istanbul-sweets-co", "Active", "Turkey"),
        MakeVendor("NutriBite", "nutribite", "Active", "Australia"),
        MakeVendor("OceanFresh MY", "oceanfresh-my", "Active", "Malaysia"),
        MakeVendor("Mamak Kitchen", "mamak-kitchen", "Active", "Malaysia"),
    ];

    private static VendorDto MakeVendor(string name, string slug, string status, string? country) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Status = status,
        Country = country,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static OrderDto[] GetSeedOrders()
    {
        var products = GetSeedProducts();
        return [
            new(Guid.NewGuid(), "Delivered", [
                new(Guid.NewGuid(), Guid.NewGuid(), "Al-Falah Foods", "Delivered", [
                    new(products[1].Id, products[1].Title, 2, products[1].Price, "MYR"),
                    new(products[7].Id, products[7].Title, 1, products[7].Price, "MYR")
                ], 69.70m, "MYR", "TRK-2026-001")
            ], 69.70m, "MYR", DateTimeOffset.UtcNow.AddDays(-5)),
            new(Guid.NewGuid(), "Processing", [
                new(Guid.NewGuid(), Guid.NewGuid(), "Pacific Harvest Co.", "Processing", [
                    new(products[0].Id, products[0].Title, 3, products[0].Price, "MYR")
                ], 26.70m, "MYR", null)
            ], 26.70m, "MYR", DateTimeOffset.UtcNow.AddDays(-1)),
            new(Guid.NewGuid(), "Pending", [
                new(Guid.NewGuid(), Guid.NewGuid(), "Heritage Sweets", "Pending", [
                    new(products[2].Id, products[2].Title, 1, products[2].Price, "MYR"),
                    new(products[5].Id, products[5].Title, 1, products[5].Price, "MYR")
                ], 47.50m, "MYR", null)
            ], 47.50m, "MYR", DateTimeOffset.UtcNow.AddHours(-3)),
        ];
    }

    private static CustomerDto[] GetSeedCustomers() =>
    [
        new(Guid.NewGuid(), "Ahmad Faisal", "ahmad@example.com", 5, 245.50m, "MYR", DateTimeOffset.UtcNow.AddDays(-60)),
        new(Guid.NewGuid(), "Siti Nurhaliza", "siti@example.com", 12, 892.00m, "MYR", DateTimeOffset.UtcNow.AddDays(-120)),
        new(Guid.NewGuid(), "Muhammad Ali", "ali@example.com", 3, 156.80m, "MYR", DateTimeOffset.UtcNow.AddDays(-30)),
        new(Guid.NewGuid(), "Fatimah Zahra", "fatimah@example.com", 8, 534.20m, "MYR", DateTimeOffset.UtcNow.AddDays(-90)),
        new(Guid.NewGuid(), "Omar Hassan", "omar@example.com", 1, 32.00m, "MYR", DateTimeOffset.UtcNow.AddDays(-7)),
        new(Guid.NewGuid(), "Aisha Rahman", "aisha@example.com", 15, 1205.00m, "MYR", DateTimeOffset.UtcNow.AddDays(-180)),
        new(Guid.NewGuid(), "Yusuf Ibrahim", "yusuf@example.com", 7, 478.90m, "MYR", DateTimeOffset.UtcNow.AddDays(-45)),
        new(Guid.NewGuid(), "Maryam Khan", "maryam@example.com", 2, 89.00m, "MYR", DateTimeOffset.UtcNow.AddDays(-14)),
    ];
}

