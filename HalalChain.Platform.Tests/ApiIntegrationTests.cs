using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Halal.Dto;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;

namespace HalalChain.Platform.Tests;

public sealed class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Catalog ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_ReturnsSeededProducts()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Length.Should().BeGreaterThanOrEqualTo(10);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(10);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetProduct_ById_ReturnsProduct()
    {
        var all = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var first = all!.Items.First();

        var product = await _client.GetFromJsonAsync<ProductDto>($"api/v1/catalog/products/{first.Id}");
        product!.Id.Should().Be(first.Id);
        product.Title.Should().Be(first.Title);
    }

    [Fact]
    public async Task GetProduct_InvalidId_Returns404()
    {
        var resp = await _client.GetAsync($"api/v1/catalog/products/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        var cats = await _client.GetFromJsonAsync<CategoryDto[]>("api/v1/catalog/categories");
        cats.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ListProducts_FilterByCategory_ReturnsFilteredResults()
    {
        var all = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var categorySlug = all!.Items.First().CategoryName.ToLowerInvariant().Replace(" ", "-");

        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>($"api/v1/catalog/products?category={categorySlug}");
        result!.Items.Should().OnlyContain(p => p.CategoryName.ToLowerInvariant().Replace(" ", "-") == categorySlug);
    }

    [Fact]
    public async Task ListProducts_FilterByMaxPrice_ReturnsCheaperProducts()
    {
        var all = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var maxPrice = all!.Items.Min(p => p.Price) + 1;

        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>($"api/v1/catalog/products?maxPrice={maxPrice}");
        result!.Items.Should().OnlyContain(p => p.Price <= maxPrice);
    }

    [Fact]
    public async Task ListProducts_FilterByHalalStatus_ReturnsVerifiedOnly()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products?halalStatus=verified");
        result!.Items.Should().OnlyContain(p => p.HalalStatus.Status == "Verified");
    }

    [Fact]
    public async Task ListProducts_SearchFilter_ReturnsMatchingProducts()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products?search=keropok");
        result!.Items.Should().OnlyContain(p => p.Title.ToLower().Contains("keropok"));
    }

    // ── Enriched Product ──────────────────────────────────────────────────

    [Fact]
    public async Task GetEnrichedProduct_ById_ReturnsEnrichedDto()
    {
        var all = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var first = all!.Items.First();

        var enriched = await _client.GetFromJsonAsync<EnrichedProductDto>($"api/v1/catalog/products/{first.Id}/enriched");
        enriched.Should().NotBeNull();
        enriched!.Id.Should().Be(first.Id);
        enriched.Title.Should().Be(first.Title);
        enriched.Profile.Should().NotBeNull();
        enriched.Profile!.Status.Should().BeGreaterThan(0);
        if (!string.IsNullOrEmpty(enriched.Profile.CertificationBodyName))
            enriched.Profile.CertificateNumber.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEnrichedProduct_InvalidId_Returns404()
    {
        var resp = await _client.GetAsync($"api/v1/catalog/products/{Guid.NewGuid()}/enriched");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Search Suggestions ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchSuggestions_MatchingQuery_ReturnsResults()
    {
        var suggestions = await _client.GetFromJsonAsync<SearchSuggestionDto[]>("api/v1/search/suggestions?q=coconut");
        suggestions.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestions_TooShort_ReturnsEmpty()
    {
        var suggestions = await _client.GetFromJsonAsync<SearchSuggestionDto[]>("api/v1/search/suggestions?q=a");
        suggestions.Should().NotBeNull();
        suggestions!.Length.Should().Be(0);
    }

    [Fact]
    public async Task ListProducts_Pagination_ReturnsCorrectMetadata()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products?page=1&pageSize=5");
        result!.Items.Length.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalPages.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── Vendors ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListVendors_ReturnsSeededVendors()
    {
        var vendors = await _client.GetFromJsonAsync<VendorDto[]>("api/v1/vendors");
        vendors.Should().NotBeNullOrEmpty();
        vendors!.Length.Should().BeGreaterThanOrEqualTo(3);
    }

    // ── Commerce ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_EmptyCart_ReturnsEmptyItems()
    {
        _client.SetTestRoles(AuthConstants.RoleMarketplaceUser);
        var cart = await _client.GetFromJsonAsync<CartDto>("api/v1/commerce/cart");
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToCart_ValidProduct_AddsToCart()
    {
        _client.SetTestRoles(AuthConstants.RoleMarketplaceUser);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        var resp = await _client.PostAsJsonAsync("api/v1/commerce/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 2 });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await _client.GetFromJsonAsync<CartDto>("api/v1/commerce/cart");
        cart!.Items.Should().Contain(i => i.ProductId == productId);
        cart.Items.First(i => i.ProductId == productId).Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddToCart_TwiceSameProduct_IncrementsQuantity()
    {
        _client.SetTestRoles(AuthConstants.RoleMarketplaceUser);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        await _client.PostAsJsonAsync("api/v1/commerce/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 1 });
        await _client.PostAsJsonAsync("api/v1/commerce/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 3 });

        var cart = await _client.GetFromJsonAsync<CartDto>("api/v1/commerce/cart");
        cart!.Items.First(i => i.ProductId == productId).Quantity.Should().Be(4);
    }

    [Fact]
    public async Task UpdateCartItem_Quantity0_RemovesItem()
    {
        _client.SetTestRoles(AuthConstants.RoleMarketplaceUser);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        await _client.PostAsJsonAsync("api/v1/commerce/cart/items", new AddToCartRequest { ProductId = productId, Quantity = 1 });
        await _client.PatchAsJsonAsync("api/v1/commerce/cart/items", new UpdateCartItemRequest { ProductId = productId, Quantity = 0 });

        var cart = await _client.GetFromJsonAsync<CartDto>("api/v1/commerce/cart");
        cart!.Items.Should().NotContain(i => i.ProductId == productId);
    }

    [Fact]
    public async Task GetCart_WithoutRole_Returns403()
    {
        _client.SetTestRoles();
        var resp = await _client.GetAsync("api/v1/commerce/cart");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Halal ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCertificate_ValidProduct_PersistsCertificate()
    {
        // Admin role bypasses vendor-ownership check so the test doesn't need
        // to know which seeded vendor owns the first product.
        _client.SetTestRoles(AuthConstants.RoleAdmin);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{productId}/certificates", new
        {
            CertificateNumber = "TEST-CERT-001",
            CertificationBody = "JAKIM",
            Jurisdiction = "MY",
            IssueDate = DateTimeOffset.UtcNow.AddDays(-30),
            ExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            Scope = "Test scope"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cert = await resp.Content.ReadFromJsonAsync<HalalCertificateDto>();
        cert!.CertificateNumber.Should().Be("TEST-CERT-001");
        cert.CertificationBody.Should().Be("JAKIM");
    }

    [Fact]
    public async Task SubmitCertificate_WithoutVendorRole_Returns403()
    {
        _client.SetTestRoles();
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{productId}/certificates", new
        {
            CertificateNumber = "TEST-CERT-002",
            CertificationBody = "JAKIM",
            Jurisdiction = "MY",
            IssueDate = DateTimeOffset.UtcNow.AddDays(-30),
            ExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            Scope = "Test scope"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TriggerVerification_ValidProduct_CreatesVerification()
    {
        _client.SetTestRoles(AuthConstants.RoleVerificationOfficer);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{productId}/verify", new
        {
            Jurisdiction = "MY",
            PolicyVersion = "MY-v3"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var ver = await resp.Content.ReadFromJsonAsync<VerificationDto>();
        ver.Should().NotBeNull();
        ver!.Status.Should().Be(ComplianceStatus.Unverified);
    }

    [Fact]
    public async Task TriggerVerification_WithoutVerificationOfficerRole_Returns403()
    {
        _client.SetTestRoles(AuthConstants.RoleVendor);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("api/v1/catalog/products");
        var productId = products!.Items.First().Id;

        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{productId}/verify", new
        {
            Jurisdiction = "MY",
            PolicyVersion = "MY-v3"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_WithoutVendorRole_Returns403()
    {
        _client.SetTestRoles();
        var resp = await _client.PostAsJsonAsync("api/v1/catalog/products", new
        {
            Title = "Unauthorized Product",
            CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Price = 10m,
            Currency = "MYR",
            Inventory = 100,
            Origin = "Malaysia"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
