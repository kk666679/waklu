using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HalalChain.Localization;

public class AppLocalizer : IHcLocalizer
{
    private const string CatalogBasePath = "i18n";
    private const string FileNameTemplate = "{0}.json";
    private static readonly string[] FallbackChain = new[] { "en" };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;
    private readonly NavigationManager _nav;
    private readonly ILogger<AppLocalizer> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private bool _loaded;

    public AppLocalizer(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment env,
        NavigationManager nav,
        ILogger<AppLocalizer> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
        _nav = nav;
        _logger = logger;
    }

    public event Action? OnChange;

    public string CurrentCulture { get; private set; } = SupportedCultures.DefaultCulture;

    public CultureDescriptor CurrentDescriptor =>
        SupportedCultures.Find(CurrentCulture) ?? SupportedCultures.Find(SupportedCultures.DefaultCulture)!;

    public IReadOnlyList<CultureDescriptor> Supported => SupportedCultures.All;

    public async Task LoadAsync()
    {
        if (_loaded) return;
        await _loadGate.WaitAsync();
        try
        {
            if (_loaded) return;
            CurrentCulture = ResolveInitialCulture();
            await EnsureLoadedAsync(CurrentCulture);
            await EnsureLoadedAsync(SupportedCultures.DefaultCulture);
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public async Task SetCultureAsync(string cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId) || !SupportedCultures.IsSupported(cultureId))
            return;
        if (string.Equals(cultureId, CurrentCulture, StringComparison.OrdinalIgnoreCase))
            return;

        await EnsureLoadedAsync(cultureId);
        CurrentCulture = cultureId;

        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
        {
            ctx.Response.Cookies.Append(SupportedCultures.CookieName, cultureId, new CookieOptions
            {
                Path = "/",
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
        }

        OnChange?.Invoke();
    }

    public string T(string key, params object?[] args)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (TryResolve(CurrentCulture, key, out var v) || TryResolve(SupportedCultures.DefaultCulture, key, out v))
        {
            return Format(v!, args);
        }
        return key;
    }

    public string T(CultureDescriptor descriptor, string key, params object?[] args)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (TryResolve(descriptor.Id, key, out var v) || TryResolve(SupportedCultures.DefaultCulture, key, out v))
        {
            return Format(v!, args);
        }
        return key;
    }

    private bool TryResolve(string culture, string key, out string? value)
    {
        value = null;
        if (_catalogs.TryGetValue(culture, out var dict) && dict.TryGetValue(key, out value))
            return true;
        return false;
    }

    private static string Format(string template, object?[] args)
    {
        if (args is null || args.Length == 0) return template;
        try { return string.Format(template, args); }
        catch { return template; }
    }

    private async Task EnsureLoadedAsync(string culture)
    {
        if (_catalogs.ContainsKey(culture)) return;
        var path = Path.Combine(_env.WebRootPath, CatalogBasePath, string.Format(FileNameTemplate, culture));
        if (!File.Exists(path))
        {
            _logger.LogWarning("Translation catalog not found at {Path}", path);
            _catalogs[culture] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }
        try
        {
            await using var stream = File.OpenRead(path);
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new Dictionary<string, string>();
            _catalogs[culture] = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load translation catalog {Culture}", culture);
            _catalogs[culture] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private string ResolveInitialCulture()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
        {
            if (ctx.Request.Query.TryGetValue(SupportedCultures.QueryName, out var qv))
            {
                var q = qv.ToString();
                if (SupportedCultures.IsSupported(q))
                {
                    ctx.Response.Cookies.Append(SupportedCultures.CookieName, q, new CookieOptions
                    {
                        Path = "/", MaxAge = TimeSpan.FromDays(365), IsEssential = true, SameSite = SameSiteMode.Lax
                    });
                    return q;
                }
            }
            if (ctx.Request.Cookies.TryGetValue(SupportedCultures.CookieName, out var cookie) &&
                SupportedCultures.IsSupported(cookie))
            {
                return cookie;
            }
            var accept = ctx.Request.Headers.AcceptLanguage.ToString();
            if (!string.IsNullOrWhiteSpace(accept))
            {
                foreach (var part in accept.Split(','))
                {
                    var tag = part.Split(';')[0].Trim();
                    if (string.IsNullOrEmpty(tag)) continue;
                    var primary = tag.Split('-')[0];
                    if (SupportedCultures.IsSupported(primary)) return primary;
                    if (SupportedCultures.IsSupported(tag)) return tag;
                }
            }
        }
        return SupportedCultures.DefaultCulture;
    }
}
