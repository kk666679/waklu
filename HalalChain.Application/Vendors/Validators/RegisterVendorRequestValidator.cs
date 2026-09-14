using FluentValidation;
using HalalChain.Platform.Contracts.Vendors.Requests;

namespace HalalChain.Application.Vendors.Validators;

public class RegisterVendorRequestValidator : AbstractValidator<RegisterVendorRequest>
{
    public RegisterVendorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vendor name is required.")
            .MaximumLength(200).WithMessage("Vendor name must not exceed 200 characters.");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Country));
    }
}
