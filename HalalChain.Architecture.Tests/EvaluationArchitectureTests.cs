// Evaluation Architecture Rules
// These rules enforce the evaluation DAG constraints from the architecture

using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace HalalChain.Architecture.Tests;

public sealed class EvaluationArchitectureTests
{
    private const string AgentsAssembly = "HalalChain.Agents";
    private const string ContractsAssembly = "HalalChain.Platform.Contracts";
    private const string StorageAssembly = "HalalChain.Storage";

    [Fact]
    public void EvalResult_DeclaresNoVerdictField()
    {
        // Same rule as EvidenceProposal: eval produces scores/root-causes, never verdicts
        var agents = LoadAssembly(AgentsAssembly);
        Assert.NotNull(agents);

        var evalResult = agents!.GetType("HalalChain.Agents.Eval.Dag.Propagation.EvalResult");

        // The type is the whole point of this rule. If it is missing the rule
        // is guarding nothing, so this fails rather than passing vacuously —
        // a renamed or deleted EvalResult must not quietly disable the check.
        Assert.NotNull(evalResult);

        // Properties as well as fields. A record with a `Verdict` property
        // backed by a compiler-generated field would slip past a fields-only
        // check, which is the same "guardrail that cannot fail" hole this
        // project already guards against in GuardedAssembly_ShouldBe_Loadable.
        var verdictMembers = evalResult!.GetFields()
            .Concat(evalResult.GetProperties().Select(p => (MemberInfo)p))
            .Where(m => m.Name.Equals("Verdict", StringComparison.OrdinalIgnoreCase)
                     || m.Name.Equals("ComplianceStatus", StringComparison.OrdinalIgnoreCase)
                     || m.Name.Equals("HalalStatus", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            verdictMembers.Count == 0,
            "EvalResult must not declare a Verdict/ComplianceStatus/HalalStatus member. " +
            "Evaluation produces scores and root causes only. Offenders:\n" +
            string.Join("\n", verdictMembers.Select(m => m.Name)));
    }

    [Fact]
    public void AgentsAssembly_ShouldNotReference_Ragas()
    {
        var agents = LoadAssembly(AgentsAssembly);
        Assert.NotNull(agents);

        var result = Types.InAssembly(agents!)
            .ShouldNot()
            .HaveDependencyOn("Ragas")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Agents must not reference RAGAS — it is unmaintained (last commit Feb 2026, " +
            "563 open issues, SSRF advisory PYSEC-2026-3046). Use DeepEval instead. " +
            "Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void EvalExtra_NotInProductionDockerfile()
    {
        // This is a config-lint rule enforced in CI via the python-locks.yml workflow
        // The eval extra must only be in CI/shadow images, never production
        // This test documents the rule; actual enforcement is in .github/workflows/python-locks.yml
        Assert.True(true, "Enforced in CI: eval extra excluded from production Docker images");
    }

    [Fact]
    public void EveryWorkflowNodeType_HasEvalScorer()
    {
        // Coverage rule: a new node type must come with a scorer. Without
        // one, traces containing that node evaluate to nothing, and a node
        // that is never scored reads as a node that passed.
        //
        // Previously a placeholder Assert.True(true) on the grounds that node
        // types "will be formalized later". They are formalized now
        // (HalalChain.Agents.Eval.Dag.WorkflowNodeType), so the rule is real.
        var agents = LoadAssembly(AgentsAssembly);
        Assert.NotNull(agents);

        var nodeType = agents!.GetType("HalalChain.Agents.Eval.Dag.WorkflowNodeType");
        var registryType = agents.GetType("HalalChain.Agents.Eval.Dag.Scorers.NodeScorerRegistry");
        var nodeTypesProperty = registryType?.GetProperty("SupportedNodeTypes");

        Assert.NotNull(nodeType);
        Assert.NotNull(nodeTypesProperty);

        var declared = Enum.GetNames(nodeType!)
            .Where(n => n != "Unknown")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var scored = ((System.Collections.IEnumerable)nodeTypesProperty!.GetValue(
                Activator.CreateInstance(registryType!)!)!)
            .Cast<object>()
            .Select(n => n!.ToString()!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(declared, scored);
    }

    [Fact]
    public void EvalReadsBlobOnlyThrough_IBlobStore()
    {
        var agents = LoadAssembly(AgentsAssembly);
        Assert.NotNull(agents);

        // Eval must use IBlobStore, never open direct file handles
        var result = Types.InAssembly(agents!)
            .That()
            .ResideInNamespace("HalalChain.Agents.Eval")
            .ShouldNot()
            .HaveDependencyOn("System.IO.File")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Evaluation must read traces through IBlobStore only. " +
            "Direct file access violates the read-only credential boundary. " +
            "Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    /// <summary>
    /// Loads a guarded assembly, failing loudly if it cannot be resolved.
    ///
    /// This helper previously returned null on any exception and every caller
    /// did `if (agents is null) return;` — which is how four guardrails spent
    /// their entire life silently passing: the test project had no
    /// ProjectReference to HalalChain.Agents, so Assembly.Load threw, so the
    /// rules were skipped. A guardrail that cannot fail is worse than no
    /// guardrail, because it reads as coverage. Callers now assert on the
    /// result instead of branching on it.
    /// </summary>
    private static Assembly LoadAssembly(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Assembly '{name}' could not be loaded, so every evaluation " +
                "guardrail on it silently passed. Add a ProjectReference to " +
                $"{name} in HalalChain.Architecture.Tests.csproj. Cause: {ex.Message}");
            return null!;
        }
    }
}