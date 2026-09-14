namespace HalalChain.AppServices;

public enum AppNotificationSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public record AppNotification(string Title, string Message, AppNotificationSeverity Severity = AppNotificationSeverity.Info);

public interface IAppNotificationService
{
    event Action<AppNotification>? OnNotify;
    void Success(string title, string message);
    void Info(string title, string message);
    void Warning(string title, string message);
    void Error(string title, string message);
}
