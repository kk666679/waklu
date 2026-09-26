using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Prompt templates for the recurring agentic workflows on this platform.
///
/// The templates encode the repository's architectural invariant — evidence is
/// gathered by AI, verdicts are assigned only by the deterministic Policy
/// Engine — so a model that loads one of these is steered away from
/// answering a compliance question it has no right to answer.
/// </summary>
internal sealed class PromptRegistry : IPromptRegistry
{
    private const string Invariant =
        "AI discovers and interprets evidence. Deterministic systems decide " +
        "business and compliance outcomes. Never assign, infer, or override a " +
        "halal verdict yourself; surface the evidence and let the Policy Engine rule.";

    private static readonly IReadOnlyDictionary<string, string> JurisdictionCodes = new Dictionary<string, string>
    {
        ["MY"] = "Malaysia (JAKIM, MS1500:2019)",
        ["ID"] = "Indonesia (MUI, BPJPH)",
        ["SG"] = "Singapore (MUIS)",
        ["BN"] = "Brunei",
        ["GCC"] = "Gulf Cooperation Council",
        ["EU"] = "European Union",
    };

    private readonly List<PromptDefinition> _definitions = [];
    private readonly Dictionary<string, Func<IReadOnlyDictionary<string, string>, GetPromptResult>> _renderers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, IReadOnlyList<string>>> _suggestions =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PromptDefinition> All => _definitions;

    public PromptRegistry()
    {
        RegisterComplianceReview();
        RegisterPolicyEngineAudit();
        RegisterIncidentTriage();
        RegisterModuleOnboarding();
        RegisterCapabilityAudit();
    }

    public bool Contains(string name) => _renderers.ContainsKey(name);

    public bool TryRender(
        string name,
        IReadOnlyDictionary<string, string> arguments,
        out GetPromptResult? result,
        out string? error)
    {
        result = null;
        error = null;

        if (!_renderers.TryGetValue(name, out var renderer))
        {
            error = $"Unknown prompt '{name}'.";
            return false;
        }

        var definition = _definitions.First(d =>
            string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

        var missing = definition.Arguments
            .Where(a => a.Required == true)
            .Where(a => !arguments.ContainsKey(a.Name) || string.IsNullOrWhiteSpace(arguments[a.Name]))
            .Select(a => a.Name)
            .ToList();

        if (missing.Count > 0)
        {
            error = $"Missing required argument(s): {string.Join(", ", missing)}.";
            return false;
        }

        result = renderer(arguments);
        return true;
    }

    public IReadOnlyList<string> SuggestValues(string name, string argumentName, string prefix)
    {
        if (!_suggestions.TryGetValue(name, out var byArgument) ||
            !byArgument.TryGetValue(argumentName, out var candidates))
        {
            return [];
        }

        return candidates
            .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void RegisterComplianceReview()
    {
        const string name = "halal-compliance-review";

        _definitions.Add(new PromptDefinition
        {
            Name = name,
            Title = "Halal compliance evidence review",
            Description =
                "Structures an evidence review for a product or certificate and " +
                "routes the outcome through the deterministic Policy Engine.",
            Arguments =
            [
                new PromptArgument
                {
                    Name = "subject",
                    Description = "Product name, SKU, or certificate reference under review.",
                    Required = true,
                },
                new PromptArgument
                {
                    Name = "jurisdiction",
                    Description = "Certification authority: MY, ID, SG, BN, GCC, or EU. Defaults to MY.",
                },
                new PromptArgument
                {
                    Name = "evidence",
                    Description = "Raw evidence available: certificates, ingredient lists, supplier records.",
                },
            ],
        });

        _renderers[name] = args =>
        {
            var jurisdiction = Lookup(args, "jurisdiction", "MY");
            var evidence = Lookup(args, "evidence",
                "No evidence supplied. List what must be collected before the Policy Engine can rule.");

            return Result(
                "Evidence review scaffold",
                $"""
                 Review the halal standing of: {Lookup(args, "subject", "(unspecified)")}

                 Jurisdiction: {jurisdiction} — {JurisdictionCodes.GetValueOrDefault(jurisdiction, jurisdiction)}
                 Policy version: {(jurisdiction == "MY" ? "MY-v3" : $"{jurisdiction}-v1")}

                 Available evidence:
                 {evidence}

                 Do this:
                 1. Enumerate the evidence classes the Policy Engine requires for this
                    jurisdiction, and mark each as present, missing, or ambiguous.
                 2. For ambiguous items, state what would disambiguate them. This is the
                    only step where interpretation is appropriate.
                 3. Report the inputs you would hand the Policy Engine. Stop there.
                 4. State explicitly that the verdict (VERIFIED, MANUAL_REVIEW, or REJECT)
                    is the Policy Engine's output and must not be predicted here.

                 Invariant: {Invariant}
                 """);
        };

        _suggestions[name] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["jurisdiction"] = JurisdictionCodes.Keys.ToList(),
        };
    }

    private void RegisterPolicyEngineAudit()
    {
        const string name = "policy-engine-audit";

        _definitions.Add(new PromptDefinition
        {
            Name = name,
            Title = "Determinism audit",
            Description =
                "Audits a module for determinism violations — places where a model " +
                "influences a compliance outcome it should only inform.",
            Arguments =
            [
                new PromptArgument
                {
                    Name = "module",
                    Description = "Module or file path to audit, e.g. Modules/Halal or a runbook slug.",
                },
            ],
        });

        _renderers[name] = args =>
        {
            var module = Lookup(args, "module", "the whole Halal module tree");

            return Result(
                "Determinism audit",
                $"""
                 Audit: {module}

                 For every decision that ends in a compliance status, identify:
                 - Which inputs are deterministic and rule-evaluated.
                 - Which inputs are model-produced, and whether they are treated as
                   evidence (allowed) or as a verdict (a violation).
                 - Whether any status is derived from a temperature, a retry, a model
                   version, or a prompt change. Any of these makes the outcome
                   irreproducible.
                 - Whether the same inputs on a clean run produce the same status.

                 Consult docs/runbooks/determinism-violation.md for the escalation path
                 when a violation is found.

                 Invariant: {Invariant}
                 """);
        };
    }

    private void RegisterIncidentTriage()
    {
        const string name = "incident-triage";

        _definitions.Add(new PromptDefinition
        {
            Name = name,
            Title = "Service incident triage",
            Description =
                "Walks an incident from symptom to the matching runbook and first " +
                "diagnostic step.",
            Arguments =
            [
                new PromptArgument
                {
                    Name = "symptom",
                    Description = "What was observed, including the error text if there is one.",
                    Required = true,
                },
                new PromptArgument
                {
                    Name = "service",
                    Description = "Service short name: platform-api, halalchain, marketplace, " +
                                  "ai-inference, tawheed, postgres, redis, neo4j, qdrant.",
                },
            ],
        });

        _renderers[name] = args =>
        {
            var symptom = Lookup(args, "symptom", "(unspecified)");
            var service = Lookup(args, "service", "unknown");

            return Result(
                "Incident triage",
                $"""
                 Symptom: {symptom}
                 Suspected service: {service}

                 1. Map the symptom to a runbook under docs/runbooks/. Read the
                    matching runbook before proposing any remediation.
                 2. Separate the blast radius from the cause. State what is degraded
                    and what is fully down, and whether the service can still serve
                    operations that do not need the failed dependency.
                 3. Give the first two diagnostic commands, not the full fix. Confirm
                    the hypothesis before changing state.
                 4. Flag any step that would mutate data, and say what the rollback is.

                 Never weaken a health endpoint, disable a probe, or edit a
                 policy version to make an alert clear.
                 """);
        };

        _suggestions[name] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["service"] =
            [
                "platform-api", "halalchain", "marketplace", "ai-inference",
                "tawheed", "postgres", "redis", "neo4j", "qdrant",
            ],
        };
    }

    private void RegisterModuleOnboarding()
    {
        const string name = "module-onboarding";

        _definitions.Add(new PromptDefinition
        {
            Name = name,
            Title = "API module onboarding",
            Description =
                "Generates the wiring for a new module in the platform API, " +
                "matching the existing modular-monolith conventions.",
            Arguments =
            [
                new PromptArgument
                {
                    Name = "module",
                    Description = "Module name, e.g. Loyalty.",
                    Required = true,
                },
                new PromptArgument
                {
                    Name = "purpose",
                    Description = "One or two sentences on what the module owns.",
                },
            ],
        });

        _renderers[name] = args =>
        {
            var module = Lookup(args, "module", "(unspecified)");
            var purpose = Lookup(args, "purpose", "Not supplied — infer from the module name and confirm.");

            return Result(
                "Module onboarding",
                $"""
                 New module: {module}
                 Purpose: {purpose}

                 Produce:
                 1. The folder layout under HalalChain.Platform.Api/Modules/{module}/,
                    matching an existing module such as Halal or Commerce.
                 2. Entity, DTO, repository, service, and endpoint files, with the
                    endpoint routes namespaced under /api/{module.ToLowerInvariant()}.
                 3. The DI registration line, and where it belongs in the composition
                    root.
                 4. The migration, and the outbox event(s) the module publishes so other
                    modules react without reaching into its tables.
                 5. The architecture test that keeps the module from depending on
                    infrastructure directly.

                 A module owns its data. Other modules consume its API or its events,
                 never its tables. DTOs shared outside the module belong in
                 HalalChain.Platform.Contracts.

                 Invariant: {Invariant}
                 """);
        };
    }

    private void RegisterCapabilityAudit()
    {
        const string name = "mcp-capability-audit";

        _definitions.Add(new PromptDefinition
        {
            Name = name,
            Title = "MCP capability audit",
            Description =
                "Checks whether the MCP server exposes enough surface for an agent " +
                "to answer a real question about this platform without guessing.",
            Arguments = [],
        });

        _renderers[name] = _ => Result(
            "MCP capability audit",
            $"""
             Audit the HalalChain MCP server's surface.

             1. Call the tools list. Note each tool's annotations. Flag anything not
                marked read-only, and confirm no tool can mutate state.
             2. Call the resources list. Confirm the architecture, project inventory,
                and documentation are reachable as resources, not only as prose.
             3. Identify questions an operator would ask that no tool or resource can
                currently answer — for example certificate lifecycle state, order
                status, or vendor compliance. These are the gaps.
             4. Propose the smallest tool or resource that closes each gap, with its
                input schema.

             Invariant: {Invariant}
             """);
    }

    private static GetPromptResult Result(string description, string body) => new()
    {
        Description = description,
        Messages =
        [
            new PromptMessage
            {
                Role = "user",
                Content = ContentItem.FromText(body),
            },
        ],
    };

    private static string Lookup(IReadOnlyDictionary<string, string> args, string key, string fallback) =>
        args.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
}
