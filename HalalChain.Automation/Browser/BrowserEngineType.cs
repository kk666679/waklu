namespace HalalChain.Automation.Browser;

/// <summary>
/// Which browser stack executes an action.
///
/// <see cref="Default"/> is not a third stack — it is the operator's configured
/// preference (ADR-010 D2). Keeping it as a distinct value means a workflow can
/// say "don't care" and still let a deployment pin a specific engine, without
/// the workflow author having to know which engine that deployment chose.
/// </summary>
public enum BrowserEngineType
{
    /// <summary>Use <c>Automation:Browser:DefaultEngine</c>. Never hard-coded.</summary>
    Default = 0,

    Playwright = 1,

    Selenium = 2,
}
