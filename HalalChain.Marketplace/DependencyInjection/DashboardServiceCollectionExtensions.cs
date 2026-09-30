namespace HalalChain.Marketplace.DependencyInjection;

using HalalChain.Marketplace.Services.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.Models.Dashboard;
using HalalChain.Application.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for registering dashboard services in the DI container.
/// Includes configuration storage, caching, and navigation services.
/// </summary>
public static class DashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers all dashboard services: config service, repositories, caching, navigation.
    /// Call this in Startup.cs or Program.cs ConfigureServices.
    /// </summary>
    public static IServiceCollection AddDashboardServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<DashboardConfigOptions>(
            configuration.GetSection("Dashboard"));

        // Register repositories
        services.AddScoped<IDashboardConfigRepository, DashboardConfigRepository>();

        // Register services
        services.AddScoped<IDashboardConfigService, DashboardConfigService>();
        services.AddScoped<INavigationService, NavigationService>();

        // Register caching (if enabled)
        var dashboardConfig = configuration.GetSection("Dashboard").Get<DashboardConfigOptions>();
        if (dashboardConfig?.EnableDistributedCache ?? false)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                var redisConnection = configuration["Dashboard:Caching:RedisConnectionString"]
                    ?? "localhost:6379";
                options.Configuration = redisConnection;
            });
        }
        else
        {
            // Use in-memory cache as fallback
            services.AddMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Registers dashboard background jobs and maintenance tasks.
    /// Call this after ConfigureServices for scheduled operations.
    /// </summary>
    public static IServiceCollection AddDashboardBackgroundJobs(
        this IServiceCollection services)
    {
        // Add hosted service for cache warm-up
        services.AddHostedService<DashboardCacheWarmupService>();

        // Add hosted service for cache cleanup
        services.AddHostedService<DashboardCacheCleanupService>();

        return services;
    }

    /// <summary>
    /// Registers dashboard endpoints for Minimal APIs.
    /// Call this in MapEndpoints or equivalent.
    /// </summary>
    public static WebApplication MapDashboardEndpoints(
        this WebApplication app)
    {
// Each endpoint below opts in to OpenAPI individually; the group only
        // carries the shared name and prefix.
        var group = app.MapGroup("/api/v1/dashboard")
            .WithName("Dashboard API");

#pragma warning disable ASPDEPR002
        // Configuration endpoints
        group.MapGet("/config", GetDashboardConfig)
            .WithName("GetDashboardConfig")
            .WithOpenApi()
            .Produces<TenantDashboardConfig>(StatusCodes.Status200OK)
            .WithSummary("Get tenant dashboard configuration")
            .RequireAuthorization();

        group.MapPost("/config", UpdateDashboardConfig)
            .WithName("UpdateDashboardConfig")
            .WithOpenApi()
            .Produces<TenantDashboardConfig>(StatusCodes.Status200OK)
            .WithSummary("Update tenant dashboard configuration")
            .RequireAuthorization("Administrator");

        group.MapGet("/config/versions", GetConfigVersions)
            .WithName("GetConfigVersions")
            .WithOpenApi()
            .Produces<List<DashboardConfigEntity>>(StatusCodes.Status200OK)
            .WithSummary("Get configuration version history")
            .RequireAuthorization("Administrator");

        group.MapPost("/config/{configId}/activate", ActivateConfig)
            .WithName("ActivateConfig")
            .WithOpenApi()
            .Produces<DashboardConfigEntity>(StatusCodes.Status200OK)
            .WithSummary("Activate configuration version")
            .RequireAuthorization("Administrator");

        // Navigation endpoints
        group.MapGet("/navigation", GetNavigation)
            .WithName("GetNavigation")
            .WithOpenApi()
            .Produces<List<NavigationItem>>(StatusCodes.Status200OK)
            .WithSummary("Get navigation items for tenant")
            .RequireAuthorization();

        group.MapGet("/breadcrumbs", GetBreadcrumbs)
            .WithName("GetBreadcrumbs")
            .WithOpenApi()
            .Produces<List<BreadcrumbItem>>(StatusCodes.Status200OK)
            .WithSummary("Get breadcrumb trail for current page")
            .RequireAuthorization();

        // Audit endpoints
        group.MapGet("/audit-log", GetAuditLog)
            .WithName("GetAuditLog")
            .WithOpenApi()
            .Produces<List<DashboardConfigAuditEntity>>(StatusCodes.Status200OK)
            .WithSummary("Get configuration audit log")
            .RequireAuthorization("Administrator");
#pragma warning restore ASPDEPR002

        return app;
    }

    // Minimal API handlers
    private static async Task<IResult> GetDashboardConfig(
        IDashboardConfigService configService,
        ITenantContext tenantContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var config = await configService.GetConfigAsync(tenantContext);
            logger.LogDebug("Dashboard config retrieved for tenant {TenantId}", tenantContext.TenantId);
            return Results.Ok(config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard config for tenant {TenantId}", tenantContext.TenantId);
            return Results.BadRequest(new { error = "Failed to retrieve dashboard configuration" });
        }
    }

    private static async Task<IResult> UpdateDashboardConfig(
        IDashboardConfigService configService,
        TenantDashboardConfig config,
        ITenantContext tenantContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            if (config == null)
                return Results.BadRequest(new { error = "Configuration cannot be null" });

            await configService.UpdateConfigAsync(tenantContext.TenantId, config);
            logger.LogInformation("Dashboard config updated for tenant {TenantId}", tenantContext.TenantId);
            return Results.Ok(new { message = "Configuration updated successfully", config });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating dashboard config for tenant {TenantId}", tenantContext.TenantId);
            return Results.BadRequest(new { error = "Failed to update dashboard configuration" });
        }
    }

    private static async Task<IResult> GetConfigVersions(
        IDashboardConfigService configService,
        ITenantContext tenantContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var versions = await configService.GetConfigurationVersionsAsync(tenantContext.TenantId);
            logger.LogDebug("Retrieved {VersionCount} configuration versions for tenant {TenantId}",
                versions.Count, tenantContext.TenantId);
            return Results.Ok(versions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting configuration versions for tenant {TenantId}", tenantContext.TenantId);
            return Results.BadRequest(new { error = "Failed to retrieve configuration versions" });
        }
    }

    private static async Task<IResult> ActivateConfig(
        IDashboardConfigService configService,
        int configId,
        HttpContext context,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var reason = context.Request.Query["reason"].ToString();
            await configService.ActivateConfigurationVersionAsync(configId, reason);
            logger.LogInformation("Configuration version {ConfigId} activated", configId);
            return Results.Ok(new { message = "Configuration activated successfully", configId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating configuration {ConfigId}", configId);
            return Results.BadRequest(new { error = "Failed to activate configuration" });
        }
    }

    private static async Task<IResult> GetNavigation(
        IDashboardConfigService configService,
        ITenantContext tenantContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var navigation = await configService.GetNavigationAsync(tenantContext);
            logger.LogDebug("Retrieved navigation for tenant {TenantId}", tenantContext.TenantId);
            return Results.Ok(navigation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting navigation for tenant {TenantId}", tenantContext.TenantId);
            return Results.BadRequest(new { error = "Failed to retrieve navigation" });
        }
    }

    private static async Task<IResult> GetBreadcrumbs(
        INavigationService navigationService,
        ITenantContext tenantContext,
        HttpContext context,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var currentPath = context.Request.Query["path"].ToString();
            if (string.IsNullOrEmpty(currentPath))
                return Results.BadRequest(new { error = "path query parameter is required" });

            var breadcrumbs = await navigationService.GetBreadcrumbsAsync(tenantContext, currentPath);
            logger.LogDebug("Retrieved breadcrumbs for path {Path}", currentPath);
            return Results.Ok(breadcrumbs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting breadcrumbs");
            return Results.BadRequest(new { error = "Failed to retrieve breadcrumbs" });
        }
    }

    private static async Task<IResult> GetAuditLog(
        IDashboardConfigService configService,
        ITenantContext tenantContext,
        HttpContext context,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DashboardApi");

        try
        {
            var configIdStr = context.Request.Query["configId"].ToString();
            var daysStr = context.Request.Query["days"].ToString() ?? "30";

            int? configId = string.IsNullOrEmpty(configIdStr) ? null : int.Parse(configIdStr);
            int days = int.TryParse(daysStr, out var d) ? d : 30;

            var since = DateTime.UtcNow.AddDays(-days);
            var audit = await configService.GetAuditLogAsync(tenantContext.TenantId, configId, since);

            logger.LogDebug("Retrieved {AuditCount} audit entries for tenant {TenantId}",
                audit.Count, tenantContext.TenantId);
            return Results.Ok(audit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting audit log for tenant {TenantId}", tenantContext.TenantId);
            return Results.BadRequest(new { error = "Failed to retrieve audit log" });
        }
    }
}

/// <summary>Hosted service for warming up dashboard configuration cache on startup.</summary>
public class DashboardCacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardCacheWarmupService> _logger;

    public DashboardCacheWarmupService(
        IServiceProvider serviceProvider,
        ILogger<DashboardCacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting dashboard configuration cache warm-up");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IDashboardConfigService>();
            var repository = scope.ServiceProvider.GetRequiredService<IDashboardConfigRepository>();

            // Get all active tenants
            var (allConfigs, _) = await repository.SearchAsync(isActive: true, pageSize: 1000);
            var tenantIds = allConfigs.Select(c => c.TenantId).Distinct().ToList();

            _logger.LogInformation("Warming up cache for {TenantCount} tenants", tenantIds.Count);

            // Pre-load configurations
            foreach (var tenantId in tenantIds)
            {
                try
                {
                    await configService.GetConfigAsync(tenantId);
                    _logger.LogDebug("Cache warmed for tenant {TenantId}", tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error warming cache for tenant {TenantId}", tenantId);
                }
            }

            _logger.LogInformation("Dashboard configuration cache warm-up completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dashboard configuration cache warm-up");
        }
    }
}

/// <summary>Hosted service for cleaning up expired cache entries.</summary>
public class DashboardCacheCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardCacheCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public DashboardCacheCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DashboardCacheCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting dashboard cache cleanup service");

        using var timer = new PeriodicTimer(_cleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupExpiredCacheEntriesAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dashboard cache cleanup service stopped");
        }
    }

    private async Task CleanupExpiredCacheEntriesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Delete expired cache entries
            var expiredEntries = await context.DashboardConfigCache
                .Where(c => c.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredEntries.Count > 0)
            {
                context.DashboardConfigCache.RemoveRange(expiredEntries);
                await context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {ExpiredCount} expired cache entries", expiredEntries.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }
}
