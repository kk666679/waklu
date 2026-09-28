namespace HalalChain.Application.Tawheed.Models;

public sealed record PolicyEvaluationRequest
{
    public required Guid VendorId { get; init; }
    public Guid? ProductId { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, object>> Evidence { get; init; }
    public string? PolicyVersion { get; init; }
}
