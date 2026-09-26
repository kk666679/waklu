using MediatR;

namespace HalalChain.Application.Catalog.Commands;

/// <summary>
/// Command to bulk create or update product variants.
/// Supports Cartesian product generation from attributes.
/// </summary>
public record BulkUpsertProductVariantsCommand : IRequest<BulkUpsertVariantsResult>
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    
    /// <summary>
    /// Attributes for variant generation (e.g., Color: [Red, Blue], Size: [S, M, L]).
    /// </summary>
    public Dictionary<string, List<string>> Attributes { get; set; } = [];
    
    /// <summary>
    /// Base price for generated variants.
    /// </summary>
    public decimal BasePrice { get; set; }
    
    /// <summary>
    /// Base inventory stock for generated variants.
    /// </summary>
    public int BaseStock { get; set; }
    
    /// <summary>
    /// Low stock threshold (default: 5).
    /// </summary>
    public int LowStockThreshold { get; set; } = 5;
}

public record BulkUpsertVariantsResult
{
    public Guid ProductId { get; set; }
    public int VariantsCreated { get; set; }
    public int VariantsUpdated { get; set; }
    public List<string>? Errors { get; set; }
}
