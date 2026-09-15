using HalalChain.Platform.Http.Abstractions;

namespace HalalChain.Platform.Http.Abstractions;

public interface IApiNotifier
{
    Task NotifyErrorAsync(string message, CancellationToken ct = default);
    Task NotifyAuthExpiredAsync(CancellationToken ct = default);
}