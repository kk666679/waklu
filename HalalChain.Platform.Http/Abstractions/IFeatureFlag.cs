namespace HalalChain.Platform.Http.Abstractions;

public interface IFeatureFlag
{
    bool IsEnabled(string name);
    Task<bool> IsEnabledAsync(string name, CancellationToken ct = default);
    Task<bool> IsEnabledAsync(string name, Guid? userId, CancellationToken ct = default);
}