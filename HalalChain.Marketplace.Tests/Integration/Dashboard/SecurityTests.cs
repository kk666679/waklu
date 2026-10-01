using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HalalChain.Marketplace.Tests.Integration.Dashboard;

public class SecurityTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IDashboardConfigRepository _repository;
    private readonly ILogger<DashboardConfigRepository> _logger;

    public SecurityTests()
    {
        // SQLite (not the in-memory provider) because DashboardConfigRepository
        // writes inside a transaction, and ApplicationDbContext configures
        // SQLite-specific defaults.
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
    public async Task CreateAsync_WithMaliciousJSON_ShouldNotBypass()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var maliciousJson = JsonSerializer.Serialize(new { TenantId = tenant2 });

        // Act - Try to create a config for tenant1 with tenant2's ID embedded
        var result = await _repository.CreateAsync(tenant1, maliciousJson, "admin");

        // Assert - The config should belong to tenant1, not tenant2
        Assert.Equal(tenant1, result.TenantId);
        
        var retrieved = await _repository.GetActiveConfigAsync(tenant1);
        Assert.NotNull(retrieved);
        Assert.Equal(tenant1, retrieved.TenantId);

        // Tenant2 should not have this config
        var tenant2Config = await _repository.GetActiveConfigAsync(tenant2);
        Assert.Null(tenant2Config);
    }

    [Fact]
    public async Task ActivateAsync_CannotActivateCrossTenantConfig()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        var config1 = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = false,
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

        // Act - Try to activate a config with tenant1's ID but it's actually tenant2's
        var activated = await _repository.ActivateAsync(config1.Id, "admin");

        // Assert
        Assert.True(activated.IsActive);
        Assert.Equal(tenant1, activated.TenantId);

        // Verify tenant2's config is deactivated only if it's the same tenant (it shouldn't be)
        var tenant2Reloaded = await _context.DashboardConfigs.FirstAsync(c => c.Id == config2.Id);
        Assert.True(tenant2Reloaded.IsActive); // Should remain active because it's different tenant
    }

    [Fact]
    public async Task DeactivateAsync_CannotDeactivateCrossTenantConfig()
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

        // Act - Deactivate config1
        await _repository.DeactivateAsync(config1.Id, "admin");

        // Assert
        var reloadedConfig1 = await _context.DashboardConfigs.FirstAsync(c => c.Id == config1.Id);
        Assert.False(reloadedConfig1.IsActive);

        var reloadedConfig2 = await _context.DashboardConfigs.FirstAsync(c => c.Id == config2.Id);
        Assert.True(reloadedConfig2.IsActive); // Unaffected
    }

    [Fact(Skip = "Requires a SQL Server provider: DashboardConfigEntity.ConcurrencyToken is a rowversion, " +
                "which SQLite does not implement, so the stale update is never rejected here.")]
    public async Task UpdateAsync_WithConcurrencyToken_PreventsRaceConditions()
    {
        // Arrange
        const string tenant1 = "tenant-1";

        var config = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        _context.DashboardConfigs.Add(config);
        await _context.SaveChangesAsync();

        // A second context over the same database stands in for the other
        // concurrent writer; a single context cannot hold two copies of the
        // same key.
        var otherOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        await using var otherContext = new ApplicationDbContext(otherOptions);
        var otherRepository = new DashboardConfigRepository(otherContext, _logger);

        var configCopy1 = await otherContext.DashboardConfigs.AsNoTracking().FirstAsync();
        var configCopy2 = await otherContext.DashboardConfigs.AsNoTracking().FirstAsync();

        _context.ChangeTracker.Clear();
        otherContext.ChangeTracker.Clear();

        configCopy1.ConfigurationJson = "\"updated-by-user-1\"";
        await _repository.UpdateAsync(configCopy1, "user1");

        // Act - Try to update with stale version
        configCopy2.ConfigurationJson = "\"updated-by-user-2\"";

        // Assert - Should throw concurrency exception
        await Assert.ThrowsAsync<HalalChain.Marketplace.Repositories.Dashboard.ConcurrencyException>(async () =>
        {
            await otherRepository.UpdateAsync(configCopy2, "user2");
        });
    }

    [Theory]
    [InlineData("tenant-1", true)]
    [InlineData("tenant-2", false)]
    public async Task GetBatchAsync_OnlyReturnsTenantConfigs(string targetTenant, bool shouldExist)
    {
        // Arrange
        var tenants = new[] { "tenant-1", "tenant-3", "tenant-5" };

        foreach (var tenant in tenants)
        {
            var config = new DashboardConfigEntity
            {
                TenantId = tenant,
                ConfigurationJson = "{}",
                IsActive = true,
                CreatedBy = "admin",
                UpdatedBy = "admin"
            };

            _context.DashboardConfigs.Add(config);
        }

        await _context.SaveChangesAsync();

        var searchTenants = new List<string> { "tenant-1", "tenant-2", "tenant-3" };

        // Act
        var result = await _repository.GetBatchAsync(searchTenants);

        // Assert
        if (shouldExist)
        {
            Assert.Contains(targetTenant, result.Keys);
        }
        else
        {
            Assert.DoesNotContain(targetTenant, result.Keys);
        }

        // Verify we only get requested tenants
        Assert.True(result.All(kvp => searchTenants.Contains(kvp.Key)));
    }

    [Fact]
    public async Task AuditLog_CapturesToEnforceAccountability()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string admin1 = "admin-user-1";
        const string admin2 = "admin-user-2";

        var config = new DashboardConfigEntity
        {
            TenantId = tenant1,
            ConfigurationJson = "{}",
            IsActive = true,
            CreatedBy = admin1,
            UpdatedBy = admin1
        };

        _context.DashboardConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act - Update by different admin
        config.ConfigurationJson = "\"updated\"";
        await _repository.UpdateAsync(config, admin2, "Updated by different admin");

        // Assert - Audit log should capture both admins
        var audit = await _context.DashboardConfigAudits.ToListAsync();
        Assert.Single(audit);
        Assert.Equal("Update", audit[0].Operation);
        Assert.Equal(admin2, audit[0].ChangedBy);
    }
}

// Mock helper
file class Mock<T> : Moq.Mock<T> where T : class { }
