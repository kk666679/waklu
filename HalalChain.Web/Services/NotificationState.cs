namespace HalalChain.Services;

public class NotificationState
{
    public event Action? OnChange;
    public List<ToastNotification> Notifications { get; private set; } = [];
    public void Success(string message) => Add("success", message);
    public void Error(string message) => Add("error", message);
    public void Warning(string message) => Add("warning", message);
    public void Info(string message) => Add("info", message);
    private void Add(string type, string message)
    {
        Notifications.Add(new ToastNotification { Type = type, Message = message, Id = Guid.NewGuid().ToString() });
        Notify();
    }
    public void Dismiss(string id)
    {
        Notifications.RemoveAll(n => n.Id == id);
        Notify();
    }
    private void Notify() => OnChange?.Invoke();
}

public class ToastNotification
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "info";
    public string Message { get; set; } = "";
}
