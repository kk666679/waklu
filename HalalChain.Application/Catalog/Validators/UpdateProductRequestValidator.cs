using FluentValidation;
using HalalChain.Platform.Contracts.Catalog.Requests;

namespace HalalChain.Application.Catalog.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price must not exceed 999,999.99.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Inventory)
            .GreaterThanOrEqualTo(0).WithMessage("Inventory cannot be negative.")
            .When(x => x.Inventory.HasValue);
    }
}
