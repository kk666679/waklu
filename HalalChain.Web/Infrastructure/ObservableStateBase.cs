namespace HalalChain.Web.Infrastructure;

public abstract class ObservableStateBase : IDisposable
{
    public event Action? OnChange;

    public bool IsLoading { get; protected set; }
    public string? Error { get; protected set; }

    protected void Notify() => OnChange?.Invoke();
    protected virtual void Dispose(bool disposing) { }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
