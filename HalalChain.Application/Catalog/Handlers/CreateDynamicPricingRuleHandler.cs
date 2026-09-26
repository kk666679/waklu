using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Domain.Catalog;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

/// <summary>
/// Handler for creating dynamic pricing rules.
/// </summary>
public sealed class CreateDynamicPricingRuleHandler(IProductRepository productRepository)
    : IRequestHandler<CreateDynamicPricingRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateDynamicPricingRuleCommand request, CancellationToken ct)
    {
        // Validate rule type
        var validRuleTypes = new[] { "Promotion", "TieredDiscount", "Flash", "ABTest", "Competitive" };
        if (!validRuleTypes.Contains(request.RuleType))
        {
            throw new ValidationException($"Invalid rule type. Must be one of: {string.Join(", ", validRuleTypes)}");
        }

        // Validate adjustment type
        var validAdjustmentTypes = new[] { "Percentage", "Fixed", "Multiplier" };
        if (!validAdjustmentTypes.Contains(request.AdjustmentType))
        {
            throw new ValidationException($"Invalid adjustment type. Must be one of: {string.Join(", ", validAdjustmentTypes)}");
        }

        // Validate dates
        if (request.EndDate.HasValue && request.EndDate <= request.StartDate)
        {
            throw new ValidationException("End date must be after start date.");
        }

        // Validate products belong to vendor
        if (request.ProductIds.Count > 0)
        {
            foreach (var productId in request.ProductIds)
            {
                var productExists = await productRepository.IsOwnedByVendorAsync(productId, request.VendorId, ct);
                if (!productExists)
                {
                    throw new ValidationException($"Product {productId} not found or does not belong to vendor.");
                }
            }
        }

        // Create pricing rule
        var pricingRule = new DynamicPricingRule
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            ProductIds = request.ProductIds,
            Name = request.Name,
            Description = request.Description,
            RuleType = request.RuleType,
            Conditions = request.Conditions,
            PriceAdjustment = request.PriceAdjustment,
            AdjustmentType = request.AdjustmentType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            Priority = request.Priority ?? 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // TODO: Persist via repository or direct EF Core context
        // await productRepository.CreatePricingRuleAsync(pricingRule, ct);

        return pricingRule.Id;
    }
}
