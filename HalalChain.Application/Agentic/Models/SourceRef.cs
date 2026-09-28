namespace HalalChain.Application.Agentic.Models;

public sealed record SourceRef(
    string Kind,
    string Reference,
    string? RetrievedAt,
    string? Note);
