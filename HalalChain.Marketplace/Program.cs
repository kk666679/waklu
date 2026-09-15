using HalalChain.Marketplace.Realtime;
using HalalChain.Marketplace.Services;
using HalalChain.Marketplace.Services.Wishlist;
using HalalChain.Marketplace.State;
using HalalChain.Marketplace.State.Cart;
using HalalChain.Marketplace.Services.Abstractions;
using HalalChain.Marketplace.State.Abstractions;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using Radzen;
using System.Globalization;

namespace HalalChain.Marketplace;

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

        // Shared API client with resilience & correlation ID
        builder.Services.AddHalalChainApiClient(builder.Configuration);
        builder.Services.AddScoped<IApiNotifier, MarketplaceApiNotifier>();
        builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

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

        // Routed services: InMemory + Api + Router (flag-controlled)
        builder.Services.AddRoutedService<IWishlistService, WishlistServiceInMemory, WishlistServiceApi, WishlistServiceRouter>();
        builder.Services.AddRoutedService<ICartState, CartStateInMemory, CartStateApi, CartStateRouter>();

        // Unchanged: already API-backed or client-side
        builder.Services.AddScoped<INotificationService, Marketplace.Services.NotificationService>();
        builder.Services.AddScoped<IThemeService, Marketplace.Services.ThemeService>();
        builder.Services.AddScoped<ISearchService, SearchService>();

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
        app.MapFallbackToPage("/_Host");
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.Run();
    }
}