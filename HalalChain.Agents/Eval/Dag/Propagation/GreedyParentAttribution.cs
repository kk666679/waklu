namespace HalalChain.Agents.Eval.Dag.Propagation;

using HalalChain.Agents.Eval.Taxonomy;

/// <summary>
/// Attributes each failed node to its nearest failed ancestor, so a workflow
/// that fails five nodes reports one root cause rather than five problems.
///
/// The greedy-parent strategy (from the AgentEval paper):
///   1. Score every node independently.
///   2. For each failed node, walk backward along DAG edges.
///   3. The first failed ancestor is the root cause; everything downstream is
///      propagated.
///   4. A failure with no failed ancestor is itself an independent root cause.
///
/// Two properties this must preserve, both covered by tests: attribution is
/// cycle-safe (a malformed trace with an edge cycle must not hang the
/// evaluator), and attribution is deterministic (the same trace always yields
/// the same root causes, or eval reports are useless as a regression signal).
/// </summary>
public static class GreedyParentAttribution
{
    public static IReadOnlyList<EvalResult> Apply(AgentTrace trace, IReadOnlyList<NodeScore> scores)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(scores);

        var parents = BuildParentMap(trace);
        var resolved = ResolveFailures(scores, parents);

        return scores.Select(score => Attribute(trace.TraceId, score, resolved)).ToArray();
    }

    /// <summary>
    /// Counts failed nodes per category. A trace with one collector failure
    /// that propagated to four downstream nodes counts as one collector
    /// failure, which is the point of attributing root causes first.
    /// </summary>
    public static IReadOnlyDictionary<FailureCategory, int> AggregateTaxonomy(
        IReadOnlyList<EvalResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results
            .Where(r => !r.Passed && r.IsRootCause && r.Category != FailureCategory.None)
            .GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>The distinct root causes in a result set, in a stable order.</summary>
    public static IReadOnlyList<EvalResult> RootCauses(IReadOnlyList<EvalResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(r => !r.Passed && r.IsRootCause).ToArray();
    }

    private static EvalResult Attribute(
        string traceId,
        NodeScore score,
        IReadOnlyDictionary<string, string?> resolved)
    {
        if (score.Passed)
        {
            return new EvalResult
            {
                TraceId = traceId,
                NodeId = score.NodeId,
                NodeType = score.NodeType,
                Score = score.Score,
                Passed = true,
                IsRootCause = false,
                Details = score.Details,
            };
        }

        var ancestor = resolved.GetValueOrDefault(score.NodeId);

        return new EvalResult
        {
            TraceId = traceId,
            NodeId = score.NodeId,
            NodeType = score.NodeType,
            Score = score.Score,
            Passed = false,
            Category = score.Category,
            // No failed ancestor means this failure is independent.
            IsRootCause = ancestor is null,
            PropagatedFrom = ancestor,
            Details = score.Details,
        };
    }

    /// <summary>
    /// Nearest failed ancestor per failed node, with cycles broken
    /// deterministically.
    ///
    /// Phase 1 attributes each failure to the nearest failed ancestor, which
    /// is the greedy-parent strategy the Python implementation uses. Phase 2
    /// repairs the one case that strategy cannot handle: in a cyclic trace
    /// (a -&gt; b -&gt; a, both failed) every failed node points at another
    /// failed node, so nothing is a root cause and the report says "failures,
    /// cause unknown" — the least actionable output possible. Breaking the
    /// cycle at the ordinal-smallest node restores exactly one root, and
    /// because the choice is ordinal it is the same on every run, which is
    /// what makes an eval report usable as a regression signal.
    /// </summary>
    private static Dictionary<string, string?> ResolveFailures(
        IReadOnlyList<NodeScore> scores,
        IReadOnlyDictionary<string, IReadOnlyList<string>> parents)
    {
        var resolved = new Dictionary<string, string?>(StringComparer.Ordinal);
        var failed = scores.Where(s => !s.Passed).Select(s => s.NodeId).ToHashSet(StringComparer.Ordinal);

        foreach (var failure in failed.OrderBy(id => id, StringComparer.Ordinal))
            resolved[failure] = NearestFailedAncestor(failure, parents, failed);

        foreach (var failure in failed.OrderBy(id => id, StringComparer.Ordinal))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { failure };
            var current = failure;

            while (resolved.TryGetValue(current, out var next) && next is not null)
            {
                if (!seen.Add(next))
                {
                    resolved[seen.OrderBy(id => id, StringComparer.Ordinal).First()] = null;
                    break;
                }

                current = next;
            }
        }

        return resolved;
    }

    /// <summary>
    /// Nearest failed ancestor, breadth-first. A visited set bounds the search,
    /// so a trace whose edges form a cycle terminates instead of running away.
    /// </summary>
    private static string? NearestFailedAncestor(
        string nodeId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> parents,
        IReadOnlySet<string> failed)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { nodeId };
        var frontier = new Queue<string>();
        frontier.Enqueue(nodeId);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (!parents.TryGetValue(current, out var currentParents))
                continue;

            foreach (var parent in currentParents)
            {
                if (!visited.Add(parent))
                    continue;
                if (failed.Contains(parent))
                    return parent;
                frontier.Enqueue(parent);
            }
        }

        return null;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildParentMap(AgentTrace trace)
    {
        var known = trace.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

        var parents = trace.Nodes.ToDictionary(
            n => n.Id,
            _ => (IReadOnlyList<string>)Array.Empty<string>(),
            StringComparer.Ordinal);

        // Edges naming nodes the trace does not declare are dropped rather than
        // throwing: a partially-written trace is a real condition in shadow mode,
        // and one dangling edge must not abort evaluation of the whole trace.
        foreach (var edge in trace.Edges)
        {
            if (!known.Contains(edge.ToNodeId) || !known.Contains(edge.FromNodeId))
                continue;
            if (string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                continue;

            parents[edge.ToNodeId] = parents[edge.ToNodeId].Append(edge.FromNodeId).ToArray();
        }

        return parents;
    }
}
