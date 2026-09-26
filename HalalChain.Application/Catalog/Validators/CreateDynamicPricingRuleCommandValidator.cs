using FluentValidation;
using HalalChain.Application.Catalog.Commands;

namespace HalalChain.Application.Catalog.Validators;

public class CreateDynamicPricingRuleCommandValidator : AbstractValidator<CreateDynamicPricingRuleCommand>
{
    public CreateDynamicPricingRuleCommandValidator()
    {
        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("VendorId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.RuleType)
            .NotEmpty().WithMessage("RuleType is required.")
            .Must(BeValidRuleType).WithMessage("RuleType must be one of: Promotion, TieredDiscount, Flash, ABTest, Competitive.");

        RuleFor(x => x.Conditions)
            .MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Conditions))
            .WithMessage("Conditions must not exceed 2000 characters.");

        RuleFor(x => x.PriceAdjustment)
            .NotEmpty().WithMessage("PriceAdjustment is required.");

        RuleFor(x => x.AdjustmentType)
            .NotEmpty().WithMessage("AdjustmentType is required.")
            .Must(BeValidAdjustmentType).WithMessage("AdjustmentType must be one of: Percentage, Fixed, Multiplier.");

        RuleFor(x => x)
            .Must(HaveValidAdjustmentValue).WithMessage("PriceAdjustment must be positive for Percentage and Multiplier types.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).When(x => x.Priority.HasValue)
            .WithMessage("Priority must be non-negative.");

        RuleFor(x => x.ProductIds)
            .Must(NotHaveDuplicates).WithMessage("ProductIds must not contain duplicates.");
    }

    private static bool BeValidRuleType(string ruleType)
    {
        var validTypes = new[] { "Promotion", "TieredDiscount", "Flash", "ABTest", "Competitive" };
        return validTypes.Contains(ruleType, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidAdjustmentType(string adjustmentType)
    {
        var validTypes = new[] { "Percentage", "Fixed", "Multiplier" };
        return validTypes.Contains(adjustmentType, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HaveValidAdjustmentValue(CreateDynamicPricingRuleCommand cmd)
    {
        // For Percentage and Multiplier, adjustment should be positive
        if (cmd.AdjustmentType == "Percentage" || cmd.AdjustmentType == "Multiplier")
        {
            return cmd.PriceAdjustment > 0;
        }

        // Fixed can be positive or negative (increase or discount)
        return true;
    }

    private static bool NotHaveDuplicates(List<Guid> productIds)
    {
        return productIds.Count == productIds.Distinct().Count();
    }
}
