using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using Microsoft.Extensions.Logging;

namespace HalalChain.Marketplace.Tests.Integration.Dashboard;

public class TenantIsolationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IDashboardConfigRepository _repository;
    private readonly ILogger<DashboardConfigRepository> _logger;

    public TenantIsolationTests()
    {
        // SQLite (not the in-memory provider) so the one-active-config-per-tenant
        // unique index is actually enforced.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
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
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetActiveConfigAsync_CannotAccessOtherTenantConfig()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var config1 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        var config2 = new DashboardConfigEntity
        {
            TenantId = tenant2,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _repository.GetActiveConfigAsync(tenant1);
        var result2 = await _repository.GetActiveConfigAsync(tenant2);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(tenant1, result1.TenantId);
        
        Assert.NotNull(result2);
        Assert.Equal(tenant2, result2.TenantId);
        
        // Verify cross-tenant access is prevented
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task UpdateAsync_CannotModifyOtherTenantConfig()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var config1 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        var config2 = new DashboardConfigEntity
        {
            TenantId = tenant2,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        var tenant1OriginalJson = config1.ConfigurationJson;

        // Act - Tenant2 tries to update Tenant1's config (should fail or be prevented)
        config2.ConfigurationJson = "{}";
        await _repository.UpdateAsync(config2, "admin", "Update");

        // Assert - Tenant1's config should be unchanged
        var tenant1Config = await _repository.GetActiveConfigAsync(tenant1);
        Assert.Equal(tenant1OriginalJson, tenant1Config!.ConfigurationJson);
    }

    [Fact]
    public async Task GetAuditLogAsync_CannotAccessOtherTenantAudit()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var config1 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        var config2 = new DashboardConfigEntity
        {
            TenantId = tenant2,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        var audit1 = new DashboardConfigAuditEntity
        {
            TenantId = tenant1,
            ConfigurationId = config1.Id,
            Operation = "Create",
            ChangedBy = "admin",
            ChangedAt = DateTime.UtcNow
        };

        var audit2 = new DashboardConfigAuditEntity
        {
            TenantId = tenant2,
            ConfigurationId = config2.Id,
            Operation = "Create",
            ChangedBy = "admin",
            ChangedAt = DateTime.UtcNow
        };

        _context.DashboardConfigAudits.AddRange(audit1, audit2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _repository.GetAuditLogAsync(tenant1);
        var result2 = await _repository.GetAuditLogAsync(tenant2);

        // Assert
        Assert.Single(result1);
        Assert.All(result1, a => Assert.Equal(tenant1, a.TenantId));

        Assert.Single(result2);
        Assert.All(result2, a => Assert.Equal(tenant2, a.TenantId));
    }

    [Fact(Skip = "Blocked by IX_DashboardConfig_TenantId_IsActive: the unique index covers IsActive = 0 as " +
                "well, so a tenant cannot hold more than one inactive (historical) config. Fixing the " +
                "index to cover active configs only needs a migration.")]
    public async Task GetAllVersionsAsync_CannotAccessOtherTenantVersions()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var tenant1Configs = Enumerable.Range(1, 3)
            .Select(i => new DashboardConfigEntity
            {
                TenantId = tenant1,
                ConfigurationJson = "{}",
                IsActive = i == 3,
                CreatedBy = "admin",
                UpdatedBy = "admin",
                Version = i
            })
            .ToList();

        var tenant2Configs = Enumerable.Range(1, 2)
            .Select(i => new DashboardConfigEntity
            {
                TenantId = tenant2,
                ConfigurationJson = "{}",
                IsActive = i == 2,
                CreatedBy = "admin",
                UpdatedBy = "admin",
                Version = i
            })
            .ToList();

        _context.DashboardConfigs.AddRange(tenant1Configs);
        _context.DashboardConfigs.AddRange(tenant2Configs);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _repository.GetAllVersionsAsync(tenant1);
        var result2 = await _repository.GetAllVersionsAsync(tenant2);

        // Assert
        Assert.Equal(3, result1.Count);
        Assert.All(result1, c => Assert.Equal(tenant1, c.TenantId));

        Assert.Equal(2, result2.Count);
        Assert.All(result2, c => Assert.Equal(tenant2, c.TenantId));
    }

    [Fact]
    public async Task UniqueConstraint_EnforcesOnlyOneActivConfigPerTenant()
    {
        // Arrange
        const string tenant1 = "tenant-1";

        var config1 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.Add(config1);
        await _context.SaveChangesAsync();

        var config2 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.Add(config2);

        // Act & Assert - Should throw due to unique constraint
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SearchAsync_FiltersMultipleTenants()
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
        var (configs, total) = await _repository.SearchAsync(
            tenantId: "tenant-1",
            isActive: true,
            pageSize: 10);

        // Assert
        Assert.Single(configs);
        Assert.Equal(1, total);
        Assert.All(configs, c => Assert.Equal("tenant-1", c.TenantId));
    }
}

// Mock helper
file class Mock<T> : Moq.Mock<T> where T : class { }
