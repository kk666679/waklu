using HalalChain.Platform.Http.Abstractions;
using HalalChain.Marketplace.State;

namespace HalalChain.Marketplace.Services;

public sealed class MarketplaceApiNotifier(NotificationState notifications) : IApiNotifier
{
    public Task NotifyErrorAsync(string message, CancellationToken ct = default)
    {
        notifications.ShowError(message);
        return Task.CompletedTask;
    }

    public Task NotifyAuthExpiredAsync(CancellationToken ct = default)
    {
        notifications.ShowWarning("Please sign in to continue.");
        return Task.CompletedTask;
    }
}