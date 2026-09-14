using FluentValidation;
using HalalChain.Platform.Contracts.Vendors.Requests;

namespace HalalChain.Application.Vendors.Validators;

public class UpdateVendorRequestValidator : AbstractValidator<UpdateVendorRequest>
{
    public UpdateVendorRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Vendor name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Country));

        RuleFor(x => x.Status)
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
