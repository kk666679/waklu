using HalalChain.Platform.Http.Abstractions;
using HalalChain.AppServices;

namespace HalalChain.Services;

public sealed class WebApiNotifier(IAppNotificationService notifications) : IApiNotifier
{
    public Task NotifyErrorAsync(string message, CancellationToken ct = default)
    {
        notifications.Error("Error", message);
        return Task.CompletedTask;
    }

    public Task NotifyAuthExpiredAsync(CancellationToken ct = default)
    {
        notifications.Warning("Session", "Your session has expired. Please sign in again.");
        return Task.CompletedTask;
    }
}