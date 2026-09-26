using FluentValidation;
using HalalChain.Application.Catalog.Commands;

namespace HalalChain.Application.Catalog.Validators;

public class UpdateProductSustainabilityCommandValidator : AbstractValidator<UpdateProductSustainabilityCommand>
{
    public UpdateProductSustainabilityCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("VendorId is required.");

        RuleFor(x => x.CarbonFootprintKgCo2)
            .GreaterThanOrEqualTo(0).When(x => x.CarbonFootprintKgCo2.HasValue)
            .WithMessage("CarbonFootprintKgCo2 must be non-negative.");

        RuleFor(x => x.EcoRating)
            .InclusiveBetween(1, 5).When(x => x.EcoRating.HasValue)
            .WithMessage("EcoRating must be between 1 and 5.");

        RuleFor(x => x.Certifications)
            .Must(CertificationsNotNull).WithMessage("Certifications must not be null.")
            .Must(HaveValidCertifications).WithMessage("Certifications contain invalid values.");

        RuleFor(x => x.PackagingMaterial)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.PackagingMaterial))
            .WithMessage("PackagingMaterial must not exceed 100 characters.");

        RuleFor(x => x.RecyclablePercentage)
            .InclusiveBetween(0, 100).When(x => x.RecyclablePercentage.HasValue)
            .WithMessage("RecyclablePercentage must be between 0 and 100.");

        RuleFor(x => x.ProductionEnergyKwh)
            .GreaterThanOrEqualTo(0).When(x => x.ProductionEnergyKwh.HasValue)
            .WithMessage("ProductionEnergyKwh must be non-negative.");

        RuleFor(x => x.ProductionWaterLiters)
            .GreaterThanOrEqualTo(0).When(x => x.ProductionWaterLiters.HasValue)
            .WithMessage("ProductionWaterLiters must be non-negative.");

        RuleFor(x => x.OriginCountry)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.OriginCountry))
            .WithMessage("OriginCountry must not exceed 100 characters.");

        RuleFor(x => x.SustainabilityReportUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrWhiteSpace(x.SustainabilityReportUrl))
            .WithMessage("SustainabilityReportUrl must be a valid URL.");
    }

    private static bool CertificationsNotNull(List<string> certifications)
    {
        return certifications != null;
    }

    private static bool HaveValidCertifications(List<string> certifications)
    {
        if (certifications == null || certifications.Count == 0)
            return true;

        var validCertifications = new[] 
        { 
            "Fair Trade", "Organic", "B Corp", "Rainforest Alliance", "MSC", "FSC", "Vegan", "Kosher", "Cruelty-Free", "Carbon Neutral"
        };

        return certifications.All(c => validCertifications.Contains(c, StringComparer.OrdinalIgnoreCase));
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
