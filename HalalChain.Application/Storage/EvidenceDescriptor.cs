namespace HalalChain.Application.Storage;

public sealed record EvidenceDescriptor(
    string ActorId,
    EvidenceKind Kind,
    string? SourceUri,
    string? OriginalFilename,
    IReadOnlyDictionary<string, string> Tags);

public enum EvidenceKind
{
    Certificate,
    LabReport,
    Invoice,
    SupplierAudit,
    ProductLabel,
    Complaint,
    AgentTrace,
    Other,
}
