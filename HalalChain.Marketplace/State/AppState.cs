namespace HalalChain.Marketplace.State;

public sealed class AppState
{
    public event Action? OnChange;

    private int _cartCount;
    public int CartCount
    {
        get => _cartCount;
        set { if (_cartCount != value) { _cartCount = value; Notify(); } }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; Notify(); } }
    }

    private void Notify() => OnChange?.Invoke();
}
