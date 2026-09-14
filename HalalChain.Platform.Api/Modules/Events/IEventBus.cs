namespace HalalChain.Platform.Api.Modules.Events;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}

public interface IEventHandler<in T> where T : class
{
    Task HandleAsync(T @event, CancellationToken ct = default);
}

/// <summary>
/// In-process event bus with outbox dual-write.
/// 1. Writes event to outbox (same DB context — same transaction).
/// 2. Attempts immediate in-process dispatch (optimistic fast path).
/// 3. Background service handles any missed events as safety net.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(
        IServiceProvider serviceProvider,
        IOutboxRepository outbox,
        ILogger<InProcessEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Publishing event {EventType}", eventName);

        // 1. Write to outbox (will be committed with the caller's SaveChangesAsync)
        await _outbox.EnqueueAsync(eventName, @event, ct);

        // 2. Attempt immediate in-process dispatch (fast path)
        try
        {
            if (_serviceProvider.GetService(typeof(IEventHandler<T>)) is IEventHandler<T> handler)
            {
                await handler.HandleAsync(@event, ct);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail — the outbox background service will retry
            _logger.LogWarning(ex, "In-process handler failed for {EventType}, will be retried by outbox", eventName);
        }
    }
}

// ── Domain Events ──────────────────────────────────────────────────────

public sealed record ProductCreatedEvent(Guid ProductId, string Title, string Slug);
public sealed record ProductUpdatedEvent(Guid ProductId, string Title);
public sealed record CertificateSubmittedEvent(Guid ProductId, Guid CertificateId, string CertificateNumber);
public sealed record HalalVerificationRequestedEvent(Guid ProductId, Guid VerificationId);
public sealed record HalalVerificationCompletedEvent(Guid ProductId, Guid VerificationId, string ComplianceStatus, bool RequiresHumanReview);
public sealed record OrderPlacedEvent(Guid OrderId, string CustomerId, decimal Total, string Currency);
public sealed record VendorApprovedEvent(Guid VendorId, string VendorName);
