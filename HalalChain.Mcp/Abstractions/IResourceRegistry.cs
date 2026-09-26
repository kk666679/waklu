using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>
/// Source of the resources advertised by <c>resources/list</c> and served by
/// <c>resources/read</c>.
/// </summary>
public interface IResourceRegistry
{
    /// <summary>All resources this server will serve, in stable order.</summary>
    IReadOnlyList<ResourceDefinition> All { get; }

    /// <summary>True when <paramref name="uri"/> is servable.</summary>
    bool Contains(string uri);

    /// <summary>
    /// Reads a resource. Returns false for an unknown URI, a path that escapes
    /// the solution root, or a file above the configured size ceiling.
    /// </summary>
    bool TryRead(string uri, out ResourceContents? contents, out string? error);
}
