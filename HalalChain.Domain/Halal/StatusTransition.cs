namespace HalalChain.Domain.Halal;

/// <summary>
/// The evaluated outcome of projecting a <see cref="VerdictBinding"/> onto a
/// product's <see cref="ProductStatus"/>.
///
/// Produced by <c>ProductStatusMachine.Evaluate</c> and applied by
/// <c>Product.ApplyTransition</c> — the only sanctioned path that mutates
/// <c>Product.Status</c>. It lives in the Domain, not the Application layer,
/// so the aggregate can enforce its own transition rule without the Domain
/// taking a dependency on Application (see HalalChain.Architecture.Tests).
/// </summary>
public sealed record StatusTransition(
    ProductStatus From,
    ProductStatus To,
    VerdictBinding Binding,
    DateTimeOffset EvaluatedAt)
{
    /// <summary>True when the evaluation produced no status change.</summary>
    public bool IsNoOp => From == To;

    /// <summary>
    /// A no-op transition that leaves the product status untouched. Carries an
    /// unbound verdict binding because no verdict drove the decision.
    /// </summary>
    public static StatusTransition NoOp(ProductStatus status) =>
        new(status, status, new VerdictBinding(), DateTimeOffset.MinValue);
}
