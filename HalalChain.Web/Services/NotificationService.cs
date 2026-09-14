using HalalChain.AppServices;

namespace HalalChain.AppServices;

public class AppNotificationService : IAppNotificationService
{
    public event Action<AppNotification>? OnNotify;
    public void Success(string title, string message) => OnNotify?.Invoke(new AppNotification(title, message, AppNotificationSeverity.Success));
    public void Info(string title, string message) => OnNotify?.Invoke(new AppNotification(title, message, AppNotificationSeverity.Info));
    public void Warning(string title, string message) => OnNotify?.Invoke(new AppNotification(title, message, AppNotificationSeverity.Warning));
    public void Error(string title, string message) => OnNotify?.Invoke(new AppNotification(title, message, AppNotificationSeverity.Error));
}
