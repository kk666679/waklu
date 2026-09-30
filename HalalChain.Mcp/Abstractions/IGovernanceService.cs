using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>
/// Read-only introspection over the repository's governance surface: the
/// principles, the decision records, the published skills, the service
/// manifest, and the governance workflows.
///
/// This interface exists so a host can *see* the rules it is bound by. It
/// cannot change them: there is no write member, and no member that evaluates
/// compliance. That asymmetry is deliberate — an agent may read the policy, and
/// only the deterministic Policy Engine in <c>tawheed</c> may apply it.
/// </summary>
public interface IGovernanceService
{
    /// <summary>Parses the numbered principles from the first principles document found.</summary>
    PrinciplesDocument GetPrinciples();

    /// <summary>Lists Architecture Decision Records.</summary>
    AdrIndex ListAdrs();

    /// <summary>Reads one ADR by id (e.g. <c>0001</c>).</summary>
    DocumentLookup GetAdr(string id);

    /// <summary>Lists published skills from both supported conventions.</summary>
    SkillIndex ListSkills();

    /// <summary>Reads one skill's manifest or steering document by slug.</summary>
    DocumentLookup GetSkill(string slug);

    /// <summary>Reads the controls manifest, when the repository has one.</summary>
    DocumentLookup GetControls();

    /// <summary>Reads <c>service-manifest.yaml</c>.</summary>
    DocumentLookup GetServiceManifest();

    /// <summary>
    /// Scans the C# sources for names that would grant a compliance-verdict
    /// authority to something other than the deterministic Policy Engine.
    /// </summary>
    VerdictAuthorityReport VerifyNoVerdictAuthority();

    /// <summary>Lists state machines and governance workflow definitions.</summary>
    IReadOnlyList<GovernanceWorkflowInfo> GetGovernanceWorkflows(string? projectName = null);
}
