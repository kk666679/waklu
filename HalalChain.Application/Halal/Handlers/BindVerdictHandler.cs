namespace HalalChain.Application.Halal.Handlers;

using HalalChain.Application.Common.Abstractions;
using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Halal.Commands;
using HalalChain.Application.Halal.Interfaces;
using HalalChain.Application.Halal.StateMachine;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;
using MediatR;

/// <summary>
/// Binds a tawheed verdict to a product and projects the resulting status.
/// The verdict is authoritative and is never recomputed here.
/// </summary>
public sealed class BindVerdictHandler
    : IRequestHandler<BindVerdictCommand, BindVerdictResult>
{
    private readonly IVerdictBindingRepository _bindings;
    private readonly IProductRepository _products;
    private readonly ProductStatusMachine _machine;
    private readonly IClock _clock;

    public BindVerdictHandler(
        IVerdictBindingRepository bindings,
        IProductRepository products,
        ProductStatusMachine machine,
        IClock clock)
    {
        _bindings = bindings;
        _products = products;
        _machine = machine;
        _clock = clock;
    }

    public async Task<BindVerdictResult> Handle(
        BindVerdictCommand request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(request.ProductId.Value, out var productGuid))
            throw new ArgumentException(
                $"'{request.ProductId}' is not a valid product identifier.",
                nameof(request));

        var product = await _products.GetAsync(productGuid, ct)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found.");

        var binding = VerdictBinding.Issue(
            productId: request.ProductId,
            certificateId: new CertificateId(request.CertificateNumber ?? string.Empty),
            state: request.State,
            policyVersion: request.PolicyVersion,
            expiresAt: request.CertificateExpiresAt ?? DateTimeOffset.MaxValue,
            boundAt: _clock.UtcNow);

        await _bindings.UpsertAsync(binding, ct);

        var transition = _machine.Evaluate(product, binding, _clock.UtcNow);
        if (!transition.IsNoOp)
            product.ApplyTransition(transition);

        return new BindVerdictResult(
            ProductId: request.ProductId,
            State: request.State,
            ProjectedStatus: product.Status,
            BoundAt: _clock.UtcNow);
    }
}
