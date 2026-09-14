using HalalChain.Domain.Blockchain;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Common;
using HalalChain.Domain.Commerce;
using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;
using HalalChain.Platform.Api.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Category = HalalChain.Domain.Catalog.Category;

namespace HalalChain.Platform.Api.Persistence;

public sealed class HalalChainDbContext : DbContext
{
    public HalalChainDbContext(DbContextOptions<HalalChainDbContext> options) : base(options) { }

    // ── Legacy / core entities ──────────────────────────────────────
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Domain.Vendors.Vendor> Vendors => Set<Domain.Vendors.Vendor>();
    public DbSet<Domain.Catalog.Product> Products => Set<Domain.Catalog.Product>();
    public DbSet<Domain.Halal.Certificate> Certificates => Set<Domain.Halal.Certificate>();
    public DbSet<Domain.Halal.HalalVerification> HalalVerifications => Set<Domain.Halal.HalalVerification>();
    public DbSet<Domain.Halal.VerificationEvidence> VerificationEvidence => Set<Domain.Halal.VerificationEvidence>();
    public DbSet<Domain.Halal.VerificationAudit> VerificationAudits => Set<Domain.Halal.VerificationAudit>();
    public DbSet<Domain.Commerce.Order> Orders => Set<Domain.Commerce.Order>();
    public DbSet<Domain.Commerce.VendorOrder> VendorOrders => Set<Domain.Commerce.VendorOrder>();
    public DbSet<Domain.Commerce.OrderItem> OrderItems => Set<Domain.Commerce.OrderItem>();
    public DbSet<Domain.Commerce.CartItem> CartItems => Set<Domain.Commerce.CartItem>();
    public DbSet<Domain.Catalog.ProductEmbedding> ProductEmbeddings => Set<Domain.Catalog.ProductEmbedding>();
    public DbSet<Domain.Common.OutboxMessage> OutboxMessages => Set<Domain.Common.OutboxMessage>();

    // ── Redesigned taxonomy (Department → Category → Subcategory → ProductType) ──
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<TaxonomyCategory> TaxonomyCategories => Set<TaxonomyCategory>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();

    // ── Variants (first-class SKUs) ─────────────────────────────────
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    // ── Reference data ─────────────────────────────────────────────
    public DbSet<CertificationBody> CertificationBodies => Set<CertificationBody>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Country> Countries => Set<Country>();

    // ── Blockchain module (chain mirror + outbox) ────────────────
    public DbSet<Domain.Blockchain.ChainTxOutbox> ChainTxOutbox => Set<Domain.Blockchain.ChainTxOutbox>();
    public DbSet<Domain.Blockchain.SupplierOnChain> SuppliersOnChain => Set<Domain.Blockchain.SupplierOnChain>();
    public DbSet<Domain.Blockchain.ProductOnChain> ProductsOnChain => Set<Domain.Blockchain.ProductOnChain>();
    public DbSet<Domain.Blockchain.CertificateOnChain> CertificatesOnChain => Set<Domain.Blockchain.CertificateOnChain>();
    public DbSet<Domain.Blockchain.TraceabilityEventOnChain> TraceabilityEventsOnChain => Set<Domain.Blockchain.TraceabilityEventOnChain>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Provider-aware column types. PostgreSQL gets the optimal
        // types (jsonb, text[], GIN); SQLite (local dev fallback) gets
        // portable equivalents (text, no GIN). The model is identical
        // either way — queries and JSONB-in-C# access work everywhere.
        var isPg = Database.IsNpgsql();

        // ── Legacy Category (flat self-referential) ──────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasOne(c => c.Parent)
             .WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Vendor ────────────────────────────────────────────────────
        modelBuilder.Entity<Domain.Vendors.Vendor>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => v.Slug).IsUnique();
        });

        // ── Product (with new taxonomy FK + JSONB profile) ───────────
        modelBuilder.Entity<Domain.Catalog.Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Vendor)
             .WithMany(v => v.Products)
             .HasForeignKey(p => p.VendorId)
             .OnDelete(DeleteBehavior.Restrict);
            // New taxonomy FK (leaf-level classification)
            e.HasOne(p => p.ProductType)
             .WithMany()
             .HasForeignKey(p => p.ProductTypeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Brand)
             .WithMany()
             .HasForeignKey(p => p.BrandId)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(p => p.Price).HasPrecision(18, 2);
            // HalalProfile is a structured record. On PostgreSQL we use the
            // native jsonb column type (GIN-indexable). On SQLite we store it
            // as a JSON string via a value converter — the C# API is identical.
            if (isPg) e.Property(p => p.HalalProfile).HasColumnType("jsonb");
            else      e.Property(p => p.HalalProfile).HasConversion(HalalProfileJsonConverter.Instance);
            // Path-index for category browse
            e.HasIndex(p => new { p.ProductTypeId, p.Price });
            // GIN index on the JSONB profile (PostgreSQL only).
            if (isPg)
            {
                e.HasIndex(p => p.HalalProfile)
                 .HasMethod("gin")
                 .HasDatabaseName("IX_Products_HalalProfile_GIN");
            }
        });

        // ── ProductVariant ────────────────────────────────────────────
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => v.Sku).IsUnique();
            e.HasIndex(v => v.Barcode);
            e.HasOne<Product>()
             .WithMany(p => p.Variants)
             .HasForeignKey(v => v.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(v => v.Price).HasPrecision(18, 2);
            e.Property(v => v.CompareAtPrice).HasPrecision(18, 2);
            if (isPg) e.Property(v => v.WholesaleTiers).HasColumnType("jsonb");
            else      e.Property(v => v.WholesaleTiers).HasConversion(WholesaleTiersJsonConverter.Instance);
            // Value comparer so EF can detect mutations to the list.
            e.Property(v => v.WholesaleTiers).Metadata.SetValueComparer(WholesaleTiersValueComparer.Instance);
        });

        // ── Department ────────────────────────────────────────────────
        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Slug).IsUnique();
            e.HasIndex(d => d.SortOrder);
        });

        // ── Category (new 2nd-level taxonomy) ─────────────────────────
        modelBuilder.Entity<TaxonomyCategory>(e =>
        {
            e.ToTable("TaxonomyCategories");
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.DepartmentId, c.Slug }).IsUnique();
            e.HasIndex(c => c.Slug);
            e.HasOne(c => c.Department)
             .WithMany(d => d.Categories)
             .HasForeignKey(c => c.DepartmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Subcategory (3rd-level) ──────────────────────────────────
        modelBuilder.Entity<Subcategory>(e =>
        {
            e.ToTable("TaxonomySubcategories");
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.CategoryId, s.Slug }).IsUnique();
            e.HasIndex(s => s.Slug);
            e.HasOne(s => s.Category)
             .WithMany(c => c.Subcategories)
             .HasForeignKey(s => s.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductType (4th-level leaf) ─────────────────────────────
        modelBuilder.Entity<ProductType>(e =>
        {
            e.ToTable("TaxonomyProductTypes");
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.SubcategoryId, t.Slug }).IsUnique();
            e.HasIndex(t => t.Slug);
            e.HasIndex(t => t.PathSlug);
            e.HasOne(t => t.Subcategory)
             .WithMany(s => s.ProductTypes)
             .HasForeignKey(t => t.SubcategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Reference data ───────────────────────────────────────────
        modelBuilder.Entity<CertificationBody>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.Slug).IsUnique();
            e.HasIndex(b => b.Country);
        });

        modelBuilder.Entity<Facility>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.VendorId, f.Country });
        });

        modelBuilder.Entity<Brand>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.Slug).IsUnique();
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.HasKey(c => c.Iso2);
            e.HasIndex(c => c.Iso3).IsUnique();
            e.HasIndex(c => c.Name);
        });

        // ── Certificate ───────────────────────────────────────────────
        modelBuilder.Entity<Domain.Halal.Certificate>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.CertificateNumber).IsUnique();
            e.HasOne<Product>()
             .WithMany(p => p.Certificates)
             .HasForeignKey(c => c.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── HalalVerification ─────────────────────────────────────────
        modelBuilder.Entity<Domain.Halal.HalalVerification>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasOne<Product>()
             .WithMany(p => p.Verifications)
             .HasForeignKey(v => v.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(v => v.ReasonCodes).HasColumnType(isPg ? "text[]" : "text");
            e.Property(v => v.MissingEvidence).HasColumnType(isPg ? "text[]" : "text");
        });

        // ── VerificationEvidence ──────────────────────────────────────
        modelBuilder.Entity<Domain.Halal.VerificationEvidence>(e =>
        {
            e.HasKey(ve => ve.Id);
            e.HasOne<Domain.Halal.HalalVerification>()
             .WithMany(v => v.Evidences)
             .HasForeignKey(ve => ve.VerificationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── VerificationAudit ─────────────────────────────────────────
        modelBuilder.Entity<Domain.Halal.VerificationAudit>(e =>
        {
            e.HasKey(va => va.Id);
            e.HasOne<Domain.Halal.HalalVerification>()
             .WithMany(v => v.Audits)
             .HasForeignKey(va => va.VerificationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Order ─────────────────────────────────────────────────────
        modelBuilder.Entity<Domain.Commerce.Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.CustomerId);
            e.Property(o => o.Total).HasPrecision(18, 2);
        });

        // ── VendorOrder ───────────────────────────────────────────────
        modelBuilder.Entity<Domain.Commerce.VendorOrder>(e =>
        {
            e.HasKey(vo => vo.Id);
            e.HasOne(vo => vo.Order)
             .WithMany(o => o.VendorOrders)
             .HasForeignKey(vo => vo.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(vo => vo.Vendor)
             .WithMany(v => v.VendorOrders)
             .HasForeignKey(vo => vo.VendorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(vo => vo.Subtotal).HasPrecision(18, 2);
        });

        // ── OrderItem ─────────────────────────────────────────────────
        modelBuilder.Entity<Domain.Commerce.OrderItem>(e =>
        {
            e.HasKey(oi => oi.Id);
            e.HasOne(oi => oi.VendorOrder)
             .WithMany(vo => vo.Items)
             .HasForeignKey(oi => oi.VendorOrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.Product)
             .WithMany(p => p.OrderItems)
             .HasForeignKey(oi => oi.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        });

        // ── CartItem ──────────────────────────────────────────────────
        modelBuilder.Entity<Domain.Commerce.CartItem>(e =>
        {
            e.HasKey(ci => ci.Id);
            e.HasIndex(ci => new { ci.CustomerId, ci.ProductId }).IsUnique();
            e.HasOne(ci => ci.Product)
             .WithMany(p => p.CartItems)
             .HasForeignKey(ci => ci.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductEmbedding ──────────────────────────────────────────
        modelBuilder.Entity<Domain.Catalog.ProductEmbedding>(e =>
        {
            e.HasKey(pe => pe.Id);
            e.HasIndex(pe => pe.ProductId).IsUnique();
            // FK to Product without a navigation property on the Domain entity
            // (Product remains in the API persistence layer for now).
            e.HasOne<Domain.Catalog.Product>()
             .WithMany()
             .HasForeignKey(pe => pe.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(pe => pe.Embedding).HasColumnType(isPg ? "real[]" : "text");
        });

        // ── OutboxMessage ─────────────────────────────────────────────
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(om => om.Id);
            e.HasIndex(om => new { om.ProcessedAt, om.CreatedAtUtc })
             .HasDatabaseName("IX_OutboxMessages_Pending");
            e.Property(om => om.EventType).HasMaxLength(256);
            e.Property(om => om.Payload).HasColumnType("text");
        });

        // ── Blockchain: chain-tx outbox ────────────────────────────
        modelBuilder.Entity<Domain.Blockchain.ChainTxOutbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Fingerprint);
            e.HasIndex(x => new { x.Status, x.SubmittedAt });
            e.HasIndex(x => x.TxHash);
        });

        // ── Blockchain: on-chain mirror tables (append-only indexer) ──
        modelBuilder.Entity<Domain.Blockchain.SupplierOnChain>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SupplierId).IsUnique();
        });
        modelBuilder.Entity<Domain.Blockchain.ProductOnChain>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId).IsUnique();
            e.HasIndex(x => x.SupplierId);
        });
        modelBuilder.Entity<Domain.Blockchain.CertificateOnChain>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CertId).IsUnique();
            e.HasIndex(x => x.ProductId);
        });
        modelBuilder.Entity<Domain.Blockchain.TraceabilityEventOnChain>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProductId, x.TimestampUnix });
        });

        HalalChainSeed.Seed(modelBuilder);
        TaxonomySeed.Seed(modelBuilder);
    }
}

// ── Value converters for SQLite (PostgreSQL uses native jsonb) ───────

/// <summary>Serializes a <see cref="HalalProfile"/> record to a JSON
/// string for storage in SQLite TEXT. On PostgreSQL this is bypassed
/// because the native jsonb type handles it directly.</summary>
internal sealed class HalalProfileJsonConverter : ValueConverter<HalalProfile?, string>
{
    public static readonly HalalProfileJsonConverter Instance = new();
    private static readonly System.Text.Json.JsonSerializerOptions Json = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    public HalalProfileJsonConverter()
        : base(v => v == null ? "" : System.Text.Json.JsonSerializer.Serialize(v, Json),
               s => string.IsNullOrEmpty(s) ? null : System.Text.Json.JsonSerializer.Deserialize<HalalProfile>(s, Json)) { }
}

/// <summary>Same idea for the wholesale price tier list on a variant.</summary>
internal sealed class WholesaleTiersJsonConverter : ValueConverter<List<WholesalePriceTier>, string>
{
    public static readonly WholesaleTiersJsonConverter Instance = new();
    private static readonly System.Text.Json.JsonSerializerOptions Json = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    public WholesaleTiersJsonConverter()
        : base(v => v == null ? "[]" : System.Text.Json.JsonSerializer.Serialize(v, Json),
               s => string.IsNullOrEmpty(s) ? new List<WholesalePriceTier>() : System.Text.Json.JsonSerializer.Deserialize<List<WholesalePriceTier>>(s, Json)!) { }
}

/// <summary>EF Core value comparer for the wholesale tier list — lets the
/// change tracker detect additions/removals/edits to the list contents.</summary>
internal sealed class WholesaleTiersValueComparer : Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<WholesalePriceTier>>
{
    public static readonly WholesaleTiersValueComparer Instance = new();
    public WholesaleTiersValueComparer() : base(
        (a, b) => a!.SequenceEqual(b!),
        a => a.Aggregate(0, (h, t) => HashCode.Combine(h, t.MinQuantity, t.UnitPrice, t.Currency)),
        a => a.ToList()) { }
}
