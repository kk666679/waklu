using System.Net;
using System.Net.Http.Json;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Catalog.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using HalalChain.Platform.Contracts.Vendors.Dto;
using FluentAssertions;
using Xunit;

namespace HalalChain.Platform.Tests;

/// <summary>Critical authorization tests: a vendor must never be able to read or
/// mutate another vendor's private marketplace data. Vendor identity is derived
/// from the JWT Name claim (a stable Guid), so we simulate vendor A and vendor B
/// with distinct subjects.</summary>
public sealed class VendorIsolationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid VendorA = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid VendorB = Guid.Parse("A0000000-0000-0000-0000-000000000002");
    private const string CategoryId = "10000000-0000-0000-0000-000000000001";

    public VendorIsolationTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private async Task<Guid> CreateProductAsVendorAsync(Guid vendorId)
    {
        _client.SetTestUser(vendorId, AuthConstants.RoleVendor);
        var resp = await _client.PostAsJsonAsync("api/v1/catalog/products", new CreateProductRequest
        {
            Title = $"Isolation product {Guid.NewGuid():N}",
            CategoryId = Guid.Parse(CategoryId),
            Price = 10m,
            Currency = "MYR",
            Inventory = 50,
            Origin = "Malaysia"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var p = await resp.Content.ReadFromJsonAsync<ProductDto>();
        return p!.Id;
    }

    [Fact]
    public async Task Vendor_CanCreateAndUpdateOwnProduct()
    {
        var id = await CreateProductAsVendorAsync(VendorA);

        _client.SetTestUser(VendorA, AuthConstants.RoleVendor);
        var resp = await _client.PatchAsJsonAsync($"api/v1/catalog/products/{id}", new UpdateProductRequest { Price = 12m, Inventory = 40 });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VendorB_CannotUpdateVendorA_Product_Returns403()
    {
        var id = await CreateProductAsVendorAsync(VendorA);

        _client.SetTestUser(VendorB, AuthConstants.RoleVendor);
        var resp = await _client.PatchAsJsonAsync($"api/v1/catalog/products/{id}", new UpdateProductRequest { Price = 1m });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VendorB_CannotSubmitCertificate_ForVendorA_Product_Returns403()
    {
        var id = await CreateProductAsVendorAsync(VendorA);

        _client.SetTestUser(VendorB, AuthConstants.RoleVendor);
        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{id}/certificates", new
        {
            CertificateNumber = "X",
            CertificationBody = "JAKIM",
            Jurisdiction = "MY",
            IssueDate = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            Scope = "x"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanUpdateAnyVendorProduct()
    {
        var id = await CreateProductAsVendorAsync(VendorA);

        _client.SetTestUser(Guid.NewGuid(), AuthConstants.RoleAdmin);
        var resp = await _client.PatchAsJsonAsync($"api/v1/catalog/products/{id}", new UpdateProductRequest { Price = 99m });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Customer_CannotCreateProduct_Returns403()
    {
        _client.SetTestUser(Guid.NewGuid(), AuthConstants.RoleMarketplaceUser);
        var resp = await _client.PostAsJsonAsync("api/v1/catalog/products", new CreateProductRequest
        {
            Title = "Unauthorized",
            CategoryId = Guid.Parse(CategoryId),
            Price = 10m,
            Currency = "MYR",
            Inventory = 1,
            Origin = "MY"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VendorA_CannotVerify_VendorB_Product_Returns403()
    {
        var id = await CreateProductAsVendorAsync(VendorA);

        _client.SetTestUser(VendorB, AuthConstants.RoleVendor);
        var resp = await _client.PostAsJsonAsync($"api/v1/halal/products/{id}/verify", new { Jurisdiction = "MY", PolicyVersion = "MY-v3" });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
