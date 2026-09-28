namespace HalalChain.Domain.Catalog;

/// <summary>
/// A rule that adjusts a variant's price based on conditions. The rule is
/// data; evaluation happens in the Application layer.
/// </summary>
public sealed record DynamicPricingRule(
    Guid Id,
    ProductVariantId VariantId,
    string Condition,
    decimal AdjustmentPercent,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);
