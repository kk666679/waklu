using System.Text.Json;

namespace HalalChain.Mcp.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }
    Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default);
}
