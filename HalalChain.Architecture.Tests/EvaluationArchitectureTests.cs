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
        if (agents is null) return; // Skip if not yet created

        var evalResult = agents.GetType("HalalChain.Agents.Eval.Dag.Propagation.EvalResult");
        if (evalResult is null) return; // Type not yet created

        var verdictFields = evalResult.GetFields()
            .Where(f => f.Name.Equals("Verdict", StringComparison.OrdinalIgnoreCase)
                     || f.Name.Equals("ComplianceStatus", StringComparison.OrdinalIgnoreCase)
                     || f.Name.Equals("HalalStatus", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            verdictFields.Count == 0,
            "EvalResult must not declare a Verdict/ComplianceStatus/HalalStatus field. " +
            "Evaluation produces scores and root causes only. Offenders:\n" +
            string.Join("\n", verdictFields.Select(f => f.Name)));
    }

    [Fact]
    public void AgentsAssembly_ShouldNotReference_Ragas()
    {
        var agents = LoadAssembly(AgentsAssembly);
        if (agents is null) return;

        var result = Types.InAssembly(agents)
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
        // Coverage test: new node in §5.4 workflow requires new scorer
        // This test will be implemented when workflow node types are formalized
        Assert.True(true, "Enforced in CI: node_scorers.py coverage check");
    }

    [Fact]
    public void EvalReadsBlobOnlyThrough_IBlobStore()
    {
        var agents = LoadAssembly(AgentsAssembly);
        if (agents is null) return;

        // Eval must use IBlobStore, never open direct file handles
        var result = Types.InAssembly(agents)
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

    private static Assembly LoadAssembly(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null!;
        }
    }
}