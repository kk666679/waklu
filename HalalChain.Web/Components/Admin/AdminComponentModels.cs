namespace HalalChain.Components.Admin;

public sealed record ArchitectureNode(string Name, double X, double Y);

public sealed class ArchitectureDiagramData
{
    public IReadOnlyList<ArchitectureNode> Nodes { get; init; } = Array.Empty<ArchitectureNode>();
}

public sealed record VerificationStep(int Order, string Title, string Description);
