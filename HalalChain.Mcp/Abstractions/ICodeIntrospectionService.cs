using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>
/// Structural introspection over the C# sources in the solution.
///
/// Every method is a pure read: it parses files under the resolved solution
/// root and returns what it found. None of them mutate anything, and none of
/// them interpret *meaning* — in particular none of them can produce, infer, or
/// override a compliance verdict. That property is what lets every tool built on
/// this interface advertise <c>readOnlyHint: true</c>.
///
/// Enumeration is best-effort and degrading: a missing root, an unreadable file,
/// or an unknown project filter yields an empty result rather than an exception,
/// so a wrong <c>HALALCHAIN_SOLUTION_ROOT</c> produces a clear empty answer
/// instead of a JSON-RPC internal error.
/// </summary>
public interface ICodeIntrospectionService
{
    /// <summary>HTTP endpoints discovered from <c>[HttpVerb]</c> attributes, with class routes composed in.</summary>
    IReadOnlyList<EndpointInfo> GetEndpoints(string? projectName = null);

    /// <summary>MediatR handlers (<c>IRequestHandler&lt;,&gt;</c> and <c>INotificationHandler&lt;&gt;</c>).</summary>
    IReadOnlyList<HandlerInfo> GetHandlers(string? projectName = null);

    /// <summary>Event-shaped types: notifications, domain-event implementations, and <c>*Event</c> types.</summary>
    IReadOnlyList<DomainEventInfo> GetDomainEvents(string? projectName = null);

    /// <summary>Aggregate roots, detected by the solution's <c>IAggregateRoot</c>/base-class conventions.</summary>
    IReadOnlyList<TypeLocationInfo> GetAggregateRoots(string? projectName = null);

    /// <summary>Value objects: records in a <c>ValueObjects</c> file or folder, or implementing <c>IValueObject</c>.</summary>
    IReadOnlyList<TypeLocationInfo> GetValueObjects(string? projectName = null);

    /// <summary>EF Core migrations, ordered by the timestamp embedded in the file name.</summary>
    IReadOnlyList<MigrationInfo> GetMigrations(string? projectName = null);

    /// <summary>Test inventory per test project.</summary>
    IReadOnlyList<TestProjectInfo> GetTestInventory(string? projectName = null);

    /// <summary>
    /// Locates type declarations (class, record, struct, interface, enum) by
    /// name. Exact-name matches are ordered before substring matches, so the
    /// declaration the caller asked for is always first when it exists.
    /// </summary>
    IReadOnlyList<TypeLocationInfo> FindType(string typeName, string? projectName = null);
}
