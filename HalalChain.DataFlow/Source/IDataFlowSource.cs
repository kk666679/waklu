using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Models;

namespace HalalChain.DataFlow.Source;

public interface IDataFlowSource<TRecord> : IAsyncDisposable
{
    string Name { get; }
    string EntityName { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<DataFlowBatch<TRecord>> ExtractBatchAsync(CancellationToken cancellationToken = default);
    Task<bool> HasMoreDataAsync(CancellationToken cancellationToken = default);
    Task<DataFlowCheckpoint> GetCheckpointAsync(CancellationToken cancellationToken = default);
    Task SetCheckpointAsync(DataFlowCheckpoint checkpoint, CancellationToken cancellationToken = default);
    Task<long> GetEstimatedRecordCountAsync(CancellationToken cancellationToken = default);
}

public interface IDataFlowSourceFactory
{
    IDataFlowSource<TRecord> CreateSource<TRecord>(DataFlowSourceOptions options)
        where TRecord : class, new();
    IDataFlowSource<Dictionary<string, object?>> CreateDynamicSource(DataFlowSourceOptions options);
}