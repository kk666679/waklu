using HalalChain.DataFlow.Models;

namespace HalalChain.DataFlow;

/// <summary>
/// A configured extract-transform-validate-load workflow. Implementations
/// move halal supply-chain data between enterprise systems and the
/// HalalChain data layer with checkpointing, retry, and dead-lettering.
/// </summary>
public interface IDataFlowPipeline<TRecord>
    where TRecord : class, new()
{
    string Name { get; }
    Task<DataFlowMetrics> RunAsync(CancellationToken cancellationToken = default);
}
