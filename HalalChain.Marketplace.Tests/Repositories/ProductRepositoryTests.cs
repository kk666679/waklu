using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HalalChain.Marketplace.Tests.Repositories;

/// <summary>Unit tests for ProductRepository using in-memory database.</summary>
public class ProductRepositoryTests
{
    private readonly PlatformDbContext _context;
    private readonly ProductRepository _repository;
    private readonly Mock<ILogger<ProductRepository>> _mockLogger;
    private readonly Vendor _vendor;

    public ProductRepositoryTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new PlatformDbContext(options);
        _mockLogger = new Mock<ILogger<ProductRepository>>();
        _repository = new ProductRepository(_context, _mockLogger.Object);

        // Product.Vendor is a required relationship, so every product under
        // test needs a persisted principal.
        _vendor = CreateVendor("test-vendor");
        _context.Vendors.Add(_vendor);
    }

    private Vendor CreateVendor(string slug)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = $"Vendor {slug}",
            Slug = slug,
            Status = "Active",
            Country = "MY"
        };
        _context.Vendors.Add(vendor);
        return vendor;
    }

    private Product CreateProduct(string title, string slug, decimal price = 99.99m, Vendor? vendor = null)
    {
        vendor ??= _vendor;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Origin = "Malaysia",
            VendorId = vendor.Id,
            Vendor = vendor,
            Price = price,
            Currency = "MYR",
            Status = ProductStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        vendor.Products.Add(product);
        return product;
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var product = CreateProduct("Test Product", "test-product");
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Test Product", result.Title);
        Assert.Equal("test-product", result.Slug);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ReturnsProduct()
    {
        // Arrange
        var product = CreateProduct("Product", "unique-slug", 50m);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySlugAsync("unique-slug");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("unique-slug", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_WithUnknownSlug_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("does-not-exist");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByVendorAsync_WithValidVendorId_ReturnsProducts()
    {
        // Arrange
        var vendor = CreateVendor("vendor-two");
        var products = new[]
        {
            CreateProduct("Product 1", "product-1", 10m, vendor),
            CreateProduct("Product 2", "product-2", 20m, vendor)
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByVendorAsync(vendor.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(vendor.Id, p.VendorId));
    }

    [Fact]
    public async Task GetByVendorAsync_WithInvalidVendorId_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByVendorAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeaturedAsync_OnlyReturnsIndexedProductsWithRecommendationReason()
    {
        // Arrange
        var featured = CreateProduct("Featured", "featured", 100m);
        featured.IsSearchIndexed = true;
        featured.RecommendationReason = "Trending";

        var unindexed = CreateProduct("Unindexed", "unindexed", 100m);
        unindexed.IsSearchIndexed = false;
        unindexed.RecommendationReason = "Trending";

        var noReason = CreateProduct("No Reason", "no-reason", 100m);
        noReason.IsSearchIndexed = true;
        noReason.RecommendationReason = null;

        _context.Products.AddRange(featured, unindexed, noReason);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetFeaturedAsync(take: 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("featured", result.First().Slug);
    }

    [Fact]
    public async Task SearchAsync_MatchesTitleAndReturnsResults()
    {
        // Arrange
        var match = CreateProduct("Halal Chicken Breast", "halal-chicken");
        var other = CreateProduct("Beef Rendang", "beef-rendang");
        _context.Products.AddRange(match, other);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("halal");

        // Assert
        Assert.Single(result);
        Assert.Equal("halal-chicken", result.First().Slug);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var products = Enumerable.Range(1, 5)
            .Select(i => CreateProduct($"Product {i}", $"product-{i}", 10m * i))
            .ToList();
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task AddAsync_SavesProductToDatabase()
    {
        // Arrange
        var product = CreateProduct("New Product", "new-product", 75m);

        // Act
        await _repository.AddAsync(product);

        // Assert
        var savedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("New Product", savedProduct.Title);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductInDatabase()
    {
        // Arrange
        var product = CreateProduct("Original Name", "update-product", 50m);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        product.Title = "Updated Name";
        product.Price = 60m;
        await _repository.UpdateAsync(product);

        // Assert
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Name", updatedProduct.Title);
        Assert.Equal(60m, updatedProduct.Price);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        // Arrange
        var product = CreateProduct("Delete Me", "delete-me", 25m);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(product.Id);

        // Assert
        var deletedProduct = await _context.Products.FindAsync(product.Id);
        Assert.Null(deletedProduct);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProduct_ReturnsTrue()
    {
        // Arrange
        var product = CreateProduct("Exists Test", "exists-test", 30m);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(product.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentProduct_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var products = Enumerable.Range(1, 25)
            .Select(i => CreateProduct($"Product {i}", $"product-{i}", 10m * i))
            .ToList();
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _repository.GetAllAsync(skip: 0, take: 10);
        var page2 = await _repository.GetAllAsync(skip: 10, take: 10);

        // Assert
        Assert.Equal(10, page1.Count());
        Assert.Equal(10, page2.Count());
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMatchingProducts()
    {
        // Arrange
        var wanted = CreateProduct("Wanted", "wanted");
        var notWanted = CreateProduct("Not Wanted", "not-wanted");
        _context.Products.AddRange(wanted, notWanted);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdsAsync([wanted.Id]);

        // Assert
        Assert.Single(result);
        Assert.Equal(wanted.Id, result.First().Id);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}