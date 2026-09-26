using FluentValidation;
using HalalChain.Application.Catalog.Commands;

namespace HalalChain.Application.Catalog.Validators;

public class AddProductMediaAssetCommandValidator : AbstractValidator<AddProductMediaAssetCommand>
{
    public AddProductMediaAssetCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("VendorId is required.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required.")
            .Must(BeValidUrl).WithMessage("Url must be a valid URL.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .Must(BeValidMediaType).WithMessage("Type must be one of: Image, Video, 360View, Demo, Instruction, ThreeDModel.");

        RuleFor(x => x.AltText)
            .MaximumLength(500).WithMessage("AltText must not exceed 500 characters.");

        RuleFor(x => x.Caption)
            .MaximumLength(1000).WithMessage("Caption must not exceed 1000 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("SortOrder must be non-negative.");

        RuleFor(x => x.BlurHash)
            .MaximumLength(100).WithMessage("BlurHash must not exceed 100 characters.");

        RuleFor(x => x.VideoDurationSeconds)
            .GreaterThan(0).When(x => x.VideoDurationSeconds.HasValue)
            .WithMessage("VideoDurationSeconds must be greater than 0.");

        RuleFor(x => x.VideoThumbnailUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrWhiteSpace(x.VideoThumbnailUrl))
            .WithMessage("VideoThumbnailUrl must be a valid URL.");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidMediaType(string type)
    {
        var validTypes = new[] { "Image", "Video", "360View", "Demo", "Instruction", "ThreeDModel" };
        return validTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
