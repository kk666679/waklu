using HalalChain.Mcp.Tools;

namespace HalalChain.Mcp.Services;

internal sealed class ToolRegistry : Abstractions.IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<ITool> All => _tools.Values.ToList().AsReadOnly();

    public bool TryGet(string name, out ITool tool)
    {
        var result = _tools.TryGetValue(name, out var found);
        tool = found!;
        return result;
    }
}
