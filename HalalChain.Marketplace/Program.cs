using HalalChain.Marketplace.Realtime;
using HalalChain.Marketplace.Services;
using HalalChain.Marketplace.Services.Wishlist;
using HalalChain.Marketplace.State;
using HalalChain.Marketplace.State.Cart;
using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Marketplace.State.Abstractions;
using HalalChain.Marketplace.Data;
using HalalChain.Marketplace.DependencyInjection;
using HalalChain.Marketplace.Repositories;
using HalalChain.Marketplace.Models.Mapping;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Extensions;
using HalalChain.Application.Tenancy;
using HalalChain.Marketplace.Services.Tenancy;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Globalization;
using AutoMapper;

namespace HalalChain.Marketplace;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSignalR();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddRadzenComponents();

        // ── Platform Database (PostgreSQL) for domain entities ─────────────
        builder.Services.AddDbContext<PlatformDbContext>(o =>
        {
            var platformConn = builder.Configuration.GetConnectionString("PlatformConnection")
                ?? builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'PlatformConnection' not found.");
            o.UseNpgsql(platformConn, opts => opts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        // ── Dashboard Services (config, caching, navigation) ──────────────────
        builder.Services.AddDbContext<ApplicationDbContext>(o =>
        {
            var conn = builder.Configuration.GetConnectionString("DashboardConnection")
                ?? $"Data Source={builder.Environment.ContentRootPath}/marketplace.db";
            o.UseSqlite(conn);
        });

        // ── AutoMapper for domain → ViewModel mapping ──────────────────────
        // Scanning by assembly marker type picks up MappingProfile and any other
// profile defined in this assembly.
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        // ── Repository Registrations (all catalog, commerce, halal, etc.) ──
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IBrandRepository, BrandRepository>();
        builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        builder.Services.AddScoped<ITaxonomyCategoryRepository, TaxonomyCategoryRepository>();
        builder.Services.AddScoped<ISubcategoryRepository, SubcategoryRepository>();
        builder.Services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        builder.Services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
        builder.Services.AddScoped<ICertificationBodyRepository, CertificationBodyRepository>();
        builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
        builder.Services.AddScoped<ICountryRepository, CountryRepository>();

        builder.Services.AddScoped<IVendorRepository, VendorRepository>();
        builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
        builder.Services.AddScoped<IHalalVerificationRepository, HalalVerificationRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IVendorOrderRepository, VendorOrderRepository>();
        builder.Services.AddScoped<ICartRepository, CartRepository>();
        builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();

        builder.Services.AddDashboardServices(builder.Configuration);
        builder.Services.AddDashboardBackgroundJobs();

        // Shared API client with resilience & correlation ID
        builder.Services.AddHalalChainApiClient(builder.Configuration);

        // Shared Platform API transport (typed HttpClient + PlatformApiSender).
        // AddHalalChainApiClient registers IApiClient, not IPlatformApiClient, so
        // without these two lines CartState and the routed cart/wishlist services
        // have no IPlatformApiClient to resolve and host validation fails at startup.
        // IPlatformTokenAccessor is registered as AuthService further down.
        builder.Services.AddPlatformHttpClient(client =>
        {
            var platformApiBaseUrl = builder.Configuration["PlatformApi:BaseUrl"]
                ?? builder.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException("PlatformApi:BaseUrl or Api:BaseUrl is required.");
            client.BaseAddress = new Uri(platformApiBaseUrl);
        });
        builder.Services.AddPlatformApiClient();

        builder.Services.AddScoped<IApiNotifier, MarketplaceApiNotifier>();
        builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

        // Tenant isolation (P6): every dashboard/config read is scoped to the ambient tenant.
        builder.Services.AddScoped<ITenantContext, HttpContextTenantContext>();

        // AuthService owns the JWT state and also implements IPlatformTokenAccessor so the
        // shared typed HttpClient can attach the bearer header. Registering it once as the
        // accessor (and again as AuthService) lets Blazor components @inject AuthService while
        // the sender resolves the same scoped instance — this avoids the circular dependency
        // that a separate wrapper accessor would create.
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<IPlatformTokenAccessor, AuthService>();

        var jwtSection = builder.Configuration.GetSection("Jwt");
        if (!string.IsNullOrEmpty(jwtSection["Key"]))
        {
            builder.Services.Configure<JwtSettings>(jwtSection);
        }

        builder.Services.AddScoped<AppState>();
        builder.Services.AddScoped<NotificationState>();
        builder.Services.AddScoped<CartState>();

        // Routed services: InMemory + Api + Router (flag-controlled)
        builder.Services.AddRoutedService<IWishlistService, WishlistServiceInMemory, WishlistServiceApi, WishlistServiceRouter>();
        builder.Services.AddRoutedService<ICartState, CartStateInMemory, CartStateApi, CartStateRouter>();

        // Unchanged: already API-backed or client-side
        builder.Services.AddScoped<INotificationService, Marketplace.Services.NotificationService>();
        builder.Services.AddScoped<IThemeService, Marketplace.Services.ThemeService>();
        builder.Services.AddScoped<ISearchService, SearchService>();

        // ── Real-time SignalR notification services ──────────────────────────
        builder.Services.AddScoped<ICartNotificationService, CartNotificationService>();
        builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();

        var supportedCultures = new[] { "en", "ms", "id", "th", "vi", "tl", "zh-Hans", "ar" }
            .Select(c => new CultureInfo(c))
            .ToList();
        builder.Services.Configure<RequestLocalizationOptions>(opts =>
        {
            opts.DefaultRequestCulture = new RequestCulture("en");
            opts.SupportedCultures = supportedCultures;
            opts.SupportedUICultures = supportedCultures;
            opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider { CookieName = "hc-culture" });
            opts.RequestCultureProviders.Insert(1, new QueryStringRequestCultureProvider { QueryStringKey = "culture" });
        });

        var app = builder.Build();

        // ── Apply pending EF Core migrations ─────────────────────────────────
        using (var scope = app.Services.CreateScope())
        {
            // Dashboard database (SQLite)
            var dashboardDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (dashboardDb.Database.IsRelational())
            {
                await dashboardDb.Database.MigrateAsync();
            }

            // Platform database (PostgreSQL) - Ensure schema and tables exist
            var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            if (platformDb.Database.IsRelational())
            {
                await platformDb.Database.MigrateAsync();
            }
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.UseRequestLocalization();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<CartHub>("/hubs/cart");
        app.MapHub<OrderHub>("/hubs/orders");

        // ── Dashboard API endpoints ──────────────────────────────────────────
        app.MapDashboardEndpoints();

        app.MapFallbackToPage("/_Host");
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        await app.RunAsync();
    }
}
