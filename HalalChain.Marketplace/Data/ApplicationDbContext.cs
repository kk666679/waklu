namespace HalalChain.Marketplace.Data;

using HalalChain.Marketplace.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Application database context for HalalChain Marketplace.
/// Includes DbSets for dashboard configuration and audit entities.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>Dashboard configurations.</summary>
    public DbSet<DashboardConfigEntity> DashboardConfigs { get; set; } = null!;

    /// <summary>Dashboard configuration audit log.</summary>
    public DbSet<DashboardConfigAuditEntity> DashboardConfigAudits { get; set; } = null!;

    /// <summary>Dashboard configuration cache entries.</summary>
    public DbSet<DashboardConfigCacheEntity> DashboardConfigCache { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DashboardConfigEntity
        modelBuilder.Entity<DashboardConfigEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.TenantId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ConfigurationJson)
                .IsRequired();

            entity.Property(e => e.Version)
                .HasDefaultValue(1);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(255);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.ConcurrencyToken)
                .IsConcurrencyToken();

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_DashboardConfig_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("IX_DashboardConfig_TenantId_IsActive")
                .IsUnique();

            entity.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_DashboardConfig_UpdatedAt");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_DashboardConfig_CreatedAt");
        });

        // Configure DashboardConfigAuditEntity
        modelBuilder.Entity<DashboardConfigAuditEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.TenantId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Operation)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ChangedBy)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Reason)
                .HasMaxLength(500);

            entity.Property(e => e.ChangesSummary)
                .HasMaxLength(1000);

            entity.Property(e => e.RequestIpAddress)
                .HasMaxLength(50);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            // Indexes for audit queries
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_DashboardConfigAudit_TenantId");

            entity.HasIndex(e => e.ConfigurationId)
                .HasDatabaseName("IX_DashboardConfigAudit_ConfigurationId");

            entity.HasIndex(e => new { e.TenantId, e.ChangedAt })
                .HasDatabaseName("IX_DashboardConfigAudit_TenantId_ChangedAt");

            entity.HasIndex(e => e.ChangedAt)
                .HasDatabaseName("IX_DashboardConfigAudit_ChangedAt");

            entity.Property(e => e.PreviousConfigurationJson)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.NewConfigurationJson)
                .HasColumnType("nvarchar(max)");
        });

        // Configure DashboardConfigCacheEntity
        modelBuilder.Entity<DashboardConfigCacheEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.CacheKey)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.TenantId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.CachedValueJson)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            entity.HasIndex(e => e.CacheKey)
                .HasDatabaseName("IX_DashboardConfigCache_CacheKey")
                .IsUnique();

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_DashboardConfigCache_TenantId");

            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_DashboardConfigCache_ExpiresAt");
        });
    }
}
