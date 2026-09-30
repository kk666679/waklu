using System.Text.Json;
using HalalChain.Mcp.Abstractions;
using HalalChain.Mcp.Configuration;
using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Tools;

/// <summary>Read-only Wave-1 tool: event-shaped types.</summary>
public sealed class ListDomainEventsTool : ITool
{
    private readonly ICodeIntrospectionService _introspection;

    public ListDomainEventsTool(ICodeIntrospectionService introspection)
    {
        _introspection = introspection;
    }

    public string Name => "halalchain_list_domain_events";

    public string Description =>
        "Lists event-shaped types: MediatR notifications, IDomainEvent implementations, and *Event payloads published through the event bus.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            project = new { type = "string", description = "Optional owning project name" }
        },
        required = Array.Empty<string>()
    };

    public ToolAnnotations Annotations => ToolAnnotations.LocalReadOnly("List domain events");

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var events = _introspection.GetDomainEvents(ToolArguments.String(arguments, "project"));
        var output = $"# Domain Events ({events.Count})\n\n";

        if (events.Count == 0)
        {
            return Task.FromResult(
                output +
                $"Nothing found. The scan walks the solution root — check `{HalalChainOptions.GetSolutionRootEnvVar()}` " +
                "if this should be non-empty.\n");
        }

        foreach (var group in events.GroupBy(e => e.ProjectName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            output += $"## {group.Key}\n\n";
            foreach (var domainEvent in group)
            {
                output += $"- **{domainEvent.Name}** — {domainEvent.Contract}\n" +
                          $"  `{domainEvent.RelativePath}:{domainEvent.Line}`\n";
            }

            output += "\n";
        }

        output += "Detection: a declaration counts when its base list names `IDomainEvent` or `INotification`, " +
                  "or when its name ends in `Event`. Events are *listed*, never raised — publishing stays a code-level action.\n";
        return Task.FromResult(output);
    }
}