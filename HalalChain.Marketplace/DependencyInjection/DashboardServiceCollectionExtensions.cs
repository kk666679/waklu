namespace HalalChain.Marketplace.DependencyInjection;

using HalalChain.Marketplace.Services.Dashboard;
using HalalChain.Marketplace.Repositories.Dashboard;
using Microsoft.Extensions.DependencyInjection;

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
        var group = app.MapGroup("/api/v1/dashboard")
            .WithName("Dashboard API")
            .WithOpenApi();

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

        return app;
    }

    // Minimal API handlers
    private static async Task<IResult> GetDashboardConfig(
        IDashboardConfigService configService,
        ITenantContext tenantContext)
    {
        var config = await configService.GetConfigAsync(tenantContext);
        return Results.Ok(config);
    }

    private static async Task<IResult> UpdateDashboardConfig(
        IDashboardConfigService configService,
        TenantDashboardConfig config,
        ITenantContext tenantContext)
    {
        await configService.UpdateConfigAsync(tenantContext.TenantId, config);
        return Results.Ok(config);
    }

    private static async Task<IResult> GetConfigVersions(
        IDashboardConfigRepository repository,
        ITenantContext tenantContext)
    {
        var versions = await repository.GetAllVersionsAsync(tenantContext.TenantId);
        return Results.Ok(versions);
    }

    private static async Task<IResult> ActivateConfig(
        IDashboardConfigRepository repository,
        int configId,
        HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value ?? "system";
        var config = await repository.ActivateAsync(configId, userId, "Activated via API");
        return Results.Ok(config);
    }

    private static async Task<IResult> GetNavigation(
        IDashboardConfigService configService,
        ITenantContext tenantContext)
    {
        var navigation = await configService.GetNavigationAsync(tenantContext);
        return Results.Ok(navigation);
    }

    private static async Task<IResult> GetBreadcrumbs(
        INavigationService navigationService,
        ITenantContext tenantContext,
        HttpContext context)
    {
        var currentPath = context.Request.Query["path"].ToString();
        var breadcrumbs = await navigationService.GetBreadcrumbsAsync(tenantContext, currentPath);
        return Results.Ok(breadcrumbs);
    }

    private static async Task<IResult> GetAuditLog(
        IDashboardConfigRepository repository,
        ITenantContext tenantContext,
        int? configId = null,
        int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var audit = await repository.GetAuditLogAsync(tenantContext.TenantId, configId, since);
        return Results.Ok(audit);
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
            var expiredCount = await context.DashboardConfigCache
                .Where(c => c.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync();

            if (expiredCount > 0)
            {
                _logger.LogInformation("Cleaned up {ExpiredCount} expired cache entries", expiredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }
}
