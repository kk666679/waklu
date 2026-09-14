using System.Text.Json;
using HalalChain.Domain.Common;
using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Events;

public interface IOutboxRepository
{
    Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize = 50, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
}

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly HalalChainDbContext _db;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OutboxRepository(HalalChainDbContext db) => _db = db;

    public async Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload, s_jsonOptions),
            CreatedAt = now,
            CreatedAtUtc = now.UtcDateTime
        };
        _db.OutboxMessages.Add(message);
    }

    public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize = 50, CancellationToken ct = default)
    {
        return await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _db.OutboxMessages.FindAsync([id], ct);
        if (message is not null)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await _db.OutboxMessages.FindAsync([id], ct);
        if (message is not null)
        {
            message.RetryCount++;
            message.Error = error;
            await _db.SaveChangesAsync(ct);
        }
    }
}
