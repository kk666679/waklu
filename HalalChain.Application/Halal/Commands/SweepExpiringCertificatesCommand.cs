namespace HalalChain.Application.Halal.Commands;

using MediatR;

public sealed record SweepExpiringCertificatesCommand(
    int LookaheadDays = 30) : IRequest<SweepExpiringCertificatesResult>;

public sealed record SweepExpiringCertificatesResult(
    int Evaluated,
    int Activated,
    int ExpiringSoon,
    int Suspended,
    int Skipped,
    DateTimeOffset CompletedAt);
