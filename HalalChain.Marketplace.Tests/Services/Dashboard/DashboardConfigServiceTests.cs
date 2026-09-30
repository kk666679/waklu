using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HalalChain.Marketplace.Services.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Common.Abstractions;
using System.Text.Json;

namespace HalalChain.Marketplace.Tests.Services.Dashboard;

public class DashboardConfigServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IDashboardConfigRepository> _mockRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly ILogger<DashboardConfigService> _logger;
    private readonly DashboardConfigService _service;

    public DashboardConfigServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockRepository = new Mock<IDashboardConfigRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _logger = new Mock<ILogger<DashboardConfigService>>().Object;

        var options = Options.Create(new DashboardConfigOptions
        {
            DefaultCacheDurationSeconds = 300,
            EnableDistributedCache = true,
            EnableRuntimeCache = true
        });

        _mockConfiguration
            .Setup(c => c.GetSection("Dashboard"))
            .Returns(new Mock<IConfigurationSection>().Object);

        _service = new DashboardConfigService(
            _mockCache.Object,
            _mockConfiguration.Object,
            options,
            _logger,
            _mockRepository.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task GetConfigAsync_WithValidTenantId_ReturnsConfig()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        var expectedConfig = new TenantDashboardConfig { TenantId = tenantId, OrganizationName = "Test Org" };
        var configJson = JsonSerializer.Serialize(expectedConfig);
        var entity = new DashboardConfigEntity
        {
            Id = 1,
            TenantId = tenantId,
            ConfigurationJson = configJson,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetActiveConfigAsync(tenantId))
            .ReturnsAsync(entity);

        _mockCache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetConfigAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        _mockRepository.Verify(r => r.GetActiveConfigAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_WithValidConfig_UpdatesAndInvalidatesCache()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        var config = new TenantDashboardConfig { TenantId = tenantId, OrganizationName = "Updated Org" };
        var configJson = JsonSerializer.Serialize(config);
        var entity = new DashboardConfigEntity
        {
            Id = 1,
            TenantId = tenantId,
            ConfigurationJson = configJson,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetAllVersionsAsync(tenantId))
            .ReturnsAsync(new List<DashboardConfigEntity> { entity });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<DashboardConfigEntity>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(entity);

        _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        // Act
        await _service.UpdateConfigAsync(tenantId, config);

        // Assert
        _mockRepository.Verify(r => r.GetAllVersionsAsync(tenantId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DashboardConfigEntity>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetConfigurationVersionsAsync_ReturnsAllVersions()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        var versions = new List<DashboardConfigEntity>
        {
            new DashboardConfigEntity { Id = 1, TenantId = tenantId, Version = 1, IsActive = false },
            new DashboardConfigEntity { Id = 2, TenantId = tenantId, Version = 2, IsActive = true }
        };

        _mockRepository
            .Setup(r => r.GetAllVersionsAsync(tenantId))
            .ReturnsAsync(versions);

        // Act
        var result = await _service.GetConfigurationVersionsAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal(tenantId, v.TenantId));
    }

    [Fact]
    public async Task ActivateConfigurationVersionAsync_WithValidConfigId_ActivatesVersion()
    {
        // Arrange
        const int configId = 1;
        const string tenantId = "test-tenant-1";
        var config = new DashboardConfigEntity
        {
            Id = configId,
            TenantId = tenantId,
            IsActive = false,
            ConfigurationJson = "{}"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync(config);

        _mockRepository
            .Setup(r => r.ActivateAsync(configId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(config);

        _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        // Act
        await _service.ActivateConfigurationVersionAsync(configId, "Test activation");

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(configId), Times.Once);
        _mockRepository.Verify(r => r.ActivateAsync(configId, It.IsAny<string>(), "Test activation"), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogAsync_ReturnsCacheStatistics()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        var auditEntries = new List<DashboardConfigAuditEntity>
        {
            new DashboardConfigAuditEntity
            {
                Id = 1,
                TenantId = tenantId,
                ConfigurationId = 1,
                Operation = "Create",
                ChangedBy = "admin",
                ChangedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetAuditLogAsync(tenantId, null, It.IsAny<DateTime?>(), 100))
            .ReturnsAsync(auditEntries);

        // Act
        var result = await _service.GetAuditLogAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Create", result[0].Operation);
    }

    [Fact]
    public async Task GetCacheStatsAsync_ReturnsCacheStatistics()
    {
        // Act
        var stats = await _service.GetCacheStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalEntries);
        Assert.Equal(0, stats.HitCount);
        Assert.Equal(0, stats.MissCount);
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesCacheEntry()
    {
        // Arrange
        const string tenantId = "test-tenant-1";

        // Act
        await _service.InvalidateCacheAsync(tenantId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Vendor")]
    public async Task GetRolePermissionsAsync_WithValidRole_ReturnsPermissions(string role)
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        var config = new TenantDashboardConfig { TenantId = tenantId };
        config.RolePermissions[role] = new DashboardRolePermissions
        {
            Role = role,
            AccessiblePages = new List<string> { "dashboard" },
            CanPersonalize = true
        };

        var configJson = JsonSerializer.Serialize(config);
        var entity = new DashboardConfigEntity
        {
            Id = 1,
            TenantId = tenantId,
            ConfigurationJson = configJson,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetActiveConfigAsync(tenantId))
            .ReturnsAsync(entity);

        _mockCache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetRolePermissionsAsync(tenantId, role);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(role, result.Role);
    }
}
