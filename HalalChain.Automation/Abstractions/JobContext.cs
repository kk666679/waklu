namespace HalalChain.Automation.Abstractions;

using Microsoft.Extensions.Logging;

/// <summary>
/// Per-run context. Contains only what a job legitimately needs:
/// a clock, a logger, a correlation id, and the cancellation token.
///
/// Note what is NOT here: no HTTP context, no current user, no ambient
/// actor. Jobs run as the system, not on behalf of a user. Any job that
/// needs an actor id must pass it explicitly.
/// </summary>
public sealed class JobContext
{
    public required string JobName { get; init; }
    public required Guid CorrelationId { get; init; }
    public required TimeProvider Clock { get; init; }
    public required ILogger Logger { get; init; }
    public required IServiceProvider Services { get; init; }

    public DateTimeOffset Now => Clock.GetUtcNow();
}
