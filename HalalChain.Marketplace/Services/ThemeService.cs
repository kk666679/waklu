using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HalalChain.Marketplace.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    event Action? OnThemeChanged;
    Task ToggleAsync();
    Task SetAsync(string theme);
}

public sealed class ThemeService : IThemeService
{
    private readonly IJSRuntime _js;
    private string _theme = "light";

    public ThemeService(IJSRuntime js) => _js = js;

    public string CurrentTheme => _theme;
    public event Action? OnThemeChanged;

    public async Task ToggleAsync()
    {
        _theme = _theme == "light" ? "dark" : "light";
        await _js.InvokeVoidAsync("hcTheme.set", _theme);
        OnThemeChanged?.Invoke();
    }

    public async Task SetAsync(string theme)
    {
        _theme = theme;
        await _js.InvokeVoidAsync("hcTheme.set", theme);
        OnThemeChanged?.Invoke();
    }
}
