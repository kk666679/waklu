namespace HalalChain.Application.Agentic.Abstractions;

using HalalChain.Application.Agentic.Models;

public interface IAgentWorkflow
{
    Task<IReadOnlyList<EvidenceProposal>> RunAsync(
        string workflow,
        object input,
        CancellationToken ct = default);
}
