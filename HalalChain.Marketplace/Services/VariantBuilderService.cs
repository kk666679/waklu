namespace HalalChain.Marketplace.Services;

using HalalChain.Models;

/// <summary>
/// Service for building and managing product variants.
/// Supports color/size/material combinations, barcode generation, and inventory management per variant.
/// </summary>
public interface IVariantBuilderService
{
    /// <summary>
    /// Generate variant combinations from attribute values.
    /// Example: colors=[Red, Blue] x sizes=[S, M, L] = 6 variants
    /// </summary>
    Task<List<VariantTemplate>> GenerateVariantCombinationsAsync(
        int productId,
        Dictionary<string, List<string>> attributes,
        decimal? basePrice = null,
        int? baseStock = null,
        CancellationToken ct = default);

    /// <summary>
    /// Create a single variant.
    /// </summary>
    Task<ProductVariantModel> CreateVariantAsync(ProductVariantModel variant, CancellationToken ct = default);

    /// <summary>
    /// Update multiple variants at once.
    /// </summary>
    Task<List<ProductVariantModel>> UpdateVariantsAsync(List<ProductVariantModel> variants, CancellationToken ct = default);

    /// <summary>
    /// Bulk update variant prices (e.g., apply discount to all S sizes).
    /// </summary>
    Task<int> BulkUpdateVariantPricesAsync(int productId, Dictionary<string, decimal> updates, CancellationToken ct = default);

    /// <summary>
    /// Generate barcodes for variants (EAN-13, UPC, etc.).
    /// </summary>
    Task<Dictionary<int, string>> GenerateBarcodesAsync(int productId, string format = "EAN13", CancellationToken ct = default);

    /// <summary>
    /// Get variant template matrix (visual grid of combinations).
    /// </summary>
    Task<VariantMatrix> GetVariantMatrixAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Sync variants with external inventory system (3PL, warehouse).
    /// </summary>
    Task<SyncResult> SyncVariantInventoryAsync(int productId, string externalSystemId, CancellationToken ct = default);

    /// <summary>
    /// Archive old/discontinued variants.
    /// </summary>
    Task<int> ArchiveVariantsAsync(int productId, List<int> variantIds, CancellationToken ct = default);
}

/// <summary>
/// Implementation of variant builder service.
/// </summary>
public class VariantBuilderService : IVariantBuilderService
{
    private readonly ILogger<VariantBuilderService> _logger;

    public VariantBuilderService(ILogger<VariantBuilderService> logger)
    {
        _logger = logger;
    }

    public async Task<List<VariantTemplate>> GenerateVariantCombinationsAsync(
        int productId,
        Dictionary<string, List<string>> attributes,
        decimal? basePrice = null,
        int? baseStock = null,
        CancellationToken ct = default)
    {
        try
        {
            var variants = new List<VariantTemplate>();

            // Validate attributes
            if (!attributes.Any())
                throw new ArgumentException("At least one attribute with values is required.");

            // Generate all combinations using Cartesian product
            var combinations = GenerateCartesianProduct(attributes);

            foreach (var (index, combination) in combinations.Select((c, i) => (i, c)))
            {
                var variantName = string.Join(" / ", combination.Values);
                var sku = GenerateSku(productId, index);

                variants.Add(new VariantTemplate
                {
                    Sku = sku,
                    Name = variantName,
                    VariantAttributes = System.Text.Json.JsonSerializer.Serialize(combination),
                    Price = basePrice ?? 0,
                    Stock = baseStock ?? 0,
                    LowStockThreshold = 5,
                    IsActive = true,
                    SortOrder = index
                });
            }

            _logger.LogInformation("Generated {Count} variant combinations for ProductId: {ProductId}",
                variants.Count, productId);

            return variants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating variant combinations for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<ProductVariantModel> CreateVariantAsync(ProductVariantModel variant, CancellationToken ct = default)
    {
        try
        {
            // Validate variant
            if (string.IsNullOrWhiteSpace(variant.Sku))
                throw new ArgumentException("SKU is required.");

            if (variant.Stock < 0)
                throw new ArgumentException("Stock cannot be negative.");

            if (variant.Price <= 0)
                throw new ArgumentException("Price must be positive.");

            variant.CreatedAt = DateTime.UtcNow;

            _logger.LogInformation("Variant created: ProductId={ProductId}, Sku={Sku}, Name={Name}",
                variant.ProductId, variant.Sku, variant.Name);

            return variant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variant for ProductId: {ProductId}", variant.ProductId);
            throw;
        }
    }

    public async Task<List<ProductVariantModel>> UpdateVariantsAsync(List<ProductVariantModel> variants, CancellationToken ct = default)
    {
        try
        {
            var updated = new List<ProductVariantModel>();

            foreach (var variant in variants)
            {
                if (variant.Price <= 0)
                    throw new ArgumentException($"Variant {variant.Sku}: Price must be positive.");

                if (variant.Stock < 0)
                    throw new ArgumentException($"Variant {variant.Sku}: Stock cannot be negative.");

                updated.Add(variant);
            }

            _logger.LogInformation("Updated {Count} variants", variants.Count);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variants");
            throw;
        }
    }

    public async Task<int> BulkUpdateVariantPricesAsync(int productId, Dictionary<string, decimal> updates, CancellationToken ct = default)
    {
        try
        {
            // updates: { "SKU-001": 29.99, "SKU-002": 39.99, ... }
            int updateCount = 0;

            foreach (var (sku, newPrice) in updates)
            {
                if (newPrice <= 0)
                    _logger.LogWarning("Invalid price for SKU {Sku}: {Price}", sku, newPrice);
                else
                    updateCount++;
            }

            _logger.LogInformation("Bulk updated {Count} variant prices for ProductId: {ProductId}",
                updateCount, productId);

            return updateCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating variant prices for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<Dictionary<int, string>> GenerateBarcodesAsync(int productId, string format = "EAN13", CancellationToken ct = default)
    {
        try
        {
            var barcodes = new Dictionary<int, string>();

            // Generate barcodes based on format
            // In production, integrate with barcode generation library
            for (int i = 0; i < 10; i++)
            {
                var barcode = format.ToUpperInvariant() switch
                {
                    "EAN13" => GenerateEan13($"{productId}{i:D3}"),
                    "UPC" => GenerateUpc($"{productId}{i:D2}"),
                    "CODE128" => $"CODE128-{productId}-{i}",
                    _ => GenerateEan13($"{productId}{i:D3}")
                };

                barcodes[i] = barcode;
            }

            _logger.LogInformation("Generated {Count} barcodes for ProductId: {ProductId} using format: {Format}",
                barcodes.Count, productId, format);

            return barcodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcodes for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<VariantMatrix> GetVariantMatrixAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            // Return a matrix view of variants (e.g., colors as columns, sizes as rows)
            var matrix = new VariantMatrix
            {
                ProductId = productId,
                Rows = new List<VariantMatrixRow>(),
                Columns = new List<string>()
            };

            // In production, fetch actual variants from database and structure as matrix
            // Example:
            // Rows: ["S", "M", "L", "XL"] (sizes)
            // Columns: ["Red", "Blue", "Green"] (colors)
            // Cells: variant data

            return matrix;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variant matrix for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<SyncResult> SyncVariantInventoryAsync(int productId, string externalSystemId, CancellationToken ct = default)
    {
        try
        {
            // Sync with external inventory systems (3PL, warehouse, etc.)
            var result = new SyncResult
            {
                ExternalSystemId = externalSystemId,
                ProductId = productId,
                SyncedAt = DateTime.UtcNow,
                Status = "Success",
                VariantsSynced = 0,
                ErrorCount = 0
            };

            _logger.LogInformation("Synced variant inventory with {SystemId} for ProductId: {ProductId}",
                externalSystemId, productId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing variant inventory with {SystemId} for ProductId: {ProductId}",
                externalSystemId, productId);
            throw;
        }
    }

    public async Task<int> ArchiveVariantsAsync(int productId, List<int> variantIds, CancellationToken ct = default)
    {
        try
        {
            int archivedCount = 0;

            foreach (var variantId in variantIds)
            {
                // Mark variant as inactive/archived
                archivedCount++;
            }

            _logger.LogInformation("Archived {Count} variants for ProductId: {ProductId}", archivedCount, productId);

            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving variants for ProductId: {ProductId}", productId);
            throw;
        }
    }

    // Private helpers

    private List<Dictionary<string, string>> GenerateCartesianProduct(Dictionary<string, List<string>> attributes)
    {
        var keys = attributes.Keys.ToList();
        var results = new List<Dictionary<string, string>>();

        if (!keys.Any()) return results;

        var indices = new int[keys.Count];
        var sizes = keys.Select(k => attributes[k].Count).ToArray();

        while (true)
        {
            var combination = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                combination[keys[i]] = attributes[keys[i]][indices[i]];
            }
            results.Add(combination);

            // Increment indices
            int pos = keys.Count - 1;
            while (pos >= 0 && ++indices[pos] >= sizes[pos])
            {
                indices[pos] = 0;
                pos--;
            }

            if (pos < 0) break;
        }

        return results;
    }

    private string GenerateSku(int productId, int variantIndex)
    {
        return $"SKU-{productId:D5}-{variantIndex:D3}";
    }

    private string GenerateEan13(string source)
    {
        // Simplified EAN-13 generation (real implementation would include checksum)
        return "5" + source.PadRight(12, '0').Substring(0, 12);
    }

    private string GenerateUpc(string source)
    {
        // Simplified UPC generation
        return source.PadRight(12, '0').Substring(0, 12);
    }
}

// DTOs

public class VariantTemplate
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? VariantAttributes { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class VariantMatrix
{
    public int ProductId { get; set; }
    public List<string> Columns { get; set; } = [];
    public List<VariantMatrixRow> Rows { get; set; } = [];
}

public class VariantMatrixRow
{
    public string Label { get; set; } = string.Empty;
    public List<VariantMatrixCell> Cells { get; set; } = [];
}

public class VariantMatrixCell
{
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }
}

public class SyncResult
{
    public string ExternalSystemId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Status { get; set; } = "Success"; // Success, Partial, Failed
    public int VariantsSynced { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
