namespace HalalChain.Domain.Catalog;

/// <summary>
/// Dynamic pricing rule for promotions, tiered discounts, and A/B testing.
/// Allows vendors to create complex pricing strategies without coding.
/// </summary>
public sealed class DynamicPricingRule
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }

    /// <summary>
    /// Product IDs this rule applies to.
    /// </summary>
    public List<Guid> ProductIds { get; set; } = [];

    /// <summary>
    /// Rule name (e.g., "Summer Flash Sale").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rule type: Promotion, TieredDiscount, Flash, ABTest, Competitive.
    /// </summary>
    public string RuleType { get; set; } = "Promotion";

    /// <summary>
    /// Conditions as JSON (minQuantity, maxQuantity, dayOfWeek, timeWindow, etc.).
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Price adjustment value.
    /// </summary>
    public decimal PriceAdjustment { get; set; }

    /// <summary>
    /// Adjustment type: Percentage, Fixed, Multiplier.
    /// </summary>
    public string AdjustmentType { get; set; } = "Percentage";

    /// <summary>
    /// Rule start date.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Rule end date (optional for ongoing rules).
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Whether the rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for rule evaluation (higher priority wins).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Customer segments this rule applies to (wholesale, vip, new, etc.).
    /// </summary>
    public List<string>? CustomerSegments { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
