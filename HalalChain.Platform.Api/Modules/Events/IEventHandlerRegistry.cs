using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HalalChain.Platform.Api.Modules.Events;

/// <summary>
/// Registry for mapping event type names to their handler types.
/// Used by the OutboxBackgroundService to dispatch persisted events.
/// </summary>
public interface IEventHandlerRegistry
{
    Type? GetHandlerType(string eventType);
    Type? GetEventType(string eventType);
    Task HandleAsync(string eventType, object @event, CancellationToken ct = default);
}

public sealed class EventHandlerRegistry : IEventHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _eventTypeMap = new();
    private readonly ConcurrentDictionary<string, Type> _handlerTypeMap = new();

    public EventHandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterKnownEvents();
    }

    private void RegisterKnownEvents()
    {
        // Register all known event types and their handlers
        var eventTypes = new[]
        {
            (EventType: typeof(ProductCreatedEvent), HandlerType: typeof(IEventHandler<ProductCreatedEvent>)),
            (EventType: typeof(ProductUpdatedEvent), HandlerType: typeof(IEventHandler<ProductUpdatedEvent>)),
            (EventType: typeof(CertificateSubmittedEvent), HandlerType: typeof(IEventHandler<CertificateSubmittedEvent>)),
            (EventType: typeof(HalalVerificationRequestedEvent), HandlerType: typeof(IEventHandler<HalalVerificationRequestedEvent>)),
            (EventType: typeof(HalalVerificationCompletedEvent), HandlerType: typeof(IEventHandler<HalalVerificationCompletedEvent>)),
            (EventType: typeof(OrderPlacedEvent), HandlerType: typeof(IEventHandler<OrderPlacedEvent>)),
            (EventType: typeof(VendorApprovedEvent), HandlerType: typeof(IEventHandler<VendorApprovedEvent>)),
        };

        foreach (var (eventType, handlerType) in eventTypes)
        {
            _eventTypeMap[eventType.Name] = eventType;
            _handlerTypeMap[eventType.Name] = handlerType;
        }
    }

    public Type? GetHandlerType(string eventType) =>
        _handlerTypeMap.TryGetValue(eventType, out var type) ? type : null;

    public Type? GetEventType(string eventType) =>
        _eventTypeMap.TryGetValue(eventType, out var type) ? type : null;

    public async Task HandleAsync(string eventType, object @event, CancellationToken ct = default)
    {
        if (!_handlerTypeMap.TryGetValue(eventType, out var handlerType))
            return;

        var handler = _serviceProvider.GetService(handlerType);
        if (handler is null)
            return;

        // Use reflection to invoke the generic HandleAsync method
        var method = handlerType.GetMethod("HandleAsync");
        if (method is not null)
        {
            var task = (Task)method.Invoke(handler, [@event, ct])!;
            await task;
        }
    }
}
