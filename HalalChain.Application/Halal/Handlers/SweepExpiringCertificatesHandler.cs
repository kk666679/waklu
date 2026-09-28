namespace HalalChain.Application.Halal.Handlers;

using HalalChain.Application.Common.Abstractions;
using HalalChain.Application.Halal.Commands;
using HalalChain.Application.Halal.Interfaces;
using HalalChain.Application.Halal.StateMachine;
using HalalChain.Domain.Catalog;
using MediatR;

/// <summary>
/// Deterministic. Reads certificates approaching or past expiry, evaluates
/// the state machine, and emits status transitions. No LLM, no agent, no
/// network calls except to the certificate repository.
///
/// This handler is what makes "certification is a snapshot, not a state" false.
/// It delists a product the day its certificate lapses, without human action.
/// </summary>
public sealed class SweepExpiringCertificatesHandler
    : IRequestHandler<SweepExpiringCertificatesCommand, SweepExpiringCertificatesResult>
{
    private readonly ICertificateRepository _certificates;
    private readonly IVerdictBindingRepository _bindings;
    private readonly ProductStatusMachine _machine;
    private readonly IClock _clock;

    public SweepExpiringCertificatesHandler(
        ICertificateRepository certificates,
        IVerdictBindingRepository bindings,
        ProductStatusMachine machine,
        IClock clock)
    {
        _certificates = certificates;
        _bindings = bindings;
        _machine = machine;
        _clock = clock;
    }

    public async Task<SweepExpiringCertificatesResult> Handle(
        SweepExpiringCertificatesCommand request,
        CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var horizon = now.AddDays(request.LookaheadDays);

        var candidates = await _certificates.QueryExpiringAsync(now, horizon, ct);

        int activated = 0, expiringSoon = 0, suspended = 0, skipped = 0;

        foreach (var certificate in candidates)
        {
            var bindings = await _bindings.QueryByCertificateAsync(certificate.Id, ct);

            foreach (var pair in bindings)
            {
                var transition = _machine.Evaluate(pair.Product, pair.Binding, now);

                if (transition.IsNoOp)
                {
                    skipped++;
                    continue;
                }

                pair.Product.ApplyTransition(transition);

                switch (transition.To)
                {
                    case ProductStatus.Active:       activated++;    break;
                    case ProductStatus.ExpiringSoon: expiringSoon++; break;
                    case ProductStatus.Suspended:    suspended++;    break;
                }
            }
        }

        return new SweepExpiringCertificatesResult(
            Evaluated: candidates.Count,
            Activated: activated,
            ExpiringSoon: expiringSoon,
            Suspended: suspended,
            Skipped: skipped,
            CompletedAt: now);
    }
}
