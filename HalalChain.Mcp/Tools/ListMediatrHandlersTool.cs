using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: MediatR handlers.</summary>
public sealed class ListMediatrHandlersTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListMediatrHandlersTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_mediator_handlers";

    public string Description =>
        "Lists MediatR handlers (IRequestHandler<,> and INotificationHandler<>) with their request and response types.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List MediatR handlers");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var handlers = _introspection.GetHandlers(ToolArguments.String(arguments, "project"));
        var output = $"# MediatR Handlers ({handlers.Count})\n\n";

        if (handlers.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in handlers.GroupBy(h => h.Kind).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            output += $"## {group.Key} handlers ({group.Count()})\n\n";
            foreach (var handler in group)
            {
                var signature = group.Key == "request"
                    ? $"`{handler.Request}` → `{handler.Response}`"
                    : "`notification`";
                output += $"- **{handler.Handler}** {signature}\n" +
                          $"  `{handler.RelativePath}:{handler.Line}` ({handler.ProjectName})\n";
            }

            output += "\n";
        }

        output += "Detection: declarations whose base list names `IRequestHandler<` or `INotificationHandler<` — " +
                  "including primary-constructor handlers where the interface sits on the following line.\n";
        return Task.FromResult(output);
    }
}