using System.Reflection;

using HalalChain.Agents.Eval.Dag;
using HalalChain.Agents.Eval.Dag.Propagation;
using HalalChain.Agents.Eval.Taxonomy;
using HalalChain.Domain.Halal;

using Xunit;

namespace HalalChain.Agents.Tests;

/// <summary>
/// The load-bearing guarantee of the eval harness: it measures, it does not
/// decide. These tests fail if a future change gives the harness any way to
/// express a compliance outcome.
/// </summary>
public sealed class VerdictBoundaryTests
{
    private static readonly string[] Forbidden =
        ["verdict", "compliance", "halalstatus", "decision", "outcome", "approved", "rejected"];

    [Fact]
    public void EvalResult_DeclaresNoVerdictShapedMember()
    {
        var type = typeof(EvalResult);

        var offenders = type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => Forbidden.Any(f => m.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(m => m.Name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "EvalResult must not carry a verdict/compliance/outcome member. Evaluation " +
            "measures agent behavior; tawheed alone decides compliance. Offenders:\n" +
            string.Join("\n", offenders));
    }

    [Fact]
    public void NoTypeInTheEvalNamespace_NamesAVerdict()
    {
        var evalTypes = typeof(VerdictBoundaryTests).Assembly
            .GetTypes()
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith("HalalChain.Agents.Eval", StringComparison.Ordinal))
            .ToArray();

        var offenders = evalTypes
            .Where(t => Forbidden.Any(f => t.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "A type in the eval namespace is named after a decision concept. The harness " +
            "scores agents; it does not decide. Offenders:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void NoMemberInTheEvalNamespace_IsTypedAsAVerdictStateOrBinding()
    {
        // Belt and braces on the name check above: even a property called
        // "Result" typed as the domain's VerdictState would be a verdict by
        // another name. VerdictState is the domain's compliance vocabulary —
        // Halal / NotHalal / InsufficientEvidence — and the eval harness must
        // not be able to hold one.
        var verdictState = typeof(HalalChain.Domain.Halal.VerdictState);
        var verdictBinding = typeof(HalalChain.Domain.Halal.VerdictBinding);

        var offenders = new List<string>();

        foreach (var type in typeof(VerdictBoundaryTests).Assembly.GetTypes()
                     .Where(t => t.Namespace is not null
                                 && t.Namespace.StartsWith("HalalChain.Agents.Eval", StringComparison.Ordinal)))
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var memberType = member switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    _ => null,
                };

                if (memberType is null)
                    continue;

                if (verdictState.IsAssignableFrom(memberType) || verdictBinding.IsAssignableFrom(memberType))
                    offenders.Add($"{type.FullName}.{member.Name} : {memberType.Name}");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "The eval harness must not carry a VerdictState or a VerdictBinding. " +
            "Offenders:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void RuntimeDoesNotExposeAVerdictFieldOnItsResponseShape()
    {
        // The agents service is the boundary where an upstream agent could try
        // to smuggle a verdict in. The response DTO must have nowhere to put it.
        var runtime = typeof(HalalChain.Agents.Runtime.AgentWorkflowClient).Assembly;
        var responseDto = runtime.GetType("HalalChain.Agents.Runtime.AgentRunResponse");

        Assert.NotNull(responseDto);

        var offenders = responseDto!
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Equals("verdict", StringComparison.OrdinalIgnoreCase)
                        || m.Name.Equals("complianceStatus", StringComparison.OrdinalIgnoreCase)
                        || m.Name.Equals("isHalal", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "AgentRunResponse must not accept a verdict from the agents service. " +
            "Offenders:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EvalNamespace_ReachesBlobsOnlyThroughTheBlobStore()
    {
        // The eval harness holds a read-only credential against evidence
        // storage — the credential an auditor gets. If any eval type could
        // open a file, a path, or a directory itself, that boundary is
        // decorative and the harness could read evidence the store would not
        // serve it. Mirrors EvaluationArchitectureTests
        // .EvalReadsBlobOnlyThrough_IBlobStore, which cannot run while the
        // Api project is unbuildable.
        Type[] forbiddenTypes =
        [
            typeof(System.IO.FileStream),
            typeof(System.IO.FileInfo),
            typeof(System.IO.DirectoryInfo),
            typeof(System.IO.Directory),
        ];

        var offenders = new List<string>();

        foreach (var type in typeof(VerdictBoundaryTests).Assembly.GetTypes()
                     .Where(t => t.Namespace is not null
                                 && t.Namespace.StartsWith("HalalChain.Agents.Eval", StringComparison.Ordinal)))
        {
            foreach (var member in type.GetMembers(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var memberType = member switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    MethodInfo m => m.ReturnType,
                    _ => null,
                };

                if (memberType is null)
                    continue;

                if (forbiddenTypes.Any(f => f == memberType))
                    offenders.Add($"{type.FullName}.{member.Name} : {memberType.Name}");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Evaluation must reach blob content through IBlobStore only. Direct file " +
            "or directory access bypasses the read-only credential boundary. " +
            "Offenders:\n" + string.Join("\n", offenders));
    }
}

/// <summary>
/// The C# and Python taxonomies are compared by wire name in eval reports, so
/// they are one contract split across two runtimes.
/// </summary>
public sealed class FailureCategoryTests
{
    [Fact]
    public void EveryCategoryHasAWireName()
    {
        foreach (var category in FailureCategories.All)
            Assert.False(string.IsNullOrWhiteSpace(category.ToWireName()));
    }

    [Fact]
    public void WireNamesRoundTrip()
    {
        foreach (var category in FailureCategories.All)
        {
            Assert.True(FailureCategories.TryParseWireName(category.ToWireName(), out var parsed));
            Assert.Equal(category, parsed);
        }
    }

    [Fact]
    public void WireNamesAreUnique()
    {
        var names = FailureCategories.All.Select(c => c.ToWireName()).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("collector.missed_source", FailureCategory.CollectorMissedSource)]
    [InlineData("handoff.missing_required_field", FailureCategory.HandoffMissingRequiredField)]
    [InlineData("gap.false_positive", FailureCategory.GapFalsePositive)]
    [InlineData("verdict.mismatch", FailureCategory.TerminalOutcomeMismatch)]
    [InlineData("infra.llm_unavailable", FailureCategory.InfraLlmUnavailable)]
    public void PythonWireNames_ParseToTheCSharpEquivalent(string wireName, FailureCategory expected) =>
        Assert.Equal(expected, FailureCategories.ParseWireName(wireName));

    [Fact]
    public void UnknownWireNameThrowsRatherThanDefaultingToNone()
    {
        // Silently mapping an unknown category to None would erase a real
        // failure from the report.
        Assert.Throws<ArgumentException>(() => FailureCategories.ParseWireName("not.a.category"));
    }

    [Fact]
    public void PythonTaxonomyCategoryCountMatches()
    {
        // .halalchain/agents/app/eval/dag/taxonomy.py documents 21 categories
        // (22 enum members including None). A new category added on one side
        // must be added on the other, or reports stop comparing.
        Assert.Equal(21, FailureCategories.All.Count);
    }
}
