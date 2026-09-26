using FluentValidation;
using HalalChain.Application.Catalog.Commands;

namespace HalalChain.Application.Catalog.Validators;

public class BulkUpsertProductVariantsCommandValidator : AbstractValidator<BulkUpsertProductVariantsCommand>
{
    public BulkUpsertProductVariantsCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("VendorId is required.");

        RuleFor(x => x.Attributes)
            .NotEmpty().WithMessage("Attributes dictionary is required and must contain at least one attribute.")
            .Must(HaveValidAttributes).WithMessage("Each attribute must have at least one value.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("BasePrice must be greater than 0.")
            .LessThanOrEqualTo(999999.99m).WithMessage("BasePrice must not exceed 999,999.99.");

        RuleFor(x => x.BaseStock)
            .GreaterThanOrEqualTo(0).WithMessage("BaseStock cannot be negative.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("LowStockThreshold cannot be negative.")
            .LessThanOrEqualTo(x => x.BaseStock)
            .When(x => x.BaseStock > 0)
            .WithMessage("LowStockThreshold cannot exceed BaseStock.");

        // Validate total variant combinations don't exceed reasonable limit (e.g., 1000)
        RuleFor(x => x)
            .Must(NotExceedMaxVariants)
            .WithMessage("Attribute combinations would generate more than 1000 variants. Please reduce attribute values.");
    }

    private static bool HaveValidAttributes(Dictionary<string, List<string>> attributes)
    {
        return attributes.All(kvp => kvp.Value != null && kvp.Value.Count > 0);
    }

    private static bool NotExceedMaxVariants(BulkUpsertProductVariantsCommand cmd)
    {
        // Calculate Cartesian product size
        long variants = 1;
        foreach (var values in cmd.Attributes.Values)
        {
            variants *= values.Count;
            if (variants > 1000)
                return false;
        }
        return true;
    }
}
