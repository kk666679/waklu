namespace HalalChain.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    event Action OnThemeChanged;
    void ToggleTheme();
    void SetTheme(string theme);
}

public class ThemeService : IThemeService
{
    private string _currentTheme = "light";
    public string CurrentTheme => _currentTheme;
    public event Action? OnThemeChanged;

    public void ToggleTheme()
    {
        _currentTheme = _currentTheme == "light" ? "dark" : "light";
        OnThemeChanged?.Invoke();
    }

    public void SetTheme(string theme)
    {
        if (theme != "light" && theme != "dark") return;
        _currentTheme = theme;
        OnThemeChanged?.Invoke();
    }
}
