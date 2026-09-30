namespace HalalChain.Application.Halal.StateMachine;

using HalalChain.Domain.Catalog;
using HalalChain.Domain.Halal;

/// <summary>
/// The single source of truth for ProductStatus transitions.
///
/// Only this state machine may change ProductStatus. Any other code path
/// assigning to Product.Status is an architecture violation, enforced by
/// HalalChain.Architecture.Tests.
///
/// Verdicts do not live here. The state machine consumes a VerdictBinding
/// issued by tawheed and projects it onto the product's status. It never
/// inspects evidence, never evaluates policy, and never decides.
/// </summary>
public sealed class ProductStatusMachine
{
    private static readonly Dictionary<ProductStatus, ProductStatus[]> Allowed = new()
    {
        [ProductStatus.Draft]               = [ProductStatus.Submitted, ProductStatus.Archived],
        [ProductStatus.Submitted]           = [ProductStatus.Approved, ProductStatus.Rejected],
        [ProductStatus.Rejected]            = [ProductStatus.Draft, ProductStatus.Archived],
        [ProductStatus.Approved]            = [ProductStatus.Published, ProductStatus.Suspended, ProductStatus.Archived],
        [ProductStatus.Published]           = [ProductStatus.Suspended, ProductStatus.Archived],
        [ProductStatus.Suspended]           = [ProductStatus.Submitted, ProductStatus.Archived],
        [ProductStatus.Archived]            = [],
    };

    public StatusTransition Evaluate(
        Product product,
        VerdictBinding binding,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(binding);

        var target = binding.State switch
        {
            VerdictState.Verified when binding.ExpiresAt <= now
                => ProductStatus.Suspended,
            VerdictState.Verified when binding.ExpiresAt - now <= TimeSpan.FromDays(30)
                => ProductStatus.Published,
            VerdictState.Verified
                => ProductStatus.Approved,
            VerdictState.NonCompliant
                => ProductStatus.Rejected,
            VerdictState.ManualReview
                => ProductStatus.UnderReview,
            _ => product.Status,
        };

        if (target == product.Status)
            return StatusTransition.NoOp(product.Status);

        if (!Allowed.TryGetValue(product.Status, out var allowed) || !allowed.Contains(target))
            throw new InvalidOperationException(
                $"Illegal ProductStatus transition: {product.Status} -> {target}.");

        return new StatusTransition(product.Status, target, binding, now);
    }
}
