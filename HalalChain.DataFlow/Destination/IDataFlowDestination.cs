using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;

namespace HalalChain.DataFlow.Destination;

public interface IDataFlowDestination<TRecord> : IAsyncDisposable
{
    string Name { get; }
    string EntityName { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<DataFlowBatchResult> LoadBatchAsync(DataFlowBatch<TRecord> batch, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<long> GetExistingRecordCountAsync(CancellationToken cancellationToken = default);
}

public sealed class DataFlowBatchResult
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString();
    public string PipelineName { get; set; } = string.Empty;
    public DataFlowBatchStatus Status { get; set; } = DataFlowBatchStatus.Completed;
    public long RecordsLoaded { get; set; }
    public long RecordsFailed { get; set; }
    public long RecordsSkipped { get; set; }
    public long BytesProcessed { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> SkippedRecordIds { get; set; } = new();
    public Dictionary<string, long> EntityCounts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}

public interface IDataFlowDestinationFactory
{
    IDataFlowDestination<TRecord> CreateDestination<TRecord>(DataFlowDestinationOptions options)
        where TRecord : class, new();
    IDataFlowDestination<Dictionary<string, object?>> CreateDynamicDestination(DataFlowDestinationOptions options);
}