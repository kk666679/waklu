namespace HalalChain.Localization;

public interface IHcLocalizer
{
    string CurrentCulture { get; }
    CultureDescriptor CurrentDescriptor { get; }
    IReadOnlyList<CultureDescriptor> Supported { get; }
    event Action? OnChange;

    string T(string key, params object?[] args);
    string T(CultureDescriptor descriptor, string key, params object?[] args);

    Task SetCultureAsync(string cultureId);
    Task LoadAsync();
}
