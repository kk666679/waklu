namespace HalalChain.Automation.Browser.Playwright;

/// <summary>
/// Pure translation from the neutral locator model to Playwright's locator
/// vocabulary (§9).
///
/// Kept as a standalone type — separate from the session and free of any
/// Playwright type — precisely so it can be unit-tested without launching a
/// browser. The session then only has to switch on the spec, which is the part
/// that genuinely needs a live page.
/// </summary>
internal static class PlaywrightLocatorTranslator
{
    public static PlaywrightLocatorSpec Translate(BrowserLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        if (string.IsNullOrWhiteSpace(locator.Value) && locator.Strategy != BrowserLocatorStrategy.Role)
            throw new ArgumentException(
                $"Locator value is empty for strategy {locator.Strategy}.", nameof(locator));

        var kind = locator.Strategy switch
        {
            BrowserLocatorStrategy.Role => PlaywrightLocatorKind.Role,
            BrowserLocatorStrategy.TestId => PlaywrightLocatorKind.TestId,
            BrowserLocatorStrategy.Label => PlaywrightLocatorKind.Label,
            BrowserLocatorStrategy.Placeholder => PlaywrightLocatorKind.Placeholder,
            BrowserLocatorStrategy.Text => PlaywrightLocatorKind.Text,
            BrowserLocatorStrategy.LinkText => PlaywrightLocatorKind.TextSelector,
            BrowserLocatorStrategy.Name => PlaywrightLocatorKind.AttributeName,
            BrowserLocatorStrategy.Id => PlaywrightLocatorKind.AttributeId,
            BrowserLocatorStrategy.Css => PlaywrightLocatorKind.Css,
            BrowserLocatorStrategy.XPath => PlaywrightLocatorKind.XPath,
            _ => throw new ArgumentOutOfRangeException(
                nameof(locator), locator.Strategy, "Unsupported locator strategy."),
        };

        return new PlaywrightLocatorSpec(kind, locator.Value, locator.Name, locator.Index);
    }
}

internal enum PlaywrightLocatorKind
{
    /// <summary><c>page.GetByRole(AriaRole, Name)</c> — the resilient default.</summary>
    Role,
    TestId,
    Label,
    Placeholder,
    Text,
    TextSelector,
    AttributeName,
    AttributeId,
    Css,
    XPath,
}

internal readonly record struct PlaywrightLocatorSpec(
    PlaywrightLocatorKind Kind,
    string Value,
    string? Name,
    int? Index);
