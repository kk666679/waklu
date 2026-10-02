namespace HalalChain.Automation.Browser;

using System.Text;

using HalalChain.Application.Storage;

/// <summary>One captured artefact pending persistence.</summary>
public sealed record BrowserEvidenceAttachment(
    string Name,
    string ContentType,
    byte[] Content,
    Uri? SourceUrl,
    EvidenceKind Kind = EvidenceKind.Other);

/// <summary>
/// Writes browser captures into the platform's existing evidence store (§22).
///
/// This is a port rather than a direct <see cref="IEvidenceStore"/> dependency
/// because adapters must not know about storage at all: a screenshot is a
/// byte[] until someone decides it needs to be retained. Keeping the boundary
/// here means an engine adapter can be unit-tested without a blob store.
///
/// The implementation composes <see cref="IEvidenceStore"/>, so captures get the
/// same append-only, SHA-256-addressed, access-logged, retention-managed
/// treatment as every other piece of evidence — no second storage path to get
/// wrong (ADR-011 D3).
/// </summary>
public interface IBrowserEvidenceWriter
{
    Task<string> WriteAsync(
        BrowserEvidenceAttachment attachment,
        AutomationExecutionContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Persists captures through <see cref="IEvidenceStore"/> with tenant and
/// execution provenance stamped into the tags.
/// </summary>
public sealed class EvidenceStoreBrowserEvidenceWriter : IBrowserEvidenceWriter
{
    private const string ContentTypeTag = "content-type";

    private readonly IEvidenceStore _evidence;

    public EvidenceStoreBrowserEvidenceWriter(IEvidenceStore evidence)
        => _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));

    public async Task<string> WriteAsync(
        BrowserEvidenceAttachment attachment,
        AutomationExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(context);

        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ContentTypeTag] = attachment.ContentType,
            ["execution-id"] = context.ExecutionId.ToString("D"),
            ["workflow-id"] = context.WorkflowId,
            ["workflow-version"] = context.WorkflowVersion.ToString(),
            ["tenant"] = context.TenantId,
            ["correlation-id"] = context.CorrelationId.ToString("D"),
            ["engine"] = context.Engine.ToString(),
            ["capture"] = attachment.Name,
        };

        if (attachment.SourceUrl is not null)
            tags["source-url"] = attachment.SourceUrl.ToString();

        var descriptor = new EvidenceDescriptor(
            ActorId: context.ActorId,
            Kind: attachment.Kind,
            SourceUri: attachment.SourceUrl?.ToString(),
            OriginalFilename: attachment.Name,
            Tags: tags);

        await using var stream = new MemoryStream(attachment.Content, writable: false);
        var record = await _evidence.IngestAsync(stream, descriptor, cancellationToken)
            .ConfigureAwait(false);

        return record.Id.ToString();
    }
}

/// <summary>
/// In-memory writer for tests and for runs that must not retain artefacts.
/// Captures are still attributed, but nothing is persisted.
/// </summary>
public sealed class VolatileBrowserEvidenceWriter : IBrowserEvidenceWriter
{
    private readonly List<(BrowserEvidenceAttachment Attachment, AutomationExecutionContext Context)> _written = [];

    public IReadOnlyList<(BrowserEvidenceAttachment Attachment, AutomationExecutionContext Context)> Written
        => _written.ToArray();

    public Task<string> WriteAsync(
        BrowserEvidenceAttachment attachment,
        AutomationExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _written.Add((attachment, context));
        return Task.FromResult($"volatile:{_written.Count}");
    }
}
