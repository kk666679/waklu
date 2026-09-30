using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using System.Text.Json;

namespace HalalChain.Marketplace.Tests.Repositories.Dashboard;

public class DashboardConfigRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly IDashboardConfigRepository _repository;
    private readonly ILogger<DashboardConfigRepository> _logger;

    public DashboardConfigRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = new Mock<ILogger<DashboardConfigRepository>>().Object;
        _repository = new DashboardConfigRepository(_context, _logger);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetActiveConfigAsync_WithValidTenantId_ReturnsActiveConfig()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        const string configJson = "{\"TenantId\":\"test-tenant-1\"}";
        
        var entity = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = configJson,
            IsActive = true,
            CreatedBy = "test",
            UpdatedBy = "test"
        };

        _context.DashboardConfigs.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveConfigAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetActiveConfigAsync_WithInactiveTenant_ReturnsNull()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        const string configJson = "{\"TenantId\":\"test-tenant-1\"}";
        
        var entity = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = configJson,
            IsActive = false,
            CreatedBy = "test",
            UpdatedBy = "test"
        };

        _context.DashboardConfigs.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveConfigAsync(tenantId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithNewConfig_CreatesAndReturnsEntity()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        const string configJson = "{\"TenantId\":\"test-tenant-1\"}";

        // Act
        var result = await _repository.CreateAsync(tenantId, configJson, "admin", replaceActive: true, "Initial config");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.Version);

        // Verify it was persisted
        var persisted = await _repository.GetActiveConfigAsync(tenantId);
        Assert.NotNull(persisted);
        Assert.Equal(tenantId, persisted.TenantId);
    }

    [Fact]
    public async Task CreateAsync_WithReplaceActiveTrue_DeactivatesPreviousVersion()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var firstConfig = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.Add(firstConfig);
        await _context.SaveChangesAsync();

        // Act
        var secondConfig = await _repository.CreateAsync(tenantId, "{}", "admin", replaceActive: true);

        // Assert
        var deactivated = await _context.DashboardConfigs.FirstAsync(c => c.Id == firstConfig.Id);
        Assert.False(deactivated.IsActive);
        Assert.True(secondConfig.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEntity_UpdatesAndCreatesAuditEntry()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        const string originalJson = "{\"name\":\"original\"}";
        const string updatedJson = "{\"name\":\"updated\"}";

        var entity = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = originalJson,
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin",
            Version = 1
        };

        _context.DashboardConfigs.Add(entity);
        await _context.SaveChangesAsync();

        entity.ConfigurationJson = updatedJson;

        // Act
        var result = await _repository.UpdateAsync(entity, "admin", "Updated config");

        // Assert
        Assert.Equal(2, result.Version);
        
        var audit = await _context.DashboardConfigAudits.FirstAsync();
        Assert.Equal("Update", audit.Operation);
        Assert.Equal(originalJson, audit.PreviousConfigurationJson);
        Assert.Equal(updatedJson, audit.NewConfigurationJson);
    }

    [Fact]
    public async Task ActivateAsync_WithValidConfigId_ActivatesAndDeactivatesOthers()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var config1 = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        var config2 = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = false,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ActivateAsync(config2.Id, "admin", "Activate version 2");

        // Assert
        Assert.True(result.IsActive);
        
        var reloadedConfig1 = await _context.DashboardConfigs.FirstAsync(c => c.Id == config1.Id);
        Assert.False(reloadedConfig1.IsActive);
    }

    [Fact]
    public async Task GetAllVersionsAsync_ReturnsSortedVersions()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var configs = Enumerable.Range(1, 3)
            .Select(i => new DashboardConfigEntity
            {
                TenantId = tenantId,
                ConfigurationJson = "{}",
                IsActive = i == 3,
                CreatedBy = "admin",
                UpdatedBy = "admin",
                Version = i,
                UpdatedAt = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();

        _context.DashboardConfigs.AddRange(configs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllVersionsAsync(tenantId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0].Version); // Most recent first
        Assert.Equal(1, result[2].Version);
    }

    [Fact]
    public async Task GetAuditLogAsync_ReturnsSortedAuditEntries()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var config = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.Add(config);
        await _context.SaveChangesAsync();

        var audits = Enumerable.Range(1, 3)
            .Select(i => new DashboardConfigAuditEntity
            {
                TenantId = tenantId,
                ConfigurationId = config.Id,
                Operation = i == 1 ? "Create" : "Update",
                ChangedBy = "admin",
                ChangedAt = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();

        _context.DashboardConfigAudits.AddRange(audits);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAuditLogAsync(tenantId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Update", result[0].Operation); // Most recent first
        Assert.Equal("Create", result[2].Operation);
    }

    [Fact]
    public async Task HasBeenModifiedSinceAsync_WithRecentModification_ReturnsTrue()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var config = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin",
            UpdatedAt = DateTime.UtcNow
        };

        _context.DashboardConfigs.Add(config);
        await _context.SaveChangesAsync();

        var since = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = await _repository.HasBeenModifiedSinceAsync(tenantId, since);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasBeenModifiedSinceAsync_WithOldModification_ReturnsFalse()
    {
        // Arrange
        const string tenantId = "test-tenant-1";
        
        var config = new DashboardConfigEntity
        {
            TenantId = tenantId,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin",
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        _context.DashboardConfigs.Add(config);
        await _context.SaveChangesAsync();

        var since = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = await _repository.HasBeenModifiedSinceAsync(tenantId, since);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetBatchAsync_ReturnsDictionaryOfConfigs()
    {
        // Arrange
        var tenantIds = new[] { "tenant-1", "tenant-2", "tenant-3" };
        
        foreach (var tenantId in tenantIds)
        {
            var config = new DashboardConfigEntity
            {
                TenantId = tenantId,
                ConfigurationJson = "{}",
                IsActive = true,
                CreatedBy = "admin",
                UpdatedBy = "admin"
            };

            _context.DashboardConfigs.Add(config);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBatchAsync(tenantIds.ToList());

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(tenantIds, tid => Assert.Contains(tid, result.Keys));
    }
}

// Mock helper for ILogger
file class Mock<T> : Moq.Mock<T> where T : class { }
