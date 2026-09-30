namespace HalalChain.Marketplace.Data;

using HalalChain.Domain.Blockchain;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Commerce;
using HalalChain.Domain.Common;
using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Platform database context for HalalChain.
/// Maps all domain entities: Catalog (Product, Brand, Taxonomy), Halal (Certificate, HalalVerification),
/// Commerce (Order, Cart, Wishlist), Vendors, Blockchain (Chain mirrors, outbox), and Common (Outbox).
/// </summary>
public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    #region Catalog Domain

    /// <summary>Products in the catalog.</summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>Product variants (size, weight, color, etc.).</summary>
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;

    /// <summary>Product media assets (images, videos, 360 views).</summary>
    public DbSet<ProductMediaAsset> ProductMediaAssets { get; set; } = null!;

    /// <summary>Product attribute values (faceted search filters).</summary>
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; } = null!;

    /// <summary>Product attribute definitions (Color, Size, Material, Weight, etc.).</summary>
    public DbSet<ProductAttribute> ProductAttributes { get; set; } = null!;

    /// <summary>Product embeddings for semantic search (AI-generated vectors).</summary>
    public DbSet<ProductEmbedding> ProductEmbeddings { get; set; } = null!;

    /// <summary>Product sustainability and ESG metrics.</summary>
    public DbSet<ProductSustainability> ProductSustainabilities { get; set; } = null!;

    /// <summary>Dynamic pricing rules (promotions, tiered discounts, A/B tests).</summary>
    public DbSet<DynamicPricingRule> DynamicPricingRules { get; set; } = null!;

    /// <summary>Brands (manufacturer/vendor brand identity).</summary>
    public DbSet<Brand> Brands { get; set; } = null!;

    /// <summary>Legacy flat category structure (being phased out).</summary>
    public DbSet<Category> Categories { get; set; } = null!;

    /// <summary>Top-level commercial departments (Food & Beverage, Modest Fashion, etc.).</summary>
    public DbSet<Department> Departments { get; set; } = null!;

    /// <summary>Mid-tier commercial categories (Meat & Poultry, Dairy & Eggs, etc.).</summary>
    public DbSet<TaxonomyCategory> TaxonomyCategories { get; set; } = null!;

    /// <summary>Filter-chip level subcategories (Beef, Chicken, Organic, etc.).</summary>
    public DbSet<Subcategory> Subcategories { get; set; } = null!;

    /// <summary>Atomic product type classification (bottom of 4-level hierarchy).</summary>
    public DbSet<ProductType> ProductTypes { get; set; } = null!;

    /// <summary>Reference data: Halal certifying bodies.</summary>
    public DbSet<CertificationBody> CertificationBodies { get; set; } = null!;

    /// <summary>Reference data: Manufacturing/processing facilities.</summary>
    public DbSet<Facility> Facilities { get; set; } = null!;

    /// <summary>Reference data: Countries for origin tracking.</summary>
    public DbSet<Country> Countries { get; set; } = null!;

    #endregion

    #region Halal Domain

    /// <summary>Halal certificates submitted by vendors for products.</summary>
    public DbSet<Certificate> Certificates { get; set; } = null!;

    /// <summary>Deterministic verification results from Policy Engine.</summary>
    public DbSet<HalalVerification> HalalVerifications { get; set; } = null!;

    /// <summary>Evidence items collected by AI agents during verification.</summary>
    public DbSet<VerificationEvidence> VerificationEvidences { get; set; } = null!;

    /// <summary>Audit trail for verification decisions (append-only).</summary>
    public DbSet<VerificationAudit> VerificationAudits { get; set; } = null!;

    #endregion

    #region Commerce Domain

    /// <summary>Customer orders (root aggregate for order cluster).</summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>Vendor order splits (one per vendor per order).</summary>
    public DbSet<VendorOrder> VendorOrders { get; set; } = null!;

    /// <summary>Order line items (product quantity and price).</summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>Shopping cart items (customer's temporary selections).</summary>
    public DbSet<CartItem> CartItems { get; set; } = null!;

    /// <summary>Wishlist items (customer's saved favorites).</summary>
    public DbSet<WishlistItem> WishlistItems { get; set; } = null!;

    #endregion

    #region Vendors Domain

    /// <summary>Vendor/merchant entities.</summary>
    public DbSet<Vendor> Vendors { get; set; } = null!;

    #endregion

    #region Blockchain Domain

    /// <summary>Durable outbox for blockchain transactions (idempotent writes).</summary>
    public DbSet<ChainTxOutbox> ChainTxOutboxes { get; set; } = null!;

    /// <summary>On-chain mirror: supplier registry entries.</summary>
    public DbSet<SupplierOnChain> SuppliersOnChain { get; set; } = null!;

    /// <summary>On-chain mirror: product registrations.</summary>
    public DbSet<ProductOnChain> ProductsOnChain { get; set; } = null!;

    /// <summary>On-chain mirror: certificates.</summary>
    public DbSet<CertificateOnChain> CertificatesOnChain { get; set; } = null!;

    /// <summary>On-chain mirror: traceability events (batch tracking).</summary>
    public DbSet<TraceabilityEventOnChain> TraceabilityEventsOnChain { get; set; } = null!;

    #endregion

    #region Common Domain

    /// <summary>Outbox for domain events pending publication.</summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── CATALOG DOMAIN ──────────────────────────────────────────────────
        ConfigureProduct(modelBuilder);
        ConfigureProductVariant(modelBuilder);
        ConfigureProductMediaAsset(modelBuilder);
        ConfigureProductAttribute(modelBuilder);
        ConfigureProductAttributeValue(modelBuilder);
        ConfigureProductEmbedding(modelBuilder);
        ConfigureProductSustainability(modelBuilder);
        ConfigureBrand(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureTaxonomy(modelBuilder);
        ConfigureDynamicPricingRule(modelBuilder);

        // ── HALAL DOMAIN ────────────────────────────────────────────────────
        ConfigureCertificate(modelBuilder);
        ConfigureHalalVerification(modelBuilder);
        ConfigureVerificationEvidence(modelBuilder);
        ConfigureVerificationAudit(modelBuilder);

        // ── COMMERCE DOMAIN ─────────────────────────────────────────────────
        ConfigureOrder(modelBuilder);
        ConfigureVendorOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureCartItem(modelBuilder);
        ConfigureWishlistItem(modelBuilder);

        // ── VENDORS DOMAIN ──────────────────────────────────────────────────
        ConfigureVendor(modelBuilder);

        // ── BLOCKCHAIN DOMAIN ───────────────────────────────────────────────
        ConfigureChainTxOutbox(modelBuilder);
        ConfigureSupplierOnChain(modelBuilder);
        ConfigureProductOnChain(modelBuilder);
        ConfigureCertificateOnChain(modelBuilder);
        ConfigureTraceabilityEventOnChain(modelBuilder);

        // ── COMMON DOMAIN ───────────────────────────────────────────────────
        ConfigureOutboxMessage(modelBuilder);
    }

    #region Configuration Methods

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Product>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.ShortDescription).HasMaxLength(500);
        entity.Property(e => e.Keywords).HasMaxLength(500);
        entity.Property(e => e.Origin).HasMaxLength(500);
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");
        entity.Property(e => e.AllergenWarnings).HasMaxLength(1000);
        entity.Property(e => e.RecommendationReason).HasMaxLength(500);

        // JSONB for HalalProfile (PostgreSQL)
        entity.Property(e => e.HalalProfile)
            .HasColumnType("jsonb")
            .IsRequired(false);

        // Store SecondaryCategories as JSON array
        entity.Property(e => e.SecondaryCategories)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        // Store Tags and Ingredients as JSON arrays
        entity.Property(e => e.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.Property(e => e.Ingredients)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.HasOne(e => e.Vendor).WithMany(v => v.Products).HasForeignKey(e => e.VendorId).IsRequired();
        entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId).IsRequired(false);
        entity.HasOne(e => e.ProductType).WithMany().HasForeignKey(e => e.ProductTypeId).IsRequired(false);
        entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).IsRequired(false);

        entity.HasMany(e => e.Variants).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.MediaAssets).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.Attributes).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.Certificates).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.Verifications).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.CartItems).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.OrderItems).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.WishlistItems).WithOne().HasForeignKey(e => e.ProductId).IsRequired();
        entity.HasMany(e => e.PricingRules).WithOne().HasForeignKey(e => e.ProductId).IsRequired();

        // Indexes for common queries
        entity.HasIndex(e => e.Slug).IsUnique();
        entity.HasIndex(e => e.VendorId);
        entity.HasIndex(e => e.IsSearchIndexed);
        entity.HasIndex(e => e.UpdatedAt);
    }

    private static void ConfigureProductVariant(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductVariant>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Sku).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Barcode).HasMaxLength(100);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
        entity.Property(e => e.VariantAttributes).HasMaxLength(1000);
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");
        entity.Property(e => e.NetUnit).HasMaxLength(50);

        // WholesaleTiers as JSON array
        entity.Property(e => e.WholesaleTiers)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<WholesalePriceTier>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.HasIndex(e => e.Sku).IsUnique();
        entity.HasIndex(e => e.ProductId);
    }

    private static void ConfigureProductMediaAsset(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductMediaAsset>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
        entity.Property(e => e.AltText).HasMaxLength(500);
        entity.Property(e => e.Caption).HasMaxLength(500);
        entity.Property(e => e.BlurHash).HasMaxLength(100);
        entity.Property(e => e.VideoThumbnailUrl).HasMaxLength(500);
        entity.Property(e => e.VideoTranscript).HasMaxLength(5000);
        entity.Property(e => e.Attribution).HasMaxLength(500);

        entity.HasIndex(e => e.ProductId);
    }

    private static void ConfigureProductAttribute(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductAttribute>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Unit).HasMaxLength(50);

        // OptionValues as JSON array
        entity.Property(e => e.OptionValues)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );
    }

    private static void ConfigureProductAttributeValue(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductAttributeValue>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Value).IsRequired().HasMaxLength(500);

        entity.HasOne<ProductAttribute>().WithMany().HasForeignKey(e => e.AttributeId).IsRequired();
        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.AttributeId);
    }

    private static void ConfigureProductEmbedding(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductEmbedding>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Model).IsRequired().HasMaxLength(100);

        // Embedding as vector (pgvector in PostgreSQL)
        entity.Property(e => e.Embedding)
            .HasColumnType("vector(256)")
            .IsRequired();

        entity.HasIndex(e => e.ProductId).IsUnique();
    }

    private static void ConfigureProductSustainability(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductSustainability>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.PackagingMaterial).HasMaxLength(200);
        entity.Property(e => e.SustainabilityReportUrl).HasMaxLength(500);

        // Certifications as JSON array
        entity.Property(e => e.Certifications)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.HasIndex(e => e.ProductId).IsUnique();
    }

    private static void ConfigureBrand(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Brand>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        entity.Property(e => e.LogoUrl).HasMaxLength(500);
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.CountryOfOrigin).HasMaxLength(2);
        entity.Property(e => e.WebsiteUrl).HasMaxLength(500);

        entity.HasIndex(e => e.Slug).IsUnique();
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Category>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);

        entity.HasOne<Category>().WithMany(c => c.Children).HasForeignKey(e => e.ParentId).IsRequired(false);
        entity.HasMany(e => e.Products).WithOne().HasForeignKey(e => e.CategoryId).IsRequired(false);

        entity.HasIndex(e => e.Slug);
    }

    private static void ConfigureTaxonomy(ModelBuilder modelBuilder)
    {
        // Department
        var dept = modelBuilder.Entity<Department>();
        dept.HasKey(e => e.Id);
        dept.Property(e => e.Id).ValueGeneratedNever();
        dept.Property(e => e.Name).IsRequired().HasMaxLength(255);
        dept.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        dept.Property(e => e.Description).HasMaxLength(1000);
        dept.Property(e => e.IconClass).HasMaxLength(100);
        dept.Property(e => e.HeroImageUrl).HasMaxLength(500);
        dept.HasIndex(e => e.Slug).IsUnique();
        dept.HasMany(e => e.Categories).WithOne(tc => tc.Department).HasForeignKey(tc => tc.DepartmentId).IsRequired();

        // TaxonomyCategory
        var taxCat = modelBuilder.Entity<TaxonomyCategory>();
        taxCat.HasKey(e => e.Id);
        taxCat.Property(e => e.Id).ValueGeneratedNever();
        taxCat.Property(e => e.Name).IsRequired().HasMaxLength(255);
        taxCat.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        taxCat.Property(e => e.Description).HasMaxLength(1000);
        taxCat.Property(e => e.IconClass).HasMaxLength(100);
        taxCat.HasIndex(e => new { e.DepartmentId, e.Slug }).IsUnique();
        taxCat.HasMany(e => e.Subcategories).WithOne(s => s.Category).HasForeignKey(s => s.CategoryId).IsRequired();

        // Subcategory
        var subCat = modelBuilder.Entity<Subcategory>();
        subCat.HasKey(e => e.Id);
        subCat.Property(e => e.Id).ValueGeneratedNever();
        subCat.Property(e => e.Name).IsRequired().HasMaxLength(255);
        subCat.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        subCat.Property(e => e.Description).HasMaxLength(1000);
        subCat.HasIndex(e => new { e.CategoryId, e.Slug }).IsUnique();
        subCat.HasMany(e => e.ProductTypes).WithOne(pt => pt.Subcategory).HasForeignKey(pt => pt.SubcategoryId).IsRequired();
    }

    private static void ConfigureDynamicPricingRule(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DynamicPricingRule>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.RuleType).IsRequired().HasMaxLength(50);

        // ProductIds as JSON array
        entity.Property(e => e.ProductIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        // Conditions as JSON
        entity.Property(e => e.Conditions).HasColumnType("jsonb");

        // CustomerSegments as JSON array
        entity.Property(e => e.CustomerSegments)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.Property(e => e.AdjustmentType).IsRequired().HasMaxLength(50);

        entity.HasIndex(e => e.VendorId);
    }

    private static void ConfigureCertificate(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Certificate>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.CertificateNumber).IsRequired().HasMaxLength(100);
        entity.Property(e => e.CertificationBody).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Jurisdiction).IsRequired().HasMaxLength(2).HasDefaultValue("MY");
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Scope).HasMaxLength(500);

        entity.HasMany(e => e.Verifications).WithOne().HasForeignKey(e => e.ProductId).IsRequired();

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.CertificateNumber).IsUnique();
    }

    private static void ConfigureHalalVerification(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<HalalVerification>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.ComplianceStatus).IsRequired().HasMaxLength(50);
        entity.Property(e => e.PolicyVersion).IsRequired().HasMaxLength(50).HasDefaultValue("MY-v3");
        entity.Property(e => e.Jurisdiction).IsRequired().HasMaxLength(2).HasDefaultValue("MY");

        // ReasonCodes and MissingEvidence as JSON arrays
        entity.Property(e => e.ReasonCodes)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<string[]>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.Property(e => e.MissingEvidence)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<string[]>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? []
            );

        entity.HasMany(e => e.Evidences).WithOne().HasForeignKey(e => e.VerificationId).IsRequired();
        entity.HasMany(e => e.Audits).WithOne().HasForeignKey(e => e.VerificationId).IsRequired();

        entity.HasIndex(e => e.ProductId).IsUnique();
        entity.HasIndex(e => e.ComplianceStatus);
    }

    private static void ConfigureVerificationEvidence(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VerificationEvidence>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.EvidenceType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Source).IsRequired().HasMaxLength(500);

        entity.HasIndex(e => e.VerificationId);
    }

    private static void ConfigureVerificationAudit(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VerificationAudit>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Actor).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Notes).HasMaxLength(500);

        entity.HasIndex(e => e.VerificationId);
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Order>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");
        entity.Property(e => e.ShippingAddress).HasMaxLength(1000);
        entity.Property(e => e.PaymentToken).HasMaxLength(500);

        entity.HasMany(e => e.VendorOrders).WithOne().HasForeignKey(e => e.OrderId).IsRequired();

        entity.HasIndex(e => e.CustomerId);
        entity.HasIndex(e => e.CreatedAt);
    }

    private static void ConfigureVendorOrder(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VendorOrder>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");
        entity.Property(e => e.TrackingNumber).HasMaxLength(100);

        entity.HasOne<Order>().WithMany(o => o.VendorOrders).HasForeignKey(e => e.OrderId).IsRequired();
        entity.HasOne<Vendor>().WithMany(v => v.VendorOrders).HasForeignKey(e => e.VendorId).IsRequired();
        entity.HasMany(e => e.Items).WithOne().HasForeignKey(e => e.VendorOrderId).IsRequired();

        entity.HasIndex(e => e.VendorId);
        entity.HasIndex(e => e.OrderId);
    }

    private static void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OrderItem>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");

        entity.HasOne<Product>().WithMany(p => p.OrderItems).HasForeignKey(e => e.ProductId).IsRequired();

        entity.HasIndex(e => e.VendorOrderId);
        entity.HasIndex(e => e.ProductId);
    }

    private static void ConfigureCartItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CartItem>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.HasOne<Product>().WithMany(p => p.CartItems).HasForeignKey(e => e.ProductId).IsRequired();

        entity.HasIndex(e => e.CustomerId);
        entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();
    }

    private static void ConfigureWishlistItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WishlistItem>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.HasOne<Product>().WithMany(p => p.WishlistItems).HasForeignKey(e => e.ProductId).IsRequired();

        entity.HasIndex(e => e.CustomerId);
        entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();
    }

    private static void ConfigureVendor(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Vendor>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Country).HasMaxLength(2);

        entity.HasIndex(e => e.Slug).IsUnique();
    }

    private static void ConfigureChainTxOutbox(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ChainTxOutbox>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.FunctionName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Target).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Data).IsRequired();
        entity.Property(e => e.Fingerprint).IsRequired().HasMaxLength(64);
        entity.Property(e => e.TxHash).IsRequired().HasMaxLength(66);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Error).HasMaxLength(1000);

        entity.HasIndex(e => e.Fingerprint).IsUnique();
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.SubmittedAt);
    }

    private static void ConfigureSupplierOnChain(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SupplierOnChain>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Wallet).IsRequired().HasMaxLength(66);
        entity.Property(e => e.IpfsMetadataCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Jurisdiction).IsRequired().HasMaxLength(2);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TxHash).HasMaxLength(66);

        entity.HasIndex(e => e.Wallet);
        entity.HasIndex(e => e.IndexedAt);
    }

    private static void ConfigureProductOnChain(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProductOnChain>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.MetadataHashHex).IsRequired().HasMaxLength(64);
        entity.Property(e => e.IpfsCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Jurisdiction).IsRequired().HasMaxLength(2);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TxHash).HasMaxLength(66);

        entity.HasIndex(e => e.ProductId).IsUnique();
        entity.HasIndex(e => e.IndexedAt);
    }

    private static void ConfigureCertificateOnChain(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CertificateOnChain>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Certifier).IsRequired().HasMaxLength(255);
        entity.Property(e => e.DocumentCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ScopeCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Country).IsRequired().HasMaxLength(2);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TxHash).HasMaxLength(66);

        entity.HasIndex(e => e.CertId);
        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.IndexedAt);
    }

    private static void ConfigureTraceabilityEventOnChain(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TraceabilityEventOnChain>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Actor).IsRequired().HasMaxLength(255);
        entity.Property(e => e.LocationCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.EvidenceCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.NotesCid).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TxHash).HasMaxLength(66);

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.BatchId);
        entity.HasIndex(e => e.TimestampUnix);
        entity.HasIndex(e => e.IndexedAt);
    }

    private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
        entity.Property(e => e.Payload).IsRequired();
        entity.Property(e => e.Error).HasMaxLength(1000);

        entity.HasIndex(e => e.ProcessedAt);
        entity.HasIndex(e => e.CreatedAt);
    }

    #endregion
}
