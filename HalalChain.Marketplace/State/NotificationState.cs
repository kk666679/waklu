namespace HalalChain.Marketplace.State;

public sealed class NotificationState
{
    public event Action? OnChange;

    public List<ToastMessage> Messages { get; } = new();

    public void Push(string title, string body, string style = "info")
    {
        Messages.Add(new ToastMessage(Guid.NewGuid(), title, body, style));
        OnChange?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        Messages.RemoveAll(m => m.Id == id);
        OnChange?.Invoke();
    }

    public void Clear() { Messages.Clear(); OnChange?.Invoke(); }
}

public sealed record ToastMessage(Guid Id, string Title, string Body, string Style);
