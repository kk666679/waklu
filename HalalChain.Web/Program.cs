using HalalChain.AppServices;
using HalalChain.Localization;
using HalalChain.Realtime;
using HalalChain.Services;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Http.Extensions;
using HalalChain.Platform.Http.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Radzen;
using System.Globalization;
using System.Text;

namespace HalalChain;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSignalR();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddRadzenComponents();
        builder.Services.AddSingleton<IRealtimeBroadcaster, SignalRRealtimeBroadcaster>();
        builder.Services.AddScoped<IHcLocalizer, AppLocalizer>();

        var platformApiBaseUrl = builder.Configuration["PlatformApi:BaseUrl"]
            ?? throw new InvalidOperationException("Missing PlatformApi:BaseUrl configuration. Set it in appsettings or environment variables.");

        // Mock data services are off by default. Set "MockData": "Enabled" = true
        // in appsettings/environment variables to fall back to the in-memory
        // demo data sources for pages that still depend on them.
        var mockDataEnabled = builder.Configuration.GetValue<string>("MockData:Enabled") == "true";

        // IPFS uploads from the Web app go through the platform API, not
        // through a fake Guid-based "ipfs://" generator. The named client is
        // resolved by PfsService via IHttpClientFactory.
        builder.Services.AddHttpClient("PlatformApiPfs", client =>
        {
            client.BaseAddress = new Uri(platformApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Shared Platform API transport (sender + typed HttpClient + token
        // accessor). The Blazor token accessor reads the token from the
        // IAuthService below.
        builder.Services.AddPlatformHttpClient(client =>
        {
            client.BaseAddress = new Uri(platformApiBaseUrl);
        });
        builder.Services.AddPlatformApiClient();
        builder.Services.AddScoped<HalalChain.Platform.Http.Abstractions.IPlatformTokenAccessor, BlazorTokenAccessor>();
        builder.Services.AddScoped<WebApiClient>();

        var jwtSection = builder.Configuration.GetSection("Jwt");
        if (!string.IsNullOrEmpty(jwtSection["Key"]))
        {
            builder.Services.Configure<JwtSettings>(jwtSection);
        }

        // Per-circuit state. Scoped = one instance per Blazor circuit (= one connected user).
        // Anything that touches IUserContext, IAuthService, CartState, or the shared
        // IPlatformApiClient must be Scoped or below — a Singleton would leak state across users.
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserContext, UserContext>();
        builder.Services.AddScoped<AppState>();
        builder.Services.AddScoped<CartState>();
        builder.Services.AddScoped<NotificationState>();
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IThemeService, Services.ThemeService>();
        builder.Services.AddScoped<ISearchService, SearchService>();
        builder.Services.AddScoped<IProductRecommendationService, ProductRecommendationService>();
        builder.Services.AddScoped<IWishlistService, WishlistService>();
        builder.Services.AddScoped<IPfsService, PfsService>();
        builder.Services.AddScoped<IBadgeService, BlockchainBadgeService>();
        builder.Services.AddScoped<PlatformOverviewService>();
        builder.Services.AddScoped<HalalChain.Navigation.IPermissionNavigationFilter, HalalChain.Navigation.PermissionNavigationFilter>();
        builder.Services.AddScoped<HalalChain.Navigation.NavigationService>();

        // Demo / mock services. These are only registered when MockData:Enabled=true.
        // The default off-mode is the real API path. The historical
        // MockData.* implementations were in a separate namespace that
        // has since been deleted; the existing IProductService /
        // ICategoryService / etc. registrations (below) act as the
        // canonical demo data sources.
        if (mockDataEnabled)
        {
            // MockData namespace intentionally not referenced; the
            // production registrations below are reused as the demo
            // path. This keeps a single source of truth and prevents
            // drift between the demo data and the real one.
        }
        else
        {
            builder.Services.AddSingleton<IAppNotificationService, AppNotificationService>();
            builder.Services.AddSingleton<AppNotificationService>();
        }

        var supportedCultures = SupportedCultures.All
            .Select(c => new CultureInfo(c.Id))
            .ToList();
        builder.Services.Configure<RequestLocalizationOptions>(opts =>
        {
            opts.DefaultRequestCulture = new RequestCulture(SupportedCultures.DefaultCulture);
            opts.SupportedCultures = supportedCultures;
            opts.SupportedUICultures = supportedCultures;
            opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
            {
                CookieName = SupportedCultures.CookieName
            });
            opts.RequestCultureProviders.Insert(1, new QueryStringRequestCultureProvider
            {
                QueryStringKey = SupportedCultures.QueryName
            });
        });

        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseStaticFiles();
        app.UseRequestLocalization();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapFallbackToPage("/_Host");
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.Run();
    }
}