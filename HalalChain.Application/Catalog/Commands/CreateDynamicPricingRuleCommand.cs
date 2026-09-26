using MediatR;

namespace HalalChain.Application.Catalog.Commands;

/// <summary>
/// Command to create a new dynamic pricing rule.
/// </summary>
public record CreateDynamicPricingRuleCommand : IRequest<Guid>
{
    public Guid VendorId { get; set; }
    public List<Guid> ProductIds { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = "Promotion"; // Promotion, TieredDiscount, Flash, ABTest, Competitive
    public string? Conditions { get; set; }
    public decimal PriceAdjustment { get; set; }
    public string AdjustmentType { get; set; } = "Percentage"; // Percentage, Fixed, Multiplier
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? Priority { get; set; }
}
