using HalalChain.Marketplace.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace HalalChain.Marketplace.Tests.Services;

/// <summary>Unit tests for DistributedCachingService.</summary>
public class CachingServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DistributedCachingService>> _mockLogger;
    private readonly DistributedCachingService _cachingService;

    public CachingServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DistributedCachingService>>();
        _cachingService = new DistributedCachingService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCachedData_ReturnsCachedValue()
    {
        // Arrange
        var key = "test-key";
        var testValue = "cached-value";
        var json = JsonSerializer.Serialize(testValue);
        var data = System.Text.Encoding.UTF8.GetBytes(json);

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var factory = new Mock<Func<Task<string>>>();

        // Act
        var result = await _cachingService.GetOrSetAsync(key, factory.Object);

        // Assert
        Assert.Equal(testValue, result);
        factory.Verify(f => f(), Times.Never); // Factory should not be called
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheMiss_CallsFactoryAndCaches()
    {
        // Arrange
        var key = "test-key";
        var testValue = "fresh-value";

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var factory = new Mock<Func<Task<string>>>();
        factory.Setup(f => f()).ReturnsAsync(testValue);

        // Act
        var result = await _cachingService.GetOrSetAsync(key, factory.Object);

        // Assert
        Assert.Equal(testValue, result);
        factory.Verify(f => f(), Times.Once); // Factory should be called once
        _mockCache.Verify(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithValidKey_ReturnsDeserializedValue()
    {
        // Arrange
        var key = "test-key";
        var testValue = new { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(testValue);
        var data = System.Text.Encoding.UTF8.GetBytes(json);

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cachingService.GetAsync<dynamic>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent";
        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cachingService.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_StoresSerializedData()
    {
        // Arrange
        var key = "test-key";
        var testValue = "test-value";

        // Act
        await _cachingService.SetAsync(key, testValue);

        // Assert
        _mockCache.Verify(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_SetsCorrectTTL()
    {
        // Arrange
        var key = "test-key";
        var testValue = "test-value";
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        await _cachingService.SetAsync(key, testValue, expiration);

        // Assert
        _mockCache.Verify(c => c.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(opts => 
                opts.AbsoluteExpirationRelativeToNow == expiration),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_DeletesKey()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cachingService.RemoveAsync(key);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WithFactoryException_ReturnsFactoryResult()
    {
        // Arrange
        var key = "test-key";
        var testValue = "fallback-value";

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        var factory = new Mock<Func<Task<string>>>();
        factory.Setup(f => f()).ReturnsAsync(testValue);

        // Act
        var result = await _cachingService.GetOrSetAsync(key, factory.Object);

        // Assert
        Assert.Equal(testValue, result); // Should return factory result despite cache error
        factory.Verify(f => f(), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullValue_DoesNotCache()
    {
        // Arrange
        var key = "test-key";
        string? nullValue = null;

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var factory = new Mock<Func<Task<string?>>>();
        factory.Setup(f => f()).ReturnsAsync(nullValue);

        // Act
        var result = await _cachingService.GetOrSetAsync(key, factory.Object);

        // Assert
        Assert.Null(result);
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never); // Should not cache null values
    }

    [Fact]
    public void CacheKeys_ProductById_GeneratesCorrectKey()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var key = CacheKeys.ProductById(productId);

        // Assert
        Assert.Equal($"product:{productId}", key);
    }

    [Fact]
    public void CacheKeys_ProductsByVendor_GeneratesCorrectKey()
    {
        // Arrange
        var vendorId = Guid.NewGuid();

        // Act
        var key = CacheKeys.ProductsByVendor(vendorId);

        // Assert
        Assert.Equal($"products:vendor:{vendorId}", key);
    }

    [Fact]
    public void CacheKeys_MultipleCallsSameId_GenerateSameKey()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var key1 = CacheKeys.ProductById(productId);
        var key2 = CacheKeys.ProductById(productId);

        // Assert
        Assert.Equal(key1, key2);
    }
}
