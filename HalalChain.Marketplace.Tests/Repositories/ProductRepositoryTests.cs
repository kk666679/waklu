using HalalChain.Domain.Catalog;
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

    public ProductRepositoryTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new PlatformDbContext(options);
        _mockLogger = new Mock<ILogger<ProductRepository>>();
        _repository = new ProductRepository(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Sku = "TEST-001",
            VendorId = vendorId,
            Price = 99.99m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal("TEST-001", result.Sku);
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
    public async Task GetByVendorAsync_WithValidVendorId_ReturnsProducts()
    {
        // Arrange
        var vendorId = Guid.NewGuid();
        var products = new[]
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 1",
                Sku = "SKU-001",
                VendorId = vendorId,
                Price = 10m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product 2",
                Sku = "SKU-002",
                VendorId = vendorId,
                Price = 20m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByVendorAsync(vendorId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(vendorId, p.VendorId));
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
    public async Task GetBySkuAsync_WithValidSku_ReturnsProduct()
    {
        // Arrange
        var sku = "UNIQUE-SKU";
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product",
            Sku = sku,
            VendorId = Guid.NewGuid(),
            Price = 50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySkuAsync(sku);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sku, result.Sku);
    }

    [Fact]
    public async Task GetActiveAsync_OnlyReturnsActiveProducts()
    {
        // Arrange
        var activeProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Active",
            Sku = "ACTIVE-001",
            VendorId = Guid.NewGuid(),
            Price = 100m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inactiveProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            Sku = "INACTIVE-001",
            VendorId = Guid.NewGuid(),
            Price = 100m,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.AddRange(activeProduct, inactiveProduct);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync(take: 10);

        // Assert
        Assert.Single(result);
        Assert.All(result, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var vendorId = Guid.NewGuid();
        var products = Enumerable.Range(1, 5)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Sku = $"SKU-{i:00}",
                VendorId = vendorId,
                Price = 10m * i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();
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
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "New Product",
            Sku = "NEW-001",
            VendorId = Guid.NewGuid(),
            Price = 75m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(product);

        // Assert
        var savedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("New Product", savedProduct.Name);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductInDatabase()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Sku = "UPDATE-001",
            VendorId = Guid.NewGuid(),
            Price = 50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        product.Name = "Updated Name";
        product.Price = 60m;
        await _repository.UpdateAsync(product);

        // Assert
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal("Updated Name", updatedProduct.Name);
        Assert.Equal(60m, updatedProduct.Price);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductFromDatabase()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Delete Me",
            Sku = "DELETE-001",
            VendorId = Guid.NewGuid(),
            Price = 25m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(productId);

        // Assert
        var deletedProduct = await _context.Products.FindAsync(productId);
        Assert.Null(deletedProduct);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProduct_ReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Exists Test",
            Sku = "EXISTS-001",
            VendorId = Guid.NewGuid(),
            Price = 30m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(productId);

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
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Sku = $"SKU-{i:00}",
                VendorId = Guid.NewGuid(),
                Price = 10m * i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();
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

    public void Dispose()
    {
        _context?.Dispose();
    }
}
