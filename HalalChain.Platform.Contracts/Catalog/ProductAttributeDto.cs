namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// A product attribute used for faceted search, filtering, and variant generation.
/// Examples: "Color", "Size", "Material", "Weight", "Flavor".
/// </summary>
public sealed class ProductAttributeDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Attribute name (e.g., "Color", "Size", "Material").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Attribute type: "Select" (dropdown), "MultiSelect", "Range", "Text", "Boolean".
    /// </summary>
    public string Type { get; set; } = "Select";

    /// <summary>
    /// Whether this attribute is filterable in faceted search.
    /// </summary>
    public bool IsFilterable { get; set; } = true;

    /// <summary>
    /// Whether this attribute affects variant creation.
    /// </summary>
    public bool IsVariantAttribute { get; set; }

    /// <summary>
    /// Sort order for display in filters.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// For "Select" or "MultiSelect" types: list of available options.
    /// Example: ["Red", "Blue", "Green", "Black"]
    /// </summary>
    public List<string> OptionValues { get; set; } = [];

    /// <summary>
    /// For "Range" types: minimum value.
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// For "Range" types: maximum value.
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "kg", "ml", "cm", "g").
    /// </summary>
    public string? Unit { get; set; }
}

/// <summary>
/// An attribute value assigned to a product.
/// Connects a product to specific attribute options for filtering and variants.
/// </summary>
public sealed class ProductAttributeValueDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid AttributeId { get; set; }

    /// <summary>
    /// The selected value(s) for this attribute.
    /// For "Select": single value. For "MultiSelect": comma-separated or JSON array.
    /// For "Range": stored as "min-max" (e.g., "10-50").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the attribute definition (for UI rendering).
    /// </summary>
    public ProductAttributeDto? Attribute { get; set; }
}
