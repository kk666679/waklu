namespace HalalChain.Automation.Browser.Selenium;

/// <summary>
/// Pure translation from the neutral locator model to Selenium's <see cref="By"/>
/// vocabulary (§9).
///
/// Selenium has no first-class role or label locator, so the resilient
/// strategies are emulated with CSS/XPath. That emulation is the entire cost of
/// supporting Selenium here, and it is why the neutral model exists: the
/// emulation lives in one 40-line class instead of being scattered across
/// workflow nodes.
///
/// Like its Playwright counterpart, this type touches no driver, so it is unit
/// testable without a browser process.
/// </summary>
internal static class SeleniumLocatorTranslator
{
    public static SeleniumLocatorSpec Translate(BrowserLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        if (string.IsNullOrWhiteSpace(locator.Value) && locator.Strategy != BrowserLocatorStrategy.Role)
            throw new ArgumentException(
                $"Locator value is empty for strategy {locator.Strategy}.", nameof(locator));

        return locator.Strategy switch
        {
            // Playwright's role+name; emulated here as an ARIA attribute pair.
            BrowserLocatorStrategy.Role when !string.IsNullOrWhiteSpace(locator.Name)
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Css,
                    $"[role='{EscapeCss(locator.Value)}'][aria-label='{EscapeCss(locator.Name!)}']",
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Role
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Css,
                    $"[role='{EscapeCss(locator.Value)}']",
                    null,
                    locator.Index),

            BrowserLocatorStrategy.TestId
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Css,
                    $"[data-testid='{EscapeCss(locator.Value)}']",
                    null,
                    locator.Index),

            // No By.Label in Selenium; label text → the control it points at.
            BrowserLocatorStrategy.Label
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.XPath,
                    $"//label[normalize-space()='{EscapeXPath(locator.Value)}']" +
                    "/following::input[1] | //label[normalize-space()='" +
                    EscapeXPath(locator.Value) + "']/following::select[1]",
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Placeholder
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Css,
                    $"[placeholder='{EscapeCss(locator.Value)}']",
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Text
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.XPath,
                    $"//*[normalize-space()='{EscapeXPath(locator.Value)}']",
                    null,
                    locator.Index),

            BrowserLocatorStrategy.LinkText
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.LinkText,
                    locator.Value,
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Name
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Name,
                    locator.Value,
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Id
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Id,
                    locator.Value,
                    null,
                    locator.Index),

            BrowserLocatorStrategy.Css
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.Css,
                    locator.Value,
                    null,
                    locator.Index),

            BrowserLocatorStrategy.XPath
                => new SeleniumLocatorSpec(
                    SeleniumLocatorKind.XPath,
                    locator.Value,
                    null,
                    locator.Index),

            _ => throw new ArgumentOutOfRangeException(
                nameof(locator), locator.Strategy, "Unsupported locator strategy."),
        };
    }

    /// <summary>
    /// Single quotes inside an XPath string literal would terminate it, so they
    /// are broken out via concat(). This is the standard XPath-1.0 escaping and
    /// the reason it must never be done with plain string concatenation.
    /// </summary>
    internal static string EscapeXPath(string value)
        => value.Contains('\'')
            ? throw new ArgumentException(
                "XPath literals containing a single quote are not supported by this translator.",
                nameof(value))
            : value;

    internal static string EscapeCss(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal);
}

internal enum SeleniumLocatorKind
{
    Id,
    Name,
    Css,
    XPath,
    LinkText,
    PartialLinkText,
}

internal readonly record struct SeleniumLocatorSpec(
    SeleniumLocatorKind Kind,
    string Value,
    string? Name,
    int? Index);
