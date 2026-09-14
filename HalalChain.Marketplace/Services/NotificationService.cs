using HalalChain.Marketplace.State;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace HalalChain.Marketplace.Services;

public interface INotificationService
{
    void Toast(string message, string style = "Info", int duration = 4000);
    void Success(string message);
    void Error(string message);
    void Warning(string message);
}

public sealed class NotificationService : INotificationService
{
    private readonly NotificationState _state;
    private readonly Radzen.NotificationService _radzen;

    public NotificationService(NotificationState state, Radzen.NotificationService radzen)
    {
        _state = state;
        _radzen = radzen;
    }

    public void Toast(string message, string style = "Info", int duration = 4000)
    {
        var ns = style switch
        {
            "Success" => NotificationSeverity.Success,
            "Error" => NotificationSeverity.Error,
            "Warning" => NotificationSeverity.Warning,
            _ => NotificationSeverity.Info
        };
        _radzen.Notify(new NotificationMessage { Severity = ns, Summary = "", Detail = message, Duration = duration });
        _state.Push(style, message);
    }

    public void Success(string message) => Toast(message, "Success");
    public void Error(string message) => Toast(message, "Error");
    public void Warning(string message) => Toast(message, "Warning");
}
