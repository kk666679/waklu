namespace HalalChain.Automation.Browser;

/// <summary>
/// A normalized element reference.
///
/// Workflow definitions carry this record, never <c>By.Id(...)</c> or
/// <c>page.Locator(...)</c>. Each engine adapter translates it, so the same
/// action list runs on Playwright or Selenium unchanged (ADR-010).
///
/// Strategies are ordered resilient-first: Role/TestId/Label/Placeholder are
/// what authors should use, Css/XPath are the fallback for pages that expose
/// no semantics. <see cref="Index"/> disambiguates an otherwise
/// non-unique match (the "nth of these" case) rather than forcing authors into
/// a fragile positional XPath.
/// </summary>
public sealed record BrowserLocator
{
    public required BrowserLocatorStrategy Strategy { get; init; }

    public required string Value { get; init; }

    /// <summary>Role name for <see cref="BrowserLocatorStrategy.Role"/>, e.g. "Submit".</summary>
    public string? Name { get; init; }

    /// <summary>Optional 0-based index for strategies that can match multiple nodes.</summary>
    public int? Index { get; init; }

    public static BrowserLocator Role(string role, string? name = null, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Role, Value = role, Name = name, Index = index };

    public static BrowserLocator TestId(string testId, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.TestId, Value = testId, Index = index };

    public static BrowserLocator Label(string label, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Label, Value = label, Index = index };

    public static BrowserLocator Placeholder(string placeholder, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Placeholder, Value = placeholder, Index = index };

    public static BrowserLocator Text(string text, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Text, Value = text, Index = index };

    public static BrowserLocator LinkText(string text, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.LinkText, Value = text, Index = index };

    public static BrowserLocator Named(string name, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Name, Value = name, Index = index };

    public static BrowserLocator Id(string id, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Id, Value = id, Index = index };

    public static BrowserLocator Css(string selector, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.Css, Value = selector, Index = index };

    public static BrowserLocator XPath(string expression, int? index = null)
        => new() { Strategy = BrowserLocatorStrategy.XPath, Value = expression, Index = index };

    public override string ToString()
    {
        var label = Name is null ? $"{Strategy}:{Value}" : $"{Strategy}:{Value} ({Name})";
        return Index is null ? label : $"{label}[{Index}]";
    }
}
