namespace HalalChain.Domain.Catalog;

using HalalChain.Domain.Common;

/// <summary>
/// The Product aggregate root.
///
/// Two invariants are structural, not conventions:
///   1. There is no IsHalal boolean. Compliance status lives in the
///      VerdictBinding, which is issued by tawheed, not by this aggregate.
///   2. Status is mutated only through ApplyTransition, which takes two
///      ProductStatus values. The state machine lives in the Application
///      layer; the mutation contract lives here.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductMediaAsset> _media = [];

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BrandId? BrandId { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public Guid VendorId { get; private set; }
    public ProductType Type { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public VerdictBinding VerdictBinding { get; private set; } = VerdictBinding.Unbound;
    public ProductSustainability? Sustainability { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyList<ProductMediaAsset> Media => _media.AsReadOnly();

    private Product() { }

    public static Product Create(
        Guid vendorId,
        string title,
        ProductType type,
        DateTimeOffset now,
        BrandId? brandId = null,
        CategoryId? categoryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var product = new Product
        {
            Id = ProductId.New(),
            VendorId = vendorId,
            Title = title,
            Type = type,
            BrandId = brandId,
            CategoryId = categoryId,
            Status = ProductStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        product.Raise(new ProductCreated(product.Id, vendorId, now));
        return product;
    }

    public void UpdateDetails(string title, string? description, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        UpdatedAt = now;
    }

    public void AddVariant(ProductVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        if (_variants.Any(v => v.Sku == variant.Sku))
            throw new InvalidOperationException($"Variant with SKU {variant.Sku} already exists.");
        _variants.Add(variant);
    }

    public void AddMedia(ProductMediaAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        _media.Add(asset);
    }

    public void SetSustainability(ProductSustainability sustainability, DateTimeOffset now)
    {
        Sustainability = sustainability ?? throw new ArgumentNullException(nameof(sustainability));
        UpdatedAt = now;
    }

    /// <summary>
    /// Rebinds the product to a new verdict. Called by the Application
    /// layer's BindVerdictHandler after tawheed issues a decision.
    ///
    /// This method does not evaluate policy. It records what tawheed said.
    /// </summary>
    public void BindVerdict(VerdictBinding binding, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(binding);
        VerdictBinding = binding;
        UpdatedAt = now;
        Raise(new VerdictBound(Id, binding.State, binding.PolicyVersion, now));
    }

    /// <summary>
    /// The only path that mutates Status. Takes two values rather than a
    /// StatusTransition record because the latter lives in the Application
    /// layer, and Domain must not reference Application.
    ///
    /// The Application-layer state machine is responsible for computing
    /// the transition. This method is responsible for validating that the
    /// product is actually in the expected From state before applying.
    /// </summary>
    public void ApplyTransition(ProductStatus from, ProductStatus to, DateTimeOffset now)
    {
        if (Status != from)
            throw new InvalidOperationException(
                $"Cannot apply transition {from} -> {to}: product is currently {Status}.");

        if (from == to)
            return;

        Status = to;
        UpdatedAt = now;
        Raise(new ProductStatusChanged(Id, from, to, now));
    }
}
