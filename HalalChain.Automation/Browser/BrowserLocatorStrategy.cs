namespace HalalChain.Automation.Browser;

/// <summary>
/// How a node locates an element on a page.
///
/// This is the point of the locator model: a workflow definition must not say
/// <c>By.Id("submit")</c> (Selenium) or <c>page.Locator("#submit")</c>
/// (Playwright). It says strategy + value, and each adapter translates it.
/// That translation is the only place a vendor selector appears.
///
/// Order matters: the resilient strategies come first because they are what
/// authors should reach for. Css and XPath are supported — pages that expose no
/// semantics leave no choice — but they are last, and the designer should
/// present them as a fallback rather than a default.
/// </summary>
public enum BrowserLocatorStrategy
{
    /// <summary>ARIA role, optionally with an accessible name. Preferred.</summary>
    Role = 0,

    /// <summary>Test id (<c>data-testid</c>). Preferred — cheap and stable.</summary>
    TestId = 1,

    /// <summary>Visible label bound to the control.</summary>
    Label = 2,

    /// <summary>Placeholder attribute of an input.</summary>
    Placeholder = 3,

    /// <summary>Visible text. Intentionally excluded for dynamic locales.</summary>
    Text = 4,

    /// <summary>Accessible name of a button/link.</summary>
    LinkText = 5,

    /// <summary>Form <c>name</c> attribute. Legacy, still common on portals.</summary>
    Name = 6,

    /// <summary>DOM id. Fast, brittle when the page restructures.</summary>
    Id = 7,

    /// <summary>CSS selector. Brittle — <c>div:nth-child(4) &gt; button</c> is a defect.</summary>
    Css = 8,

    /// <summary>XPath. Most brittle and slowest; last resort.</summary>
    XPath = 9,
}
