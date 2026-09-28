namespace HalalChain.Application.Halal.Commands;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;
using MediatR;

public sealed record BindVerdictCommand(
    ProductId ProductId,
    VerdictState State,
    string PolicyVersion,
    string? CertificateNumber,
    DateTimeOffset? CertificateExpiresAt,
    string? TraceHash) : IRequest<BindVerdictResult>;

public sealed record BindVerdictResult(
    ProductId ProductId,
    VerdictState State,
    ProductStatus ProjectedStatus,
    DateTimeOffset BoundAt);
