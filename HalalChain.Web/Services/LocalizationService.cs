using System.Globalization;
using System.Resources;
using System.Reflection;

namespace HalalChain.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resources = new("HalalChain.Localization.Resources", typeof(LocalizationService).Assembly);
    public string CurrentCulture { get; private set; } = "en";
    public IReadOnlyList<string> SupportedCultures { get; } = new[] { "en", "ar", "fr", "es", "de" };

    public string GetString(string key)
    {
        try { return _resources.GetString(key, new CultureInfo(CurrentCulture)) ?? key; }
        catch { return key; }
    }

    public void SetCulture(string culture)
    {
        if (SupportedCultures.Contains(culture)) CurrentCulture = culture;
    }
}
