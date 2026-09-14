namespace HalalChain.Services;

public interface ILocalizationService
{
    string GetString(string key);
    void SetCulture(string culture);
    string CurrentCulture { get; }
    IReadOnlyList<string> SupportedCultures { get; }
}
