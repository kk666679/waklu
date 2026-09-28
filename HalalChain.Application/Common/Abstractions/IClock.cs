namespace HalalChain.Application.Common.Abstractions;

/// <summary>
/// Deterministic clock. All handlers take this rather than reading
/// DateTimeOffset.UtcNow directly. Makes the expiry sweep testable
/// without Thread.Sleep or clock rewinding.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock(TimeProvider provider) : IClock
{
    public DateTimeOffset UtcNow => provider.GetUtcNow();
}
