namespace HalalChain.Application.Halal.StateMachine;

using HalalChain.Domain.Catalog;

public sealed record StatusTransition(
    ProductStatus From,
    ProductStatus To,
    VerdictBinding Binding,
    DateTimeOffset EvaluatedAt)
{
    public bool IsNoOp => From == To;

    public static StatusTransition NoOp(ProductStatus status) =>
        new(status, status, VerdictBinding.Unbound, DateTimeOffset.MinValue);
}
