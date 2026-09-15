using System.Text.Json;
using HalalChain.Platform.Api.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HalalChain.Platform.Api.Modules.Events;

/// <summary>
/// Background service that polls the outbox for unprocessed messages
/// and dispatches them to the appropriate event handlers.
/// Implements exponential backoff retry (max 5 attempts).
/// </summary>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _maxBackoff = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox background service started");

        var consecutiveFailures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
                consecutiveFailures = 0;
            }
            catch (Exception ex) when (IsTransientDatabaseError(ex))
            {
                consecutiveFailures++;
                var backoff = CalculateBackoff(consecutiveFailures);
                _logger.LogWarning(
                    "Database unavailable for outbox processing (attempt {Attempt}); retrying in {Backoff}s. Error: {Error}",
                    consecutiveFailures, backoff.TotalSeconds, ex.Message);
                try
                {
                    await Task.Delay(backoff, stoppingToken);
                }
                catch (TaskCanceledException) { break; }
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
                consecutiveFailures = 0;
            }

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }

        _logger.LogInformation("Outbox background service stopped");
    }

    private static bool IsTransientDatabaseError(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            if (e is Npgsql.NpgsqlException
                || e is System.Net.Sockets.SocketException
                || e is System.IO.IOException
                || (e is InvalidOperationException ioe && ioe.Message.Contains("transient", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        var seconds = Math.Min(_maxBackoff.TotalSeconds, Math.Pow(2, Math.Min(attempt, 6)));
        return TimeSpan.FromSeconds(seconds);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var handlerRegistry = scope.ServiceProvider.GetRequiredService<IEventHandlerRegistry>();

        // Record pending count before processing
        var pending = await outbox.GetPendingAsync(batchSize: 50, ct);
        OutboxMetrics.Pending.Record(pending.Count);
        if (pending.Count > 0)
        {
            var oldest = pending.Min(m => m.CreatedAt);
            OutboxMetrics.OldestTimestamp.Record((DateTimeOffset.UtcNow - oldest).TotalSeconds);
        }

        foreach (var message in pending)
        {
            try
            {
                var handlerType = handlerRegistry.GetHandlerType(message.EventType);
                if (handlerType is null)
                {
                    _logger.LogWarning("No handler registered for event type {EventType}", message.EventType);
                    await outbox.MarkProcessedAsync(message.Id, ct); // Mark as processed (no handler)
                    OutboxMetrics.Dispatched.Record(1, new System.Diagnostics.Metrics.KeyValuePair<string, object?>("type", message.EventType));
                    continue;
                }

                // Deserialize the payload to the event type
                var eventType = handlerRegistry.GetEventType(message.EventType);
                if (eventType is null)
                {
                    _logger.LogWarning("Cannot resolve event type {EventType}", message.EventType);
                    await outbox.MarkFailedAsync(message.Id, "Cannot resolve event type", ct);
                    OutboxMetrics.DeadLettered.Record(1, new System.Diagnostics.Metrics.KeyValuePair<string, object?>("type", message.EventType));
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType, s_jsonOptions);
                if (@event is null)
                {
                    _logger.LogWarning("Failed to deserialize event {EventType}", message.EventType);
                    await outbox.MarkFailedAsync(message.Id, "Deserialization failed", ct);
                    OutboxMetrics.DeadLettered.Record(1, new System.Diagnostics.Metrics.KeyValuePair<string, object?>("type", message.EventType));
                    continue;
                }

                // Invoke the handler
                await handlerRegistry.HandleAsync(message.EventType, @event, ct);
                await outbox.MarkProcessedAsync(message.Id, ct);
                OutboxMetrics.Dispatched.Record(1, new System.Diagnostics.Metrics.KeyValuePair<string, object?>("type", message.EventType));

                _logger.LogDebug("Processed outbox message {MessageId} ({EventType})", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId} ({EventType})", message.Id, message.EventType);
                await outbox.MarkFailedAsync(message.Id, ex.Message, ct);
                OutboxMetrics.DeadLettered.Record(1, new System.Diagnostics.Metrics.KeyValuePair<string, object?>("type", message.EventType));
            }
        }
    }
}
