using HalalChain.Mcp.Tools;

namespace HalalChain.Mcp.Abstractions;

public interface IToolRegistry
{
    IReadOnlyCollection<ITool> All { get; }
    bool TryGet(string name, out ITool tool);
}
