using HalalChain.Mcp.Models;

namespace HalalChain.Mcp.Abstractions;

/// <summary>Source of the prompt templates advertised by <c>prompts/list</c>.</summary>
public interface IPromptRegistry
{
    IReadOnlyList<PromptDefinition> All { get; }

    /// <summary>True when <paramref name="name"/> is a known prompt.</summary>
    bool Contains(string name);

    /// <summary>
    /// Renders a prompt. Returns false for an unknown name, or when a
    /// required argument is missing.
    /// </summary>
    bool TryRender(
        string name,
        IReadOnlyDictionary<string, string> arguments,
        out GetPromptResult? result,
        out string? error);

    /// <summary>
    /// Candidate completions for one prompt argument, backing
    /// <c>completion/complete</c>. Returns an empty list for unknown prompts.
    /// </summary>
    IReadOnlyList<string> SuggestValues(string name, string argumentName, string prefix);
}
