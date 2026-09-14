using HalalChain.Marketplace.Realtime;
using HalalChain.Marketplace.Services;
using HalalChain.Marketplace.State;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Extensions;
using HalalChain.Platform.Http.Services;
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

        var platformApiBaseUrl = builder.Configuration["PlatformApi:BaseUrl"]
            ?? throw new InvalidOperationException("Missing PlatformApi:BaseUrl configuration.");

        builder.Services.AddHttpClient("PlatformApi", client =>
        {
            client.BaseAddress = new Uri(platformApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddPlatformHttpClient(client =>
        {
            client.BaseAddress = new Uri(platformApiBaseUrl);
        });
        builder.Services.AddPlatformApiClient();
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
        builder.Services.AddScoped<CartState>();
        builder.Services.AddScoped<NotificationState>();
        builder.Services.AddScoped<INotificationService, Marketplace.Services.NotificationService>();
        builder.Services.AddScoped<IThemeService, Marketplace.Services.ThemeService>();
        builder.Services.AddScoped<ISearchService, SearchService>();
        builder.Services.AddScoped<IWishlistService, WishlistService>();
        builder.Services.AddScoped<IPlatformApiClient>(sp => sp.GetRequiredService<PlatformApiClient>());

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
