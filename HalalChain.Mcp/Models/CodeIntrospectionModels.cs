namespace HalalChain.Mcp.Models;

// ──────────────────────────────────────────────────────────────────────────
// Solution-introspection models (Wave 1).
//
// These describe *structure discovered in the tree*, never compliance state.
// Nothing here can express a halal verdict, which is the point: the tool
// surface cannot carry one even by accident.
// ──────────────────────────────────────────────────────────────────────────

/// <summary>An HTTP endpoint: class-level route composed with the action route.</summary>
public class EndpointInfo
{
    public string Verb { get; set; } = "";
    public string Path { get; set; } = "";
    public string Handler { get; set; } = "";
    public string Controller { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>A MediatR request handler (<c>IRequestHandler&lt;TRequest, TResponse&gt;</c>).</summary>
public class HandlerInfo
{
    public string Kind { get; set; } = "request";
    public string Handler { get; set; } = "";
    public string Request { get; set; } = "";
    public string Response { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>
/// An event-shaped type: a MediatR notification, a <c>IDomainEvent</c>
/// implementation, or a type named <c>*Event</c> published through the API's
/// event bus.
/// </summary>
public class DomainEventInfo
{
    public string Name { get; set; } = "";
    public string Contract { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>A named type and where it lives. Used for aggregate roots and value objects.</summary>
public class TypeLocationInfo
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public int Line { get; set; }
}

/// <summary>An EF Core migration, ordered by the timestamp in its file name.</summary>
public class MigrationInfo
{
    public string Timestamp { get; set; } = "";
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ProjectName { get; set; } = "";
}

/// <summary>Per-project test inventory derived from <c>[Fact]</c>/<c>[Theory]</c>.</summary>
public class TestProjectInfo
{
    public string ProjectName { get; set; } = "";
    public int Facts { get; set; }
    public int Theories { get; set; }
    public int InlineDataCases { get; set; }
    public int Files { get; set; }

    /// <summary>Declared test methods. A theory counts as one method, not one case.</summary>
    public int TestMethods => Facts + Theories;
}
