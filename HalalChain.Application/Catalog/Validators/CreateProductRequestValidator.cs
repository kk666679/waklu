using FluentValidation;
using HalalChain.Platform.Contracts.Catalog.Requests;

namespace HalalChain.Application.Catalog.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price must not exceed 999,999.99.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.Inventory)
            .GreaterThanOrEqualTo(0).WithMessage("Inventory cannot be negative.");

        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Origin is required.")
            .MaximumLength(100).WithMessage("Origin must not exceed 100 characters.");
    }
}
