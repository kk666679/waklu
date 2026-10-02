namespace HalalChain.Automation.Browser;

using System.Text.Json.Serialization;

/// <summary>
/// A domain-level browser operation.
///
/// Deliberately neither Playwright nor Selenium. An action is the vocabulary
/// a workflow node speaks; <see cref="Playwright.PlaywrightBrowserSession"/> and
/// <see cref="Selenium.SeleniumBrowserSession"/> are two independent
/// translations of it. Adding an action therefore never touches the workflow
/// engine (ADR-010).
///
/// Two properties make this worth having as a type:
///   1. Immutable — an action list is a value that can be logged, replayed,
///      diffed, and serialised to the designer.
///   2. Polymorphic on <c>Kind</c> — workflow JSON round-trips through
///      System.Text.Json without a hand-maintained switch statement in five
///      places.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(OpenBrowserAction), "openBrowser")]
[JsonDerivedType(typeof(NavigateAction), "navigate")]
[JsonDerivedType(typeof(ClickAction), "click")]
[JsonDerivedType(typeof(FillAction), "fill")]
[JsonDerivedType(typeof(FillSecretAction), "fillSecret")]
[JsonDerivedType(typeof(SelectAction), "select")]
[JsonDerivedType(typeof(UncheckAction), "uncheck")]
[JsonDerivedType(typeof(WaitForElementAction), "waitForElement")]
[JsonDerivedType(typeof(WaitForNavigationAction), "waitForNavigation")]
[JsonDerivedType(typeof(ExtractTextAction), "extractText")]
[JsonDerivedType(typeof(ExtractAttributeAction), "extractAttribute")]
[JsonDerivedType(typeof(ScreenshotAction), "screenshot")]
[JsonDerivedType(typeof(SaveHtmlAction), "saveHtml")]
[JsonDerivedType(typeof(UploadFileAction), "uploadFile")]
[JsonDerivedType(typeof(RunBrowserAction), "runBrowserAction")]
[JsonDerivedType(typeof(CloseBrowserAction), "closeBrowser")]
public abstract record BrowserAction
{
    /// <summary>Short discriminator for logs, telemetry, and UI badges.</summary>
    public abstract string Kind { get; }

    /// <summary>
    /// Whether re-running this action after a partial failure is safe.
    ///
    /// Retries are never implicit (ADR-010/§27): a runner may only retry when
    /// it is explicitly allowed AND the action declares itself idempotent.
    /// Navigation and waiting are; submitting a form is not.
    /// </summary>
    public virtual bool IsIdempotent => false;
}

/// <summary>Starts an isolated browser context for one execution.</summary>
public sealed record OpenBrowserAction(
    BrowserEngineType Engine = BrowserEngineType.Default,
    bool? Headless = null) : BrowserAction
{
    public override string Kind => "openBrowser";
    public override bool IsIdempotent => true;
}

public sealed record NavigateAction(Uri Url) : BrowserAction
{
    public override string Kind => "navigate";

    /// <summary>Re-navigating to the same URL changes no state we did not already ask for.</summary>
    public override bool IsIdempotent => true;
}

public sealed record ClickAction(BrowserLocator Locator) : BrowserAction
{
    public override string Kind => "click";
}

/// <summary>
/// Types a plain value into a field. Not for secrets.
/// </summary>
/// <remarks>
/// The split between <see cref="FillAction"/> and <see cref="FillSecretAction"/>
/// is what makes ADR-011 D2 a type-level guarantee rather than a code-review
/// norm: a password cannot be attached to a <c>fill</c>, because that record
/// has nowhere to put one. (The same trick the storage port uses — no
/// <c>Delete</c> member — applied to credentials.)
/// </remarks>
public sealed record FillAction(BrowserLocator Locator, string Value) : BrowserAction
{
    public override string Kind => "fill";
}

/// <summary>
/// Types a secret into a field, resolved server-side from a credential
/// reference. The workflow carries an id; the plaintext never leaves the
/// executing process (ADR-011 D2).
/// </summary>
public sealed record FillSecretAction(BrowserLocator Locator, string CredentialId) : BrowserAction
{
    public override string Kind => "fillSecret";
}

public sealed record SelectAction(BrowserLocator Locator, string Value) : BrowserAction
{
    public override string Kind => "select";
}

public sealed record UncheckAction(BrowserLocator Locator) : BrowserAction
{
    public override string Kind => "uncheck";
}

/// <summary>
/// Semantic wait. The engine-independent replacement for <c>Task.Delay(5000)</c>:
/// the workflow says what must become true, Playwright resolves it via a
/// locator/URL condition and Selenium via an explicit wait.
/// </summary>
public sealed record WaitForElementAction(
    BrowserLocator Locator,
    BrowserWaitCondition Condition = BrowserWaitCondition.Visible,
    TimeSpan? Timeout = null) : BrowserAction
{
    public override string Kind => "waitForElement";
    public override bool IsIdempotent => true;
}

/// <summary>
/// Waits for a navigation to complete — i.e. for a pending click to have
/// landed — without knowing how the page signals it.
/// </summary>
public sealed record WaitForNavigationAction(
    Uri? Url = null,
    TimeSpan? Timeout = null) : BrowserAction
{
    public override string Kind => "waitForNavigation";
    public override bool IsIdempotent => true;
}

public sealed record ExtractTextAction(BrowserLocator Locator, string OutputKey) : BrowserAction
{
    public override string Kind => "extractText";
    public override bool IsIdempotent => true;
}

public sealed record ExtractAttributeAction(
    BrowserLocator Locator,
    string Attribute,
    string OutputKey) : BrowserAction
{
    public override string Kind => "extractAttribute";
    public override bool IsIdempotent => true;
}

/// <summary>
/// Captures a screenshot and persists it as evidence. Not a redaction
/// boundary — callers must not capture pages containing secrets they do not
/// intend to retain (ADR-011 D3).
/// </summary>
public sealed record ScreenshotAction(string Name) : BrowserAction
{
    public override string Kind => "screenshot";
    public override bool IsIdempotent => true;
}

public sealed record SaveHtmlAction(string Name) : BrowserAction
{
    public override string Kind => "saveHtml";
    public override bool IsIdempotent => true;
}

/// <summary>
/// Uploads a file through a file input. <paramref name="ContentDisposition"/>
/// is an opaque server-side handle — the browser never receives a path,
/// because a path would be a traversal primitive on the host (ADR-011 D5).
/// </summary>
public sealed record UploadFileAction(BrowserLocator Locator, string ContentDisposition) : BrowserAction
{
    public override string Kind => "uploadFile";
}

/// <summary>
/// Groups a nested action list executed as one unit against the current
/// session — the "run browser action" composite node in the designer.
/// </summary>
public sealed record RunBrowserAction(IReadOnlyList<BrowserAction> Actions) : BrowserAction
{
    public override string Kind => "runBrowserAction";
}

public sealed record CloseBrowserAction : BrowserAction
{
    public override string Kind => "closeBrowser";
    public override bool IsIdempotent => true;
}

/// <summary>What a wait is waiting for. Engine-specific syntax stays out of it.</summary>
public enum BrowserWaitCondition
{
    Visible,
    Hidden,
    Enabled,
    Attached,
    Url,
}
