namespace HalalChain.Agents.Runtime;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using HalalChain.Application.Agentic.Abstractions;
using HalalChain.Application.Agentic.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Runs agent workflows by calling the agents service over HTTP.
///
/// The agent loop itself lives in Python (.halalchain/agents/). This is the
/// .NET half: sequencing, transport, budget, and the translation of service
/// responses into <see cref="EvidenceProposal"/>.
///
/// It returns evidence. It never returns a verdict, and it has no field,
/// property, or response shape through which one could arrive. If the agents
/// service tries to send one, deserialization drops it — the response DTO has
/// nowhere to put it. That is intentional: the type makes the boundary
/// un-crossable rather than merely documented.
///
/// There is no in-process fallback. If the agents service is unreachable the
/// call fails, matching the same rule ITawheedClient follows for policy. A
/// local "best guess" agent loop is a second, unauditable decision path.
/// </summary>
public sealed class AgentWorkflowClient(
    HttpClient http,
    IOptions<AgentsRuntimeOptions> options,
    ILogger<AgentWorkflowClient> logger) : IAgentWorkflow
{
    private readonly AgentsRuntimeOptions _options = options.Value;

    public async Task<IReadOnlyList<EvidenceProposal>> RunAsync(
        string workflow,
        object input,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflow);
        ArgumentNullException.ThrowIfNull(input);

        if (!_options.AllowedWorkflows.Contains(workflow, StringComparer.Ordinal))
        {
            throw new AgentWorkflowNotAllowedException(
                workflow,
                $"Workflow '{workflow}' is not in Agents:AllowedWorkflows. " +
                "Running an unregistered workflow means its evaluation goldens and " +
                "scorers may not exist, so its failures would go unnoticed.");
        }

        var request = new AgentRunRequest(workflow, input);

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync($"/api/v1/workflows/{workflow}/run", request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AgentWorkflowUnavailableException(workflow, ex.Message, ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            {
                throw new AgentWorkflowUnavailableException(
                    workflow, $"Agents service returned {(int)response.StatusCode}.");
            }

            response.EnsureSuccessStatusCode();

            AgentRunResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<AgentRunResponse>(cancellationToken: ct);
            }
            catch (JsonException ex)
            {
                throw new AgentWorkflowUnavailableException(
                    workflow, $"Agents service returned a malformed response: {ex.Message}", ex);
            }

            if (payload is null)
                throw new AgentWorkflowUnavailableException(workflow, "Agents service returned an empty body.");

            logger.LogDebug(
                "Workflow {Workflow} run {RunId} produced {ProposalCount} evidence proposal(s).",
                workflow, payload.RunId, payload.Proposals.Count);

            return payload.Proposals.Select(ToProposal).ToArray();
        }
    }

    private static EvidenceProposal ToProposal(AgentProposalDto dto) => new()
    {
        Kind = Enum.TryParse<EvidenceKind>(dto.Kind, ignoreCase: true, out var kind)
            ? kind
            : EvidenceKind.Other,
        Summary = dto.Summary ?? string.Empty,
        Confidence = dto.Confidence,
        SourceRefs = dto.SourceRefs?.Select(s => new SourceRef(
            s.Kind ?? string.Empty,
            s.Reference ?? string.Empty,
            s.RetrievedAt,
            s.Note)).ToArray() ?? Array.Empty<SourceRef>(),
        ProposedEvidence = dto.Evidence ?? new Dictionary<string, object>(),
    };
}

/// <summary>
/// Response DTO for a workflow run.
///
/// Deliberately has no verdict-shaped member. The agents service reporting a
/// verdict is already a contract violation; giving the field a name here would
/// let that violation travel silently into the .NET side. Anything extra in
/// the JSON is ignored on deserialization.
/// </summary>
internal sealed record AgentRunResponse(
    string RunId,
    IReadOnlyList<AgentProposalDto> Proposals);

internal sealed record AgentProposalDto(
    string? Kind,
    string? Summary,
    double Confidence,
    IReadOnlyList<AgentSourceRefDto>? SourceRefs,
    IReadOnlyDictionary<string, object>? Evidence);

internal sealed record AgentSourceRefDto(
    string? Kind,
    string? Reference,
    string? RetrievedAt,
    string? Note);

internal sealed record AgentRunRequest(
    [property: JsonPropertyName("workflow")] string Workflow,
    [property: JsonPropertyName("input")] object Input);

public sealed class AgentWorkflowUnavailableException(string workflow, string detail, Exception? inner = null)
    : InvalidOperationException($"Agent workflow '{workflow}' is unavailable: {detail}", inner)
{
    public string Workflow { get; } = workflow;
}

public sealed class AgentWorkflowNotAllowedException(string workflow, string detail)
    : InvalidOperationException($"Agent workflow '{workflow}' is not allowed: {detail}")
{
    public string Workflow { get; } = workflow;
}
