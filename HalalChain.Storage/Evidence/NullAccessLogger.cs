using HalalChain.Application.Storage;

namespace HalalChain.Storage.Evidence;

/// <summary>No-op. Use only in tests, never in production.</summary>
internal sealed class NullAccessLogger : IAccessLog
{
    public Task RecordAsync(AccessRecord record, CancellationToken ct = default)
        => Task.CompletedTask;
}
