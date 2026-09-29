namespace HalalChain.Marketplace.Models.Dashboard;

using System;
using System.Collections.Generic;

/// <summary>
/// Database entity for persisting tenant dashboard configurations.
/// Stores complete configuration state for multi-tenant isolation and configuration versioning.
/// </summary>
public class DashboardConfigEntity
{
    /// <summary>Primary key - surrogate ID for database.</summary>
    public int Id { get; set; }

    /// <summary>Tenant identifier - foreign key reference to tenant.</summary>
    public string TenantId { get; set; } = null!;

    /// <summary>Serialized configuration JSON (entire TenantDashboardConfig).</summary>
    public string ConfigurationJson { get; set; } = null!;

    /// <summary>Configuration version for optimistic concurrency control.</summary>
    public int Version { get; set; }

    /// <summary>Whether this configuration is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Timestamp of creation.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User ID who created this configuration.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Timestamp of last modification.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User ID who last modified this configuration.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Optional notes about this configuration version.</summary>
    public string? Notes { get; set; }

    /// <summary>Concurrency token for EF Core optimistic updates.</summary>
    public byte[]? ConcurrencyToken { get; set; }

    /// <summary>Unique constraint on (TenantId, IsActive) - only one active config per tenant.</summary>
    public static readonly string UniqueActiveTenantIndex = nameof(TenantId);
}

/// <summary>
/// Audit log entity for tracking dashboard configuration changes.
/// Immutable - records every configuration update for compliance and debugging.
/// </summary>
public class DashboardConfigAuditEntity
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }

    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; set; } = null!;

    /// <summary>ID of the configuration that was changed.</summary>
    public int ConfigurationId { get; set; }

    /// <summary>Operation type: Create, Update, Delete, Activate, Deactivate.</summary>
    public string Operation { get; set; } = null!;

    /// <summary>Previous configuration JSON (for updates).</summary>
    public string? PreviousConfigurationJson { get; set; }

    /// <summary>New configuration JSON (for updates and creates).</summary>
    public string? NewConfigurationJson { get; set; }

    /// <summary>Change summary for quick review.</summary>
    public string? ChangesSummary { get; set; }

    /// <summary>User who made the change.</summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>Timestamp of change.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional reason for the change.</summary>
    public string? Reason { get; set; }

    /// <summary>IP address of requester (for audit trail).</summary>
    public string? RequestIpAddress { get; set; }

    /// <summary>User agent string (for audit trail).</summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Cache entry entity for distributed cache fallback and warm-up.
/// Stores pre-computed dashboard configurations for quick retrieval.
/// </summary>
public class DashboardConfigCacheEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Cache key (e.g., "dashboard:config:tenant-123").</summary>
    public string CacheKey { get; set; } = null!;

    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; set; } = null!;

    /// <summary>Serialized cached value.</summary>
    public string CachedValueJson { get; set; } = null!;

    /// <summary>Absolute expiration time (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When cache entry was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hit count for cache effectiveness metrics.</summary>
    public int HitCount { get; set; }

    /// <summary>Unique constraint on CacheKey.</summary>
    public static readonly string UniqueCacheKey = nameof(CacheKey);
}
