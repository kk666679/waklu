using HalalChain.Application.Common.Interfaces;
using HalalChain.Application.Catalog.Commands;
using HalalChain.Application.Common.Exceptions;
using HalalChain.Domain.Catalog;
using MediatR;

namespace HalalChain.Application.Catalog.Handlers;

/// <summary>
/// Handler for bulk upserting product variants.
/// Supports Cartesian product generation from attribute combinations.
/// </summary>
public sealed class BulkUpsertProductVariantsHandler(IProductRepository productRepository)
    : IRequestHandler<BulkUpsertProductVariantsCommand, BulkUpsertVariantsResult>
{
    public async Task<BulkUpsertVariantsResult> Handle(BulkUpsertProductVariantsCommand request, CancellationToken ct)
    {
        // Verify product exists and belongs to vendor
        var productExists = await productRepository.IsOwnedByVendorAsync(request.ProductId, request.VendorId, ct);
        if (!productExists)
        {
            throw new ValidationException("Product not found or does not belong to vendor.");
        }

        var result = new BulkUpsertVariantsResult
        {
            ProductId = request.ProductId,
            VariantsCreated = 0,
            VariantsUpdated = 0,
            Errors = []
        };

        // Generate Cartesian product of attribute values
        var variantCombinations = GenerateVariantCombinations(request.Attributes);
        
        if (variantCombinations.Count == 0)
        {
            result.Errors!.Add("No valid variant combinations generated from provided attributes.");
            return result;
        }

        // TODO: Batch upsert variants via repository
        // For each combination, create or update ProductVariant
        foreach (var combination in variantCombinations)
        {
            try
            {
                // Create variant with combination attributes
                var variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    Sku = GenerateSku(combination),
                    Price = request.BasePrice,
                    Stock = request.BaseStock,
                    LowStockThreshold = request.LowStockThreshold,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // TODO: Persist via repository
                // await productRepository.UpsertVariantAsync(variant, ct);
                
                result.VariantsCreated++;
            }
            catch (Exception ex)
            {
                result.Errors!.Add($"Error processing variant: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Generates all Cartesian product combinations from attributes.
    /// </summary>
    private static List<Dictionary<string, string>> GenerateVariantCombinations(Dictionary<string, List<string>> attributes)
    {
        if (attributes.Count == 0)
            return [];

        var result = new List<Dictionary<string, string>>();
        var keys = attributes.Keys.ToList();
        var indices = new int[keys.Count];

        while (true)
        {
            // Build current combination
            var combination = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                combination[key] = attributes[key][indices[i]];
            }
            result.Add(combination);

            // Advance indices
            int pos = keys.Count - 1;
            while (pos >= 0 && ++indices[pos] >= attributes[keys[pos]].Count)
            {
                indices[pos] = 0;
                pos--;
            }

            if (pos < 0)
                break;
        }

        return result;
    }

    /// <summary>
    /// Generates a SKU from variant combination.
    /// </summary>
    private static string GenerateSku(Dictionary<string, string> combination)
    {
        var values = string.Join("-", combination.Values.Select(v => v.Substring(0, Math.Min(3, v.Length)).ToUpperInvariant()));
        return $"VAR-{Guid.NewGuid().ToString().Substring(0, 8).ToUpperInvariant()}-{values}";
    }
}
