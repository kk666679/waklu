using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HalalChain.Platform.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    LogoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificatesOnChain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CertId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    Certifier = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentCid = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    ScopeCid = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", nullable: false),
                    BlockNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificatesOnChain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificationBodies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Acronym = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TrustTier = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationBodies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChainTxOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FunctionName = table.Column<string>(type: "TEXT", nullable: false),
                    Target = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    BlockNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainTxOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Iso2 = table.Column<string>(type: "TEXT", nullable: false),
                    Iso3 = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Iso2);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconClass = table.Column<string>(type: "TEXT", nullable: true),
                    HeroImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    CertificationBodyIds = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    ShippingAddress = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentToken = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductsOnChain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", nullable: false),
                    MetadataHashHex = table.Column<string>(type: "TEXT", nullable: false),
                    IpfsCid = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentCertId = table.Column<string>(type: "TEXT", nullable: false),
                    RegisteredAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", nullable: false),
                    BlockNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductsOnChain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuppliersOnChain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", nullable: false),
                    Wallet = table.Column<string>(type: "TEXT", nullable: false),
                    IpfsMetadataCid = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: false),
                    RegisteredAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", nullable: false),
                    BlockNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuppliersOnChain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraceabilityEventsOnChain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    BatchId = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationCid = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceCid = table.Column<string>(type: "TEXT", nullable: false),
                    NotesCid = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", nullable: false),
                    BlockNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    LogIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceabilityEventsOnChain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconClass = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomyCategories_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomySubcategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomySubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomySubcategories_TaxonomyCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TaxonomyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyProductTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubcategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PathSlug = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyProductTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxonomyProductTypes_TaxonomySubcategories_SubcategoryId",
                        column: x => x.SubcategoryId,
                        principalTable: "TaxonomySubcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProductTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BrandId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Inventory = table.Column<int>(type: "INTEGER", nullable: false),
                    HalalProfile = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_TaxonomyProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "TaxonomyProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CertificateNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CertificationBody = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_VendorOrders_VendorOrderId",
                        column: x => x.VendorOrderId,
                        principalTable: "VendorOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Embedding = table.Column<string>(type: "text", nullable: false),
                    Dimension = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEmbeddings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    VariantAttributes = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    NetQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    NetUnit = table.Column<string>(type: "TEXT", nullable: true),
                    WholesaleTiers = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HalalVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComplianceStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCodes = table.Column<string>(type: "text", nullable: false),
                    MissingEvidence = table.Column<string>(type: "text", nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HalalVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HalalVerifications_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HalalVerifications_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VerificationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationAudits_HalalVerifications_VerificationId",
                        column: x => x.VerificationId,
                        principalTable: "HalalVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VerificationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvidenceType = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationEvidence_HalalVerifications_VerificationId",
                        column: x => x.VerificationId,
                        principalTable: "HalalVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "CountryOfOrigin", "CreatedAt", "Description", "LogoUrl", "Name", "Slug", "WebsiteUrl" },
                values: new object[,]
                {
                    { new Guid("10000020-0000-0000-0000-000000000001"), "MY", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(6436), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Selera Masak", "selera-masak", null },
                    { new Guid("10000020-0000-0000-0000-000000000002"), "MY", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7852), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Al-Barakah Foods", "al-barakah", null },
                    { new Guid("10000020-0000-0000-0000-000000000003"), "ID", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7857), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Nusantara Herbal", "nusantara", null },
                    { new Guid("10000020-0000-0000-0000-000000000004"), "SG", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7859), new TimeSpan(0, 0, 0, 0, 0)), null, null, "PureBite SG", "purebite", null },
                    { new Guid("10000020-0000-0000-0000-000000000005"), "AE", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 701, DateTimeKind.Unspecified).AddTicks(7861), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Gulf Halal Trading", "gulf-halal", null }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "ParentId", "Slug" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Food & Beverages", null, "food-beverages" },
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Cosmetics", null, "cosmetics" },
                    { new Guid("30000000-0000-0000-0000-000000000001"), "Supplements", null, "supplements" }
                });

            migrationBuilder.InsertData(
                table: "CertificationBodies",
                columns: new[] { "Id", "Acronym", "Country", "Description", "IsActive", "LogoUrl", "Name", "Slug", "TrustTier", "WebsiteUrl" },
                values: new object[,]
                {
                    { new Guid("10000010-0000-0000-0000-000000000001"), "JAKIM", "MY", "Malaysia's federal halal authority; widely recognised across ASEAN, MENA, and East Asia.", true, null, "Department of Islamic Development Malaysia (JAKIM)", "jakim", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000002"), "MUI", "ID", "Indonesia's national halal authority; largest Muslim population globally.", true, null, "Majelis Ulama Indonesia (MUI)", "mui", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000003"), "ESMA", "AE", "UAE federal authority; recognised across the Gulf and increasingly internationally.", true, null, "Emirates Authority for Standardisation (ESMA)", "esma", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000004"), "SFDA", "SA", "Saudi Arabia's federal authority; the most widely accepted standard in MENA.", true, null, "Saudi Food and Drug Authority (SFDA)", "sfda", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000005"), "GAC", "AE", "GCC-wide accreditation body for halal conformity assessment.", true, null, "Gulf Accreditation Center (GAC)", "gac", 4, null },
                    { new Guid("10000010-0000-0000-0000-000000000006"), "MUIS", "SG", "Majlis Ugama Islam Singapura; Singapore's official halal authority.", true, null, "Singapore MUIS", "muis", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000007"), "BHB", "BN", "Brunei's national halal certification, run under the Ministry of Religious Affairs.", true, null, "Brunei Halal Brand", "brunei-halal", 3, null },
                    { new Guid("10000010-0000-0000-0000-000000000008"), "CICOT", "TH", "Thailand's recognised halal certifier.", true, null, "Thailand Central Islamic Council of Thailand (CICOT)", "cicot", 2, null },
                    { new Guid("10000010-0000-0000-0000-000000000009"), "PHA", "PK", "Pakistan's federal halal authority.", true, null, "Pakistan Halal Authority", "pha", 2, null },
                    { new Guid("10000010-0000-0000-0000-00000000000a"), "TSE", "TR", "Türkiye's national standards body; halal certification arm.", true, null, "Turkish Standards Institute (TSE)", "tse", 2, null }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Iso2", "IsActive", "Iso3", "Name", "Region" },
                values: new object[,]
                {
                    { "AE", true, "ARE", "United Arab Emirates", "Middle East" },
                    { "BD", true, "BGD", "Bangladesh", "South Asia" },
                    { "BH", true, "BHR", "Bahrain", "Middle East" },
                    { "BN", true, "BRN", "Brunei", "Southeast Asia" },
                    { "GB", true, "GBR", "United Kingdom", "Europe" },
                    { "ID", true, "IDN", "Indonesia", "Southeast Asia" },
                    { "KW", true, "KWT", "Kuwait", "Middle East" },
                    { "MY", true, "MYS", "Malaysia", "Southeast Asia" },
                    { "OM", true, "OMN", "Oman", "Middle East" },
                    { "PH", true, "PHL", "Philippines", "Southeast Asia" },
                    { "PK", true, "PAK", "Pakistan", "South Asia" },
                    { "QA", true, "QAT", "Qatar", "Middle East" },
                    { "SA", true, "SAU", "Saudi Arabia", "Middle East" },
                    { "SG", true, "SGP", "Singapore", "Southeast Asia" },
                    { "TH", true, "THA", "Thailand", "Southeast Asia" },
                    { "TR", true, "TUR", "Türkiye", "Europe" },
                    { "US", true, "USA", "United States", "North America" },
                    { "VN", true, "VNM", "Vietnam", "Southeast Asia" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Description", "HeroImageUrl", "IconClass", "IsActive", "Name", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), "Halal-certified food and drink across all categories.", null, "fa-utensils", true, "Food & Beverage", "food-beverage", 1 },
                    { new Guid("390bfdd0-ff24-8f62-7b2b-ebb20ad21fee"), "Halal-verified vitamins, supplements, herbal products, healthcare, and specialty nutrition.", null, "fa-heart-pulse", true, "Health, Wellness & Nutrition", "health-wellness", 3 },
                    { new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), "Modest clothing, hijab, footwear, and accessories for the whole family.", null, "fa-shirt", true, "Modest Fashion & Lifestyle", "modest-fashion", 5 },
                    { new Guid("51ab50d0-42e0-4f38-0f26-4db5e178c113"), "Halal-conscious baby care, mother & maternity products, and feeding essentials.", null, "fa-baby-carriage", true, "Baby, Mother & Maternity", "baby-mother", 7 },
                    { new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), "Halal-certified skincare, haircare, fragrance, bath & body, makeup.", null, "fa-spray-can-sparkles", true, "Personal Care & Beauty", "personal-care", 2 },
                    { new Guid("bf7352d0-5093-ff97-60aa-83b3403bc877"), "Halal-conscious home care, kitchen, and home fragrance.", null, "fa-house", true, "Household & Home", "household", 4 },
                    { new Guid("cb43fbd0-f271-df02-2aa8-b5b4ab0e198a"), "Qurans, prayer, Islamic books, and religious gifts.", null, "fa-mosque", true, "Islamic & Religious Essentials", "islamic-religious", 6 },
                    { new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), "Bulk halal ingredients, OEM, private label, and HoReCa supplies. Visible to verified business accounts.", null, "fa-building", true, "Business, B2B & Wholesale", "b2b", 8 }
                });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Country", "CreatedAt", "Name", "Slug", "Status" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), "MY", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 694, DateTimeKind.Unspecified).AddTicks(9403), new TimeSpan(0, 0, 0, 0, 0)), "Selera Masak", "selera-masak", "Active" },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), "MY", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2550), new TimeSpan(0, 0, 0, 0, 0)), "Al-Barakah Foods", "al-barakah", "Active" },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), "ID", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2559), new TimeSpan(0, 0, 0, 0, 0)), "Nusantara Herbal", "nusantara", "Active" },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), "SG", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2593), new TimeSpan(0, 0, 0, 0, 0)), "PureBite SG", "purebite", "Active" },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), "AE", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(2596), new TimeSpan(0, 0, 0, 0, 0)), "Gulf Halal Trading", "gulf-halal", "Active" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "ParentId", "Slug" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Snacks", new Guid("10000000-0000-0000-0000-000000000001"), "snacks" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Beverages", new Guid("10000000-0000-0000-0000-000000000001"), "beverages" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Meat & Poultry", new Guid("10000000-0000-0000-0000-000000000001"), "meat-poultry" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Spices & Condiments", new Guid("10000000-0000-0000-0000-000000000001"), "spices-condiments" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Bakery", new Guid("10000000-0000-0000-0000-000000000001"), "bakery" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "Skincare", new Guid("20000000-0000-0000-0000-000000000001"), "skincare" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Haircare", new Guid("20000000-0000-0000-0000-000000000001"), "haircare" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BrandId", "CategoryId", "CreatedAt", "Currency", "Description", "HalalProfile", "Inventory", "Origin", "Price", "ProductTypeId", "Slug", "Title", "UpdatedAt", "VendorId" },
                values: new object[,]
                {
                    { new Guid("b0500000-0000-0000-0000-000000000000"), null, new Guid("30000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Forest honey 500ml", null, 100, "Malaysia", 45.00m, null, "tualang-honey", "Tualang Honey", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4713), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0510000-0000-0000-0000-000000000000"), null, new Guid("30000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Black seed capsules 60pcs", null, 150, "UAE", 55.00m, null, "habbatus-sauda", "Habbatus Sauda", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4718), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0520000-0000-0000-0000-000000000000"), null, new Guid("30000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Premium dates 1kg", null, 200, "UAE", 65.00m, null, "medjool-dates", "Medjool Dates", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4723), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") }
                });

            migrationBuilder.InsertData(
                table: "TaxonomyCategories",
                columns: new[] { "Id", "DepartmentId", "Description", "IconClass", "IsActive", "Name", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("002e05d1-7638-458d-e254-8ff7f0ccedff"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-boxes-stacked", true, "Wholesale & Bulk", "wholesale", 5 },
                    { new Guid("0a6b3fd1-b495-80f7-3678-540701d86355"), new Guid("390bfdd0-ff24-8f62-7b2b-ebb20ad21fee"), null, "fa-briefcase-medical", true, "Healthcare & First Aid", "healthcare", 3 },
                    { new Guid("157bb9d1-f805-55fa-247c-1e2e4b77c2f3"), new Guid("cb43fbd0-f271-df02-2aa8-b5b4ab0e198a"), null, "fa-person-praying", true, "Prayer", "prayer", 2 },
                    { new Guid("16161bd1-57cd-1e79-65ea-e744dc9aa190"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-tag", true, "Private Label & OEM", "oem", 4 },
                    { new Guid("240017d1-d430-68f8-003b-267c83207c31"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-bottle-droplet", true, "Cooking Oils & Fats", "oils-fats", 11 },
                    { new Guid("243bc2d1-b166-c1f3-a6e4-08971996f916"), new Guid("bf7352d0-5093-ff97-60aa-83b3403bc877"), null, "fa-bed", true, "Home Essentials", "home-essentials", 4 },
                    { new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-cookie-bite", true, "Snacks & Confectionery", "snacks-confectionery", 8 },
                    { new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-snowflake", true, "Frozen Food", "frozen-food", 6 },
                    { new Guid("4d20bfd1-fcf3-d988-9b34-c8190b544ec1"), new Guid("cb43fbd0-f271-df02-2aa8-b5b4ab0e198a"), null, "fa-gift", true, "Islamic Gifts & Decor", "islamic-gifts", 4 },
                    { new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-drumstick-bite", true, "Meat & Poultry", "meat-poultry", 1 },
                    { new Guid("60ad95d1-8b0b-4387-83f1-d9143288a8c7"), new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), null, "fa-person-dress", true, "Women's Clothing", "womens-clothing", 1 },
                    { new Guid("696da2d1-169d-50b5-bc90-059ce9d611ac"), new Guid("51ab50d0-42e0-4f38-0f26-4db5e178c113"), null, "fa-person-pregnant", true, "Mother & Maternity", "maternity", 3 },
                    { new Guid("6a76dbd1-da33-1b7b-f49d-3f48045d5458"), new Guid("bf7352d0-5093-ff97-60aa-83b3403bc877"), null, "fa-spray-can-sparkles", true, "Household Cleaning", "cleaning", 1 },
                    { new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-flask", true, "Halal Ingredients", "halal-ingredients", 1 },
                    { new Guid("736014d1-c356-3bf6-7f54-42a42906bed9"), new Guid("51ab50d0-42e0-4f38-0f26-4db5e178c113"), null, "fa-baby", true, "Baby Care", "baby-care", 2 },
                    { new Guid("8003c1d1-0e9e-8de5-6e25-3faef604486e"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-paw", true, "Pet & Animal (B2B)", "pet-animal-b2b", 6 },
                    { new Guid("805073d1-be3e-f1f1-38b9-1b0b167dbb2b"), new Guid("cb43fbd0-f271-df02-2aa8-b5b4ab0e198a"), null, "fa-book-quran", true, "Qurans & Quran Accessories", "quran", 1 },
                    { new Guid("82808bd1-9157-9527-a8f6-65aa24aebd86"), new Guid("cb43fbd0-f271-df02-2aa8-b5b4ab0e198a"), null, "fa-book", true, "Islamic Books & Education", "islamic-books", 3 },
                    { new Guid("94cf87d1-7cef-b1c5-b234-63e8373f86bc"), new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), null, "fa-shoe-prints", true, "Footwear", "footwear", 5 },
                    { new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-egg", true, "Dairy & Eggs", "dairy-eggs", 3 },
                    { new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-mug-saucer", true, "Beverages", "beverages", 12 },
                    { new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-fish", true, "Seafood", "seafood", 2 },
                    { new Guid("9f1544d1-be54-86c4-06c9-bf6ee46ffa11"), new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), null, "fa-children", true, "Children's Clothing", "kids-clothing", 3 },
                    { new Guid("9f5922d1-d4b9-11e6-4be3-db69644ef876"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-baby", true, "Baby & Children's Food", "baby-food", 13 },
                    { new Guid("9f8913d1-a125-61d0-58d7-44f34abb7df2"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-spray-can", true, "Haircare", "haircare", 2 },
                    { new Guid("aa7117d1-c237-0781-c035-70e0f0bfcfe5"), new Guid("bf7352d0-5093-ff97-60aa-83b3403bc877"), null, "fa-spray-can", true, "Home Fragrance", "home-fragrance", 2 },
                    { new Guid("af579cd1-320a-9581-39fa-cca82497ccfb"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-wind", true, "Fragrance", "fragrance", 4 },
                    { new Guid("b51bafd1-502e-06bc-3bfc-0104c14fb0aa"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-tooth", true, "Oral Care", "oral-care", 6 },
                    { new Guid("b74b50d1-1a20-cf97-d715-83e37952b37f"), new Guid("bf7352d0-5093-ff97-60aa-83b3403bc877"), null, "fa-kitchen-set", true, "Kitchen", "kitchen", 3 },
                    { new Guid("b964a0d1-df83-1f56-a5ff-635a5838be16"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-flask-vial", true, "Cosmetic Ingredients", "cosmetic-ingredients", 2 },
                    { new Guid("be62fed1-3091-5fd1-dfa1-7813338e640a"), new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), null, "fa-person", true, "Men's Clothing", "mens-clothing", 2 },
                    { new Guid("bf9802d1-f4c4-4bf1-a0a9-c9269e9b99e4"), new Guid("51ab50d0-42e0-4f38-0f26-4db5e178c113"), null, "fa-jar", true, "Baby Food", "baby-food-d7", 1 },
                    { new Guid("cebc7bd1-af4f-b20d-1cef-93581c822578"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-paintbrush", true, "Cosmetics & Makeup", "cosmetics-makeup", 3 },
                    { new Guid("d14dcad1-3c2e-f436-d0a7-378010d32869"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-bowl-food", true, "Ready-to-Eat & Prepared", "ready-to-eat", 7 },
                    { new Guid("d8721ad1-c9c4-26d1-bca5-b4e506d9cf4a"), new Guid("d713a4d0-9350-c06d-f4a5-e6b617e16a9c"), null, "fa-utensils", true, "HoReCa & Food Service", "horeca", 3 },
                    { new Guid("da3754d1-9038-f673-3bc0-60e175e95158"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-face-smile", true, "Skincare", "skincare", 1 },
                    { new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-wheat-awn", true, "Rice, Grains & Staples", "rice-grains-staples", 4 },
                    { new Guid("ebaae6d1-ee50-4fb6-c894-9dd0c1d2a201"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-bread-slice", true, "Bakery", "bakery", 9 },
                    { new Guid("ec26efd1-0078-4c83-503e-d247092983dd"), new Guid("390bfdd0-ff24-8f62-7b2b-ebb20ad21fee"), null, "fa-wheat-awn-circle-check", true, "Specialty Nutrition", "specialty-nutrition", 4 },
                    { new Guid("ee0d88d1-6d91-3eee-d1cc-f53b47b2a5b1"), new Guid("390bfdd0-ff24-8f62-7b2b-ebb20ad21fee"), null, "fa-pills", true, "Vitamins & Supplements", "vitamins", 1 },
                    { new Guid("efc9f0d1-3f41-9b5f-1ee5-1df2ec7e840f"), new Guid("b3a3a9d0-afb4-1f2c-97ac-52b1d5697764"), null, "fa-shower", true, "Bath & Body", "bath-body", 5 },
                    { new Guid("f134e7d1-84d6-ee6e-dfb5-d866a541ae8f"), new Guid("45dba6d0-a102-6fcd-4529-1cb376a57000"), null, "fa-bag-shopping", true, "Accessories", "fashion-accessories", 4 },
                    { new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-apple-whole", true, "Fresh Produce", "fresh-produce", 5 },
                    { new Guid("f2fdb5d1-a2e2-7946-b0c1-375d21085ae3"), new Guid("2d3b54d0-5e45-aef7-b22e-b9b09fffcedb"), null, "fa-pepper-hot", true, "Sauces, Spices & Condiments", "sauces-spices", 10 },
                    { new Guid("f4568cd1-945e-1503-1684-a4d502c9b4f4"), new Guid("51ab50d0-42e0-4f38-0f26-4db5e178c113"), null, "fa-baby", true, "Baby Essentials", "baby-essentials", 4 },
                    { new Guid("f4e11bd1-46a5-f278-af11-08e15df18d79"), new Guid("390bfdd0-ff24-8f62-7b2b-ebb20ad21fee"), null, "fa-mortar-pestle", true, "Herbal & Traditional", "herbal-traditional", 2 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BrandId", "CategoryId", "CreatedAt", "Currency", "Description", "HalalProfile", "Inventory", "Origin", "Price", "ProductTypeId", "Slug", "Title", "UpdatedAt", "VendorId" },
                values: new object[,]
                {
                    { new Guid("b0010000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Traditional fish cracker", null, 500, "Malaysia", 8.50m, null, "keropok-lekor", "Keropok Lekor Original", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 695, DateTimeKind.Unspecified).AddTicks(8703), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0020000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Spicy variant with chili", null, 300, "Malaysia", 9.00m, null, "keropok-spicy", "Keropok Lekor Spicy", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4305), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0030000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Crispy Indian ring snack", null, 800, "Malaysia", 6.50m, null, "murukku", "Murukku Masala", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4384), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0040000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Layered cake with coconut milk", null, 120, "Malaysia", 15.00m, null, "kueh-lapis", "Kueh Lapis Premium", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4392), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0050000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Crunchy fermented soybean chips", null, 600, "Indonesia", 7.00m, null, "tempeh-chips", "Tempeh Chips", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4397), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0060000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Rice cracker with shrimp", null, 400, "Indonesia", 5.50m, null, "rengginang", "Rengginang Udang", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4409), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0070000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Crispy tofu snack", null, 350, "Singapore", 4.50m, null, "tau-huay", "Tau Huay Crisps", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4416), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0080000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Pistachio and walnut baklava", null, 100, "UAE", 28.00m, null, "baklava", "Baklava Assorted", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4421), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0090000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Savory snack mix", null, 200, "UAE", 12.00m, null, "namkeen", "Namkeen Mix", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4426), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0100000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Peanut brittle with palm sugar", null, 150, "Malaysia", 11.00m, null, "peanut-brittle", "Peanut Brittle", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4434), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0110000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Instant teh tarik 10 sachets", null, 400, "Malaysia", 12.00m, null, "teh-tarik", "Teh Tarik Concentrate", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4441), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0120000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Black coffee 15 packs", null, 350, "Malaysia", 14.00m, null, "kopi-o", "Kopi O Kosong", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4446), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0130000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Rose syrup 500ml", null, 250, "Malaysia", 8.00m, null, "sirap-bandung", "Sirap Bandung", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4451), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0140000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Turmeric tamarind drink", null, 300, "Indonesia", 9.50m, null, "kunyit-asam", "Kunyit Asam", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4474), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0150000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Pure coconut water 1L", null, 500, "Singapore", 5.50m, null, "coconut-water", "Coconut Water", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4480), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0160000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Arabic coffee cardamom 250g", null, 80, "UAE", 35.00m, null, "qahwa", "Qahwa Arabic Coffee", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4485), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0170000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Rose water beverage 750ml", null, 120, "UAE", 18.00m, null, "rose-water", "Rose Water Drink", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4491), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0180000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Rose milk mix", null, 200, "Malaysia", 10.00m, null, "bandung", "Bandung Concentrate", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4499), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0190000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Village chicken 1.5kg", null, 100, "Malaysia", 28.00m, null, "ayam-kampung", "Ayam Kampung", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4505), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0200000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Goat meat 500g", null, 80, "Malaysia", 42.00m, null, "daging-kambing", "Daging Kambing", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4510), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0210000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Chicken satay 10pcs", null, 200, "Malaysia", 18.00m, null, "sate-ayam", "Sate Ayam", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4516), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0220000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Beef rendang 400g", null, 150, "Malaysia", 25.00m, null, "rendang-daging", "Rendang Daging", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4521), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0230000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Halal nuggets 500g", null, 300, "Singapore", 8.50m, null, "chicken-nugget", "Chicken Nugget", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4526), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0240000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Halal lamb mince 400g", null, 60, "UAE", 45.00m, null, "lamb-mince", "Lamb Mince", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4531), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0250000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Beef sausage 10pcs", null, 250, "Indonesia", 35000m, null, "sosis-sapi", "Sosis Sapi", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4558), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0260000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Grilled fish cake 12pcs", null, 180, "Malaysia", 16.00m, null, "otak-otak", "Otak-Otak", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4564), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0270000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Rendang spice paste 200g", null, 400, "Malaysia", 12.00m, null, "rendang-paste", "Rendang Paste", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4569), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0280000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Chili sauce 250ml", null, 300, "Indonesia", 18000m, null, "sambal-terasi", "Sambal Terasi", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4574), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0290000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Curry powder 100g", null, 500, "Malaysia", 7.50m, null, "kari-ayam", "Kari Ayam Powder", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4590), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0300000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Organic turmeric 150g", null, 250, "Singapore", 6.00m, null, "turmeric", "Turmeric Ground", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4596), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0310000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Herb blend 120g", null, 180, "UAE", 22.00m, null, "zaatar", "Zaatar Blend", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4601), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0320000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Ground black pepper 100g", null, 600, "Malaysia", 9.00m, null, "black-pepper", "Black Pepper", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4606), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0330000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Dried curry leaves 30g", null, 400, "Singapore", 4.50m, null, "curry-leaf", "Curry Leaf", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4612), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0340000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Fried rice paste 150g", null, 350, "Indonesia", 12000m, null, "bumbu-nasi", "Bumbu Nasi Goreng", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4619), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0350000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Frozen roti canai 8pcs", null, 200, "Malaysia", 10.00m, null, "roti-canai", "Roti Canai", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4625), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0360000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Garlic butter naan 6pcs", null, 150, "Malaysia", 11.00m, null, "naan-garlic", "Naan Garlic", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4631), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0370000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Wholemeal bread 400g", null, 300, "Singapore", 4.20m, null, "wholemeal-bread", "Wholemeal Bread", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4636), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0380000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Malay sponge cake 12pcs", null, 100, "Malaysia", 13.00m, null, "kuih-bahulu", "Kuih Bahulu", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4641), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0390000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Blueberry muffins 4pcs", null, 180, "Singapore", 7.50m, null, "muffin", "Blueberry Muffin", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4647), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0400000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Date cookies 250g", null, 90, "UAE", 25.00m, null, "date-cookies", "Date Cookies", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4652), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0410000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Durian pia 6pcs", null, 120, "Malaysia", 18.00m, null, "pia-durian", "Pia Durian", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4657), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0420000-0000-0000-0000-000000000000"), null, new Guid("10000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Sweet pancake frozen", null, 80, "Indonesia", 25000m, null, "martabak", "Martabak Manis", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4663), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0430000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SGD", "Halal Vitamin C 30ml", null, 200, "Singapore", 32.00m, null, "vitamin-c", "Vitamin C Serum", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4675), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("b0440000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "IDR", "Face mask 10 sheets", null, 150, "Indonesia", 45000m, null, "spirulina-mask", "Spirulina Mask", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4681), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("b0450000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Aloe vera cream 100ml", null, 250, "Malaysia", 22.00m, null, "aloe-moisturizer", "Aloe Moisturizer", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4686), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("b0460000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Rose water toner 200ml", null, 180, "UAE", 28.00m, null, "rose-toner", "Rose Toner", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4692), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0470000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Palm oil shampoo 400ml", null, 300, "Malaysia", 15.00m, null, "palm-shampoo", "Palm Shampoo", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4698), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("b0480000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AED", "Argan oil treatment 100ml", null, 120, "UAE", 35.00m, null, "argan-oil", "Argan Hair Oil", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4703), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000005") },
                    { new Guid("b0490000-0000-0000-0000-000000000000"), null, new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MYR", "Lightweight conditioner 300ml", null, 200, "Malaysia", 18.00m, null, "hijab-cond", "Hijab Conditioner", new DateTimeOffset(new DateTime(2026, 8, 26, 19, 15, 12, 696, DateTimeKind.Unspecified).AddTicks(4708), new TimeSpan(0, 0, 0, 0, 0)), new Guid("a0000000-0000-0000-0000-000000000002") }
                });

            migrationBuilder.InsertData(
                table: "TaxonomySubcategories",
                columns: new[] { "Id", "CategoryId", "Description", "IsActive", "Name", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("034140d2-ae78-36e1-1f4a-1baecde863f8"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Eggs", "eggs", 22 },
                    { new Guid("036c3ad2-b97c-4bcb-0ece-e1be8d0bb0df"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Cereals & Muesli", "cereals", 28 },
                    { new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891"), new Guid("cebc7bd1-af4f-b20d-1cef-93581c822578"), null, true, "Face", "face", 78 },
                    { new Guid("058e65d2-fcd1-59a1-12be-14e797000487"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Chocolate", "chocolate", 50 },
                    { new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Cheese", "cheese", 16 },
                    { new Guid("08a7a9d2-8e75-3cda-513e-9cc91b3b6c3e"), new Guid("f4e11bd1-46a5-f278-af11-08e15df18d79"), null, true, "Essential oils & extracts", "essential-oils", 93 },
                    { new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Chicken", "chicken", 4 },
                    { new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Pasta & Noodles", "pasta-noodles", 27 },
                    { new Guid("097a78d2-4e6b-9f77-9178-022420d06d74"), new Guid("b964a0d1-df83-1f56-a5ff-635a5838be16"), null, true, "Oils & butters", "oils-butters", 152 },
                    { new Guid("0e5eced2-ca03-32b6-7af0-33c79642edfb"), new Guid("696da2d1-169d-50b5-bc90-059ce9d611ac"), null, true, "Postpartum care", "postpartum", 143 },
                    { new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Bottled water & hydration", "water", 63 },
                    { new Guid("148f29d2-a1da-5ee6-c4cc-e7af1ea771a9"), new Guid("b74b50d1-1a20-cf97-d715-83e37952b37f"), null, true, "Bakeware", "bakeware", 106 },
                    { new Guid("15119fd2-bb73-0f1c-212d-ef653e83fa23"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen meat", "frozen-meat", 36 },
                    { new Guid("1643ced2-bd0c-1ddd-e61b-57c9b963115e"), new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), null, true, "Meat ingredients", "meat-ingredients", 147 },
                    { new Guid("165b72d2-7a36-a783-4101-1a1dd081e0f6"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Fresh-cut & pre-prepared", "fresh-cut", 35 },
                    { new Guid("18658ed2-2781-1d0f-7b71-645486a75898"), new Guid("243bc2d1-b166-c1f3-a6e4-08971996f916"), null, true, "Storage & accessories", "home-accessories", 110 },
                    { new Guid("18fb36d2-b5a9-258a-ed9c-01cb0738e9ab"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen fruits", "frozen-fruit", 40 },
                    { new Guid("195badd2-1777-8b8b-076e-578874a8f7d4"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen desserts", "frozen-desserts", 43 },
                    { new Guid("19b8c5d2-14a6-7f91-f997-7fc8edd229cb"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Cream & Crème fraîche", "cream", 19 },
                    { new Guid("1b553bd2-c435-79ce-5d72-e4de2539c86c"), new Guid("f4568cd1-945e-1503-1684-a4d502c9b4f4"), null, true, "Sleep & comfort", "sleep-comfort", 145 },
                    { new Guid("1bbd4dd2-ab32-53a3-04d0-e004a74e16f8"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Goat", "goat", 3 },
                    { new Guid("1d30e6d2-c067-2177-403e-f82269279ff4"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Oats, Barley & Ancient grains", "oats-grains", 26 },
                    { new Guid("1d8b41d2-f41e-6176-dc88-350139fe2a74"), new Guid("b74b50d1-1a20-cf97-d715-83e37952b37f"), null, true, "Storage & hydration", "storage", 108 },
                    { new Guid("1e9766d2-8e14-bc32-0e66-d58542d25d58"), new Guid("da3754d1-9038-f673-3bc0-60e175e95158"), null, true, "Body care", "body-care", 74 },
                    { new Guid("248af1d2-cb0e-0f0a-8f82-84c11c6caecf"), new Guid("240017d1-d430-68f8-003b-267c83207c31"), null, true, "Ghee & rendered fats", "ghee", 61 },
                    { new Guid("269c54d2-b716-467c-5d4f-9b5afacd0ff2"), new Guid("da3754d1-9038-f673-3bc0-60e175e95158"), null, true, "Targeted treatments", "treatments", 75 },
                    { new Guid("271a15d2-2ac1-fafb-cb6d-068d7133640e"), new Guid("696da2d1-169d-50b5-bc90-059ce9d611ac"), null, true, "Maternity clothing", "maternity-clothing", 141 },
                    { new Guid("272b18d2-b8d2-297b-7e32-94c38f278611"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen ready meals", "frozen-meals", 41 },
                    { new Guid("2e6b16d2-a533-72a3-5ecb-d4258815635a"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen vegetables", "frozen-veg", 39 },
                    { new Guid("2ea98cd2-0b7e-5ed3-6c9d-22c5096ae03a"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Ice cream & Frozen desserts", "ice-cream", 20 },
                    { new Guid("2eecc8d2-1b9e-5b16-ad0f-75617186a9b0"), new Guid("be62fed1-3091-5fd1-dfa1-7813338e640a"), null, true, "Shirts & trousers", "shirts-trousers", 117 },
                    { new Guid("3120ffd2-9ab4-a2ce-5185-1bb4c691f055"), new Guid("f2fdb5d1-a2e2-7946-b0c1-375d21085ae3"), null, true, "Stock & bouillon", "stock", 59 },
                    { new Guid("328929d2-a919-8811-3c80-20c5fd6981ff"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Chips & Crisps", "chips", 48 },
                    { new Guid("341decd2-a9d9-e834-6813-186654d2a475"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Fresh herbs", "fresh-herbs", 32 },
                    { new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd"), new Guid("736014d1-c356-3bf6-7f54-42a42906bed9"), null, true, "Bath & skin", "bath-skin", 139 },
                    { new Guid("39b160d2-54bf-974b-9bda-82ac72849672"), new Guid("94cf87d1-7cef-b1c5-b234-63e8373f86bc"), null, true, "By audience", "by-audience", 126 },
                    { new Guid("3a5915d2-dcfd-9506-e745-953bab655b81"), new Guid("cebc7bd1-af4f-b20d-1cef-93581c822578"), null, true, "Eyes", "eyes", 79 },
                    { new Guid("3c06cfd2-ccd3-495d-819d-76553b497504"), new Guid("f2fdb5d1-a2e2-7946-b0c1-375d21085ae3"), null, true, "Spice mixes & single spices", "spices", 58 },
                    { new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4"), new Guid("d14dcad1-3c2e-f436-d0a7-378010d32869"), null, true, "Ready meals", "ready-meals", 44 },
                    { new Guid("3cad13d2-6a60-bd9c-c433-3342228d351f"), new Guid("b964a0d1-df83-1f56-a5ff-635a5838be16"), null, true, "Fragrance ingredients", "fragrance-ingredients", 155 },
                    { new Guid("400cf4d2-820e-70fa-0802-540d54203b6e"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Crab", "crab", 10 },
                    { new Guid("401953d2-1437-5628-d42c-e4c6f4a1a243"), new Guid("9f8913d1-a125-61d0-58d7-44f34abb7df2"), null, true, "Shampoo & Conditioner", "shampoo-conditioner", 76 },
                    { new Guid("4108d4d2-d111-f301-c2c6-231985a2b227"), new Guid("9f5922d1-d4b9-11e6-4be3-db69644ef876"), null, true, "Children's drinks", "children-drinks", 72 },
                    { new Guid("436179d2-bcdc-5c7d-1b3b-dcee8c42358b"), new Guid("9f1544d1-be54-86c4-06c9-bf6ee46ffa11"), null, true, "Boys", "boys", 120 },
                    { new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f"), new Guid("f4e11bd1-46a5-f278-af11-08e15df18d79"), null, true, "Herbal supplements", "herbal-supps", 91 },
                    { new Guid("4648f2d2-08d7-49f0-6a55-f68e9604a94b"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Candy, Gummies & Marshmallows", "candy", 51 },
                    { new Guid("46a81bd2-a65c-b762-cb53-60d7efb6a019"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Biscuits & Cookies", "biscuits", 49 },
                    { new Guid("46d91bd2-cec3-9851-1cd1-f89f54bb31d9"), new Guid("240017d1-d430-68f8-003b-267c83207c31"), null, true, "Shortening & specialty", "shortening", 62 },
                    { new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Fresh vegetables", "fresh-vegetables", 31 },
                    { new Guid("47d502d2-644a-bca0-fa90-41882f752db5"), new Guid("d8721ad1-c9c4-26d1-bca5-b4e506d9cf4a"), null, true, "Hotel supplies", "hotel", 158 },
                    { new Guid("49b9e3d2-e1f2-e025-e046-dec8531eb5b6"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Lamb & Mutton", "lamb-mutton", 2 },
                    { new Guid("49e85fd2-dc0c-2e5c-6284-5669988a659f"), new Guid("0a6b3fd1-b495-80f7-3678-540701d86355"), null, true, "First aid", "first-aid", 95 },
                    { new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8"), new Guid("da3754d1-9038-f673-3bc0-60e175e95158"), null, true, "Facial care", "facial", 73 },
                    { new Guid("50a758d2-e0b2-e472-305d-879c98566871"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Coffee", "coffee", 67 },
                    { new Guid("543c5cd2-7728-3b67-2497-8c8e79b1ff89"), new Guid("16161bd1-57cd-1e79-65ea-e744dc9aa190"), null, true, "By product", "by-product", 159 },
                    { new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb"), new Guid("ec26efd1-0078-4c83-503e-d247092983dd"), null, true, "Sports nutrition", "sports", 97 },
                    { new Guid("5592e0d2-546b-c966-3827-db3843f75cc0"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Juice & juice drinks", "juice", 64 },
                    { new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Dates & dried fruits", "dates-dried", 34 },
                    { new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6"), new Guid("af579cd1-320a-9581-39fa-cca82497ccfb"), null, true, "Oud, Attar & Bakhoor", "oud-attar", 83 },
                    { new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161"), new Guid("9f5922d1-d4b9-11e6-4be3-db69644ef876"), null, true, "Infant formula", "infant-formula", 71 },
                    { new Guid("5c9b1dd2-9b94-dcc8-73c1-42d054df30d5"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Snack & protein bars", "snack-bars", 53 },
                    { new Guid("5e4f2ad2-aad9-95d2-dcda-da060568463d"), new Guid("243bc2d1-b166-c1f3-a6e4-08971996f916"), null, true, "Textiles", "textiles", 109 },
                    { new Guid("5e90d9d2-661a-aeb2-6ba2-3904d1106e2f"), new Guid("cebc7bd1-af4f-b20d-1cef-93581c822578"), null, true, "Makeup tools & removers", "tools", 81 },
                    { new Guid("619bf8d2-13b6-8c7b-f4be-1292530d6762"), new Guid("ebaae6d1-ee50-4fb6-c894-9dd0c1d2a201"), null, true, "Traditional desserts", "traditional-desserts", 56 },
                    { new Guid("63295ad2-f567-52ad-739d-a4ba5fb7d082"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Lobster", "lobster", 11 },
                    { new Guid("644fd3d2-6041-7f69-0c15-17f0f0c38b2b"), new Guid("bf9802d1-f4c4-4bf1-a0a9-c9269e9b99e4"), null, true, "Baby drinks", "baby-drinks", 138 },
                    { new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3"), new Guid("ebaae6d1-ee50-4fb6-c894-9dd0c1d2a201"), null, true, "Bread", "bread", 54 },
                    { new Guid("669517d2-a3ae-d7e1-f93f-536676cb582e"), new Guid("be62fed1-3091-5fd1-dfa1-7813338e640a"), null, true, "Prayer wear", "prayer-wear-men", 118 },
                    { new Guid("66c580d2-4291-9f23-abdb-048c5fb2a2ed"), new Guid("f134e7d1-84d6-ee6e-dfb5-d866a541ae8f"), null, true, "Belts & small accessories", "belts", 124 },
                    { new Guid("68bad0d2-8a96-04b9-1552-1cd110ab4a02"), new Guid("f4568cd1-945e-1503-1684-a4d502c9b4f4"), null, true, "Accessories", "baby-accessories", 146 },
                    { new Guid("69bea1d2-68d0-38f0-464f-17bbc431cef0"), new Guid("696da2d1-169d-50b5-bc90-059ce9d611ac"), null, true, "Nursing & breastfeeding", "nursing", 142 },
                    { new Guid("6a3304d2-ffb2-a73a-84e7-299d7270a3a0"), new Guid("736014d1-c356-3bf6-7f54-42a42906bed9"), null, true, "Diapers & wipes", "diapers-wipes", 140 },
                    { new Guid("6b4e2fd2-fb7e-d6b7-99e7-1348fa428a43"), new Guid("6a76dbd1-da33-1b7b-f49d-3f48045d5458"), null, true, "Laundry", "laundry", 101 },
                    { new Guid("6d5f2dd2-bc68-2e75-a3fe-adac70a08b37"), new Guid("d8721ad1-c9c4-26d1-bca5-b4e506d9cf4a"), null, true, "Catering & packaging", "catering", 157 },
                    { new Guid("6dc32ad2-10b9-7c94-7de5-f45feb94e073"), new Guid("d14dcad1-3c2e-f436-d0a7-378010d32869"), null, true, "Sandwiches, salads & soups", "sandwiches-salads", 47 },
                    { new Guid("733bdad2-957e-8900-8965-86f883c49c56"), new Guid("82808bd1-9157-9527-a8f6-65aa24aebd86"), null, true, "Islamic history & literature", "history", 133 },
                    { new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652"), new Guid("b74b50d1-1a20-cf97-d715-83e37952b37f"), null, true, "Cookware", "cookware", 105 },
                    { new Guid("757aeed2-5da0-eb6a-e049-17f920363d1c"), new Guid("efc9f0d1-3f41-9b5f-1ee5-1df2ec7e840f"), null, true, "Cleansing", "cleansing", 84 },
                    { new Guid("771918d2-92fb-f616-b356-33dc5d6ad7b8"), new Guid("d14dcad1-3c2e-f436-d0a7-378010d32869"), null, true, "Instant meals & kits", "instant-meals", 46 },
                    { new Guid("779198d2-09c8-8c6b-ee20-dbc5f841c99c"), new Guid("82808bd1-9157-9527-a8f6-65aa24aebd86"), null, true, "Hadith & Fiqh", "hadith-fiqh", 131 },
                    { new Guid("7a4c51d2-3a04-a071-54c2-682511d05fb6"), new Guid("b51bafd1-502e-06bc-3bfc-0104c14fb0aa"), null, true, "Tools", "tools", 88 },
                    { new Guid("7b3aa8d2-4cdc-c22a-f308-77485f6ff511"), new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), null, true, "Flavour & aroma", "flavour", 149 },
                    { new Guid("7df5f9d2-7a91-dfde-cdbd-608238ba566c"), new Guid("bf9802d1-f4c4-4bf1-a0a9-c9269e9b99e4"), null, true, "Cereals & meals", "cereals-meals", 137 },
                    { new Guid("801120d2-55d3-1b47-cc42-72a206df3883"), new Guid("8003c1d1-0e9e-8de5-6e25-3faef604486e"), null, true, "Animal feed (wholesale)", "animal-feed", 164 },
                    { new Guid("83e7a0d2-f880-4239-6489-b22aff95394c"), new Guid("be62fed1-3091-5fd1-dfa1-7813338e640a"), null, true, "Kurta & Baju", "kurta-baju", 116 },
                    { new Guid("85aa6cd2-5fb1-222a-106d-e8174cd191fb"), new Guid("9f1544d1-be54-86c4-06c9-bf6ee46ffa11"), null, true, "Girls", "girls", 119 },
                    { new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Rice", "rice", 24 },
                    { new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5"), new Guid("4d20bfd1-fcf3-d988-9b34-c8190b544ec1"), null, true, "Ornaments & gifts", "ornaments", 135 },
                    { new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429"), new Guid("f4e11bd1-46a5-f278-af11-08e15df18d79"), null, true, "Traditional remedies", "traditional", 92 },
                    { new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15"), new Guid("ebaae6d1-ee50-4fb6-c894-9dd0c1d2a201"), null, true, "Cakes & Pastries", "cakes", 55 },
                    { new Guid("915801d2-844d-ec5d-fcc2-656ec7e8910e"), new Guid("805073d1-be3e-f1f1-38b9-1b0b167dbb2b"), null, true, "Quran accessories", "quran-accessories", 128 },
                    { new Guid("925043d2-44cd-d364-0cb2-08cd7d1e7d4b"), new Guid("60ad95d1-8b0b-4387-83f1-d9143288a8c7"), null, true, "Prayer wear", "prayer-wear-women", 113 },
                    { new Guid("927f1bd2-f1c8-b1ea-002a-d7eb533544f8"), new Guid("aa7117d1-c237-0781-c035-70e0f0bfcfe5"), null, true, "Traditional fragrance", "traditional-fragrance", 104 },
                    { new Guid("931af1d2-1327-3650-c921-7013f1061837"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen snacks", "frozen-snacks", 42 },
                    { new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1"), new Guid("ee0d88d1-6d91-3eee-d1cc-f53b47b2a5b1"), null, true, "By type", "by-type", 89 },
                    { new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Beef", "beef", 1 },
                    { new Guid("97a1a7d2-a92e-9df3-383d-53872a3dd6a6"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Shellfish", "shellfish", 13 },
                    { new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2"), new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), null, true, "Functional ingredients", "functional-ingredients", 148 },
                    { new Guid("980928d2-75e3-08f0-1ccd-a25d3962cc24"), new Guid("16161bd1-57cd-1e79-65ea-e744dc9aa190"), null, true, "By service", "by-service", 160 },
                    { new Guid("983f12d2-694d-8266-346d-a947ef94cbeb"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen poultry", "frozen-poultry", 37 },
                    { new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817"), new Guid("f2fdb5d1-a2e2-7946-b0c1-375d21085ae3"), null, true, "Sauces & Pastes", "sauces", 57 },
                    { new Guid("99bab0d2-8e11-eafa-8f08-3c719acdca01"), new Guid("82808bd1-9157-9527-a8f6-65aa24aebd86"), null, true, "Children's Islamic", "children-islamic", 132 },
                    { new Guid("99dddfd2-9738-7b86-9f78-b555b77d130c"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Bread, wraps & tortillas", "bread-wraps", 29 },
                    { new Guid("9b492ad2-d17e-a059-c279-80c4654425d6"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Butter & Ghee", "butter-ghee", 18 },
                    { new Guid("9b8c11d2-56e7-f6bc-b367-2562cafeb595"), new Guid("002e05d1-7638-458d-e254-8ff7f0ccedff"), null, true, "By category", "by-category", 161 },
                    { new Guid("9be6d2d2-078d-fa70-816b-665c204e5482"), new Guid("f4e11bd1-46a5-f278-af11-08e15df18d79"), null, true, "Herbal teas", "herbal-teas", 94 },
                    { new Guid("9dd85cd2-edb8-7602-3bb7-46deb09f5ed9"), new Guid("ec26efd1-0078-4c83-503e-d247092983dd"), null, true, "Meal replacement", "meal-replacement", 98 },
                    { new Guid("a16371d2-6324-444c-cebf-da38e1307dbb"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Prawns & Shrimp", "prawns-shrimp", 9 },
                    { new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380"), new Guid("b74b50d1-1a20-cf97-d715-83e37952b37f"), null, true, "Utensils & tools", "utensils", 107 },
                    { new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f"), new Guid("be62fed1-3091-5fd1-dfa1-7813338e640a"), null, true, "Thobes & jubbas", "thobes", 115 },
                    { new Guid("a63cddd2-fee7-1d9f-eacc-a5d6a951ca38"), new Guid("0a6b3fd1-b495-80f7-3678-540701d86355"), null, true, "Medical devices", "medical-devices", 96 },
                    { new Guid("a90d41d2-3ae5-4939-f0d5-4cff704ba5ce"), new Guid("4d20bfd1-fcf3-d988-9b34-c8190b544ec1"), null, true, "Wall art & calligraphy", "wall-art", 134 },
                    { new Guid("aa7cacd2-f2a0-11fe-8505-69723bb65d6a"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Plant-based drinks", "plant-drinks", 69 },
                    { new Guid("aab407d2-b3b4-55dc-b2e4-86cd8f991836"), new Guid("b964a0d1-df83-1f56-a5ff-635a5838be16"), null, true, "Waxes", "waxes", 153 },
                    { new Guid("aab9bfd2-a883-6348-6215-92e1aaca2a0c"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Dairy desserts & puddings", "dairy-desserts", 23 },
                    { new Guid("ad02f5d2-9f01-75f9-35b3-1812a5c4f337"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Soft drinks & mixers", "soft-drinks", 65 },
                    { new Guid("ad53b4d2-044d-b4ad-4755-0ed5f1105690"), new Guid("f4568cd1-945e-1503-1684-a4d502c9b4f4"), null, true, "Feeding", "feeding", 144 },
                    { new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e"), new Guid("94cf87d1-7cef-b1c5-b234-63e8373f86bc"), null, true, "By type", "by-type", 125 },
                    { new Guid("b2840bd2-908f-8c33-828d-79bad56d006c"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Mushrooms", "mushrooms", 33 },
                    { new Guid("b2ea6bd2-3ee3-0222-1343-8abc7030d643"), new Guid("d14dcad1-3c2e-f436-d0a7-378010d32869"), null, true, "Canned meals", "canned-meals", 45 },
                    { new Guid("b3ecf0d2-691f-ff7d-507a-e934cf8e93dc"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Canned & preserved seafood", "canned-preserved", 14 },
                    { new Guid("b455e9d2-1637-96bc-1d91-17eccff9ab22"), new Guid("aa7117d1-c237-0781-c035-70e0f0bfcfe5"), null, true, "Active fragrance", "active", 103 },
                    { new Guid("b779ecd2-8416-1ca5-589c-54e76e66565a"), new Guid("af579cd1-320a-9581-39fa-cca82497ccfb"), null, true, "Perfume", "perfume", 82 },
                    { new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4"), new Guid("398c04d1-0daf-6abb-1070-c79522f720c5"), null, true, "Nuts, seeds & popcorn", "nuts-seeds", 52 },
                    { new Guid("ba652ad2-de24-d0b3-e225-cbdeb99d1e5f"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Turkey", "turkey", 5 },
                    { new Guid("bbb126d2-7508-6bb6-9881-5bc2d6be57d6"), new Guid("157bb9d1-f805-55fa-247c-1e2e4b77c2f3"), null, true, "Prayer mats", "prayer-mats", 129 },
                    { new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6"), new Guid("60ad95d1-8b0b-4387-83f1-d9143288a8c7"), null, true, "Modest everyday", "modest-everyday", 114 },
                    { new Guid("be412ad2-16ea-0a0b-80cc-e870767baf55"), new Guid("002e05d1-7638-458d-e254-8ff7f0ccedff"), null, true, "By logistics", "by-logistics", 162 },
                    { new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c"), new Guid("ee0d88d1-6d91-3eee-d1cc-f53b47b2a5b1"), null, true, "Specialty", "specialty", 90 },
                    { new Guid("c278d2d2-6301-8192-242e-145233b94bfa"), new Guid("b964a0d1-df83-1f56-a5ff-635a5838be16"), null, true, "Extracts & actives", "extracts", 154 },
                    { new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99"), new Guid("f20168d1-6a7c-e430-db51-0bbeaf75ad79"), null, true, "Fresh fruits", "fresh-fruits", 30 },
                    { new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a"), new Guid("9f8913d1-a125-61d0-58d7-44f34abb7df2"), null, true, "Treatments & styling", "treatments-styling", 77 },
                    { new Guid("d05671d2-1cb3-0bdc-1e49-e5e189e4dc51"), new Guid("b51bafd1-502e-06bc-3bfc-0104c14fb0aa"), null, true, "Toothpaste & mouthwash", "toothpaste", 87 },
                    { new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Milk", "milk", 15 },
                    { new Guid("d213fad2-d5e5-a59a-a642-5c63c488948d"), new Guid("6a76dbd1-da33-1b7b-f49d-3f48045d5458"), null, true, "Dishwashing", "dishwashing", 102 },
                    { new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf"), new Guid("805073d1-be3e-f1f1-38b9-1b0b167dbb2b"), null, true, "Qurans", "qurans", 127 },
                    { new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9"), new Guid("f134e7d1-84d6-ee6e-dfb5-d866a541ae8f"), null, true, "Bags & wallets", "bags-wallets", 123 },
                    { new Guid("d4d96bd2-6008-bd2c-4d7e-2066a922817b"), new Guid("9f5922d1-d4b9-11e6-4be3-db69644ef876"), null, true, "Baby food", "baby-food", 70 },
                    { new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Processed meat", "processed-meat", 7 },
                    { new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa"), new Guid("6a76dbd1-da33-1b7b-f49d-3f48045d5458"), null, true, "Surface cleaners", "surface", 100 },
                    { new Guid("d81fdbd2-0284-97c3-41ca-677a4e85a95d"), new Guid("efc9f0d1-3f41-9b5f-1ee5-1df2ec7e840f"), null, true, "Feminine hygiene", "feminine", 86 },
                    { new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be"), new Guid("da6facd1-b6de-05ff-d151-6f4d5820e408"), null, true, "Wheat & Flour", "wheat-flour", 25 },
                    { new Guid("e3d391d2-ab2e-d995-1ae6-1b4059e0c915"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Functional, sports & energy", "functional", 68 },
                    { new Guid("e43f39d2-399a-bbbd-dddd-8c751a24b299"), new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), null, true, "Processing ingredients", "processing", 151 },
                    { new Guid("e47115d2-f163-dc61-232d-96825d1f1f7f"), new Guid("cebc7bd1-af4f-b20d-1cef-93581c822578"), null, true, "Lips", "lips", 80 },
                    { new Guid("e67ff7d2-9d3d-e8b9-e8b9-388ad6a76031"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Milk alternatives", "milk-alt", 21 },
                    { new Guid("e77cafd2-ec5f-39af-b161-5c8527add63f"), new Guid("45b7a2d1-f88b-29b6-3bc4-cbde7854b0c0"), null, true, "Frozen seafood", "frozen-seafood", 38 },
                    { new Guid("eb2125d2-5dd5-092e-83b8-5db65c8ca7ed"), new Guid("ec26efd1-0078-4c83-503e-d247092983dd"), null, true, "Special dietary foods", "special-dietary", 99 },
                    { new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941"), new Guid("60ad95d1-8b0b-4387-83f1-d9143288a8c7"), null, true, "Hijabs & head coverings", "hijabs", 112 },
                    { new Guid("f12708d2-d7dd-f11c-ab9d-1fb251073f15"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Squid & Octopus", "squid-octopus", 12 },
                    { new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3"), new Guid("240017d1-d430-68f8-003b-267c83207c31"), null, true, "Cooking oils", "cooking-oils", 60 },
                    { new Guid("f1ea79d2-5a1b-9032-e4da-39a839c285fa"), new Guid("efc9f0d1-3f41-9b5f-1ee5-1df2ec7e840f"), null, true, "Spa & treatments", "spa", 85 },
                    { new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8"), new Guid("60ad95d1-8b0b-4387-83f1-d9143288a8c7"), null, true, "Abayas", "abayas", 111 },
                    { new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5"), new Guid("157bb9d1-f805-55fa-247c-1e2e4b77c2f3"), null, true, "Prayer accessories", "prayer-accessories", 130 },
                    { new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe"), new Guid("9dab71d1-2307-dd5b-84ef-75deb4a3e6ea"), null, true, "Tea", "tea", 66 },
                    { new Guid("f3c106d2-bdc8-4e80-9e7a-1baf621d9d0e"), new Guid("9f1544d1-be54-86c4-06c9-bf6ee46ffa11"), null, true, "Prayer wear", "kids-prayer", 121 },
                    { new Guid("f40d8ad2-aed2-55b6-889e-fdce8e20e05b"), new Guid("8003c1d1-0e9e-8de5-6e25-3faef604486e"), null, true, "Pet food (retail packs under dept 1)", "pet-food", 163 },
                    { new Guid("f62090d2-8c0b-d9c7-2cdc-a63d3ec59e8c"), new Guid("6d8574d1-12f3-b765-b36b-5c7bec6e296b"), null, true, "Food additives", "additives", 150 },
                    { new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd"), new Guid("bf9802d1-f4c4-4bf1-a0a9-c9269e9b99e4"), null, true, "Formula & milk", "formula-milk", 136 },
                    { new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f"), new Guid("d8721ad1-c9c4-26d1-bca5-b4e506d9cf4a"), null, true, "Restaurant supplies", "restaurant", 156 },
                    { new Guid("fce9d2d2-92d3-9775-401f-a76309fae629"), new Guid("9e6783d1-8758-f6ff-75a6-49cc91a2e784"), null, true, "Fish", "fish", 8 },
                    { new Guid("fe0861d2-40c3-d2b1-0b67-1d20ee82a28f"), new Guid("997017d1-3429-3586-1b60-7b568fc452bb"), null, true, "Yogurt & Yogurt drinks", "yogurt", 17 },
                    { new Guid("fe9fc9d2-3128-c77c-2a28-e6e17a0585b8"), new Guid("5d68efd1-05a4-2686-bfb5-55d4c8465637"), null, true, "Duck", "duck", 6 },
                    { new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0"), new Guid("f134e7d1-84d6-ee6e-dfb5-d866a541ae8f"), null, true, "Hijab accessories", "hijab-accessories", 122 }
                });

            migrationBuilder.InsertData(
                table: "Certificates",
                columns: new[] { "Id", "CertificateNumber", "CertificationBody", "CreatedAt", "ExpiryDate", "IssueDate", "Jurisdiction", "ProductId", "Scope", "Status" },
                values: new object[,]
                {
                    { new Guid("c0010000-0000-0000-0000-000000000000"), "JAKIM-2025-0001", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0010000-0000-0000-0000-000000000000"), "Certification for Keropok Lekor Original", 3 },
                    { new Guid("c0020000-0000-0000-0000-000000000000"), "JAKIM-2026-0002", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0030000-0000-0000-0000-000000000000"), "Certification for Murukku Masala", 3 },
                    { new Guid("c0030000-0000-0000-0000-000000000000"), "JAKIM-2024-0003", "MUI", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ID", new Guid("b0050000-0000-0000-0000-000000000000"), "Certification for Tempeh Chips", 3 },
                    { new Guid("c0040000-0000-0000-0000-000000000000"), "JAKIM-2025-0004", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0070000-0000-0000-0000-000000000000"), "Certification for Tau Huay Crisps", 3 },
                    { new Guid("c0050000-0000-0000-0000-000000000000"), "JAKIM-2026-0005", "ESMA", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AE", new Guid("b0090000-0000-0000-0000-000000000000"), "Certification for Namkeen Mix", 3 },
                    { new Guid("c0060000-0000-0000-0000-000000000000"), "JAKIM-2024-0006", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0110000-0000-0000-0000-000000000000"), "Certification for Teh Tarik Concentrate", 3 },
                    { new Guid("c0070000-0000-0000-0000-000000000000"), "JAKIM-2025-0007", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0130000-0000-0000-0000-000000000000"), "Certification for Sirap Bandung", 3 },
                    { new Guid("c0080000-0000-0000-0000-000000000000"), "JAKIM-2026-0008", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0150000-0000-0000-0000-000000000000"), "Certification for Coconut Water", 3 },
                    { new Guid("c0090000-0000-0000-0000-000000000000"), "JAKIM-2024-0009", "ESMA", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AE", new Guid("b0170000-0000-0000-0000-000000000000"), "Certification for Rose Water Drink", 3 },
                    { new Guid("c0100000-0000-0000-0000-000000000000"), "JAKIM-2025-0010", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0190000-0000-0000-0000-000000000000"), "Certification for Ayam Kampung", 3 },
                    { new Guid("c0110000-0000-0000-0000-000000000000"), "JAKIM-2026-0011", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0210000-0000-0000-0000-000000000000"), "Certification for Sate Ayam", 3 },
                    { new Guid("c0120000-0000-0000-0000-000000000000"), "JAKIM-2024-0012", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0230000-0000-0000-0000-000000000000"), "Certification for Chicken Nugget", 3 },
                    { new Guid("c0130000-0000-0000-0000-000000000000"), "JAKIM-2025-0013", "MUI", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ID", new Guid("b0250000-0000-0000-0000-000000000000"), "Certification for Sosis Sapi", 3 },
                    { new Guid("c0140000-0000-0000-0000-000000000000"), "JAKIM-2026-0014", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0270000-0000-0000-0000-000000000000"), "Certification for Rendang Paste", 3 },
                    { new Guid("c0150000-0000-0000-0000-000000000000"), "JAKIM-2024-0015", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0290000-0000-0000-0000-000000000000"), "Certification for Kari Ayam Powder", 3 },
                    { new Guid("c0160000-0000-0000-0000-000000000000"), "JAKIM-2025-0016", "ESMA", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AE", new Guid("b0310000-0000-0000-0000-000000000000"), "Certification for Zaatar Blend", 3 },
                    { new Guid("c0170000-0000-0000-0000-000000000000"), "JAKIM-2026-0017", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0330000-0000-0000-0000-000000000000"), "Certification for Curry Leaf", 3 },
                    { new Guid("c0180000-0000-0000-0000-000000000000"), "JAKIM-2024-0018", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0350000-0000-0000-0000-000000000000"), "Certification for Roti Canai", 3 },
                    { new Guid("c0190000-0000-0000-0000-000000000000"), "JAKIM-2025-0019", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0370000-0000-0000-0000-000000000000"), "Certification for Wholemeal Bread", 1 },
                    { new Guid("c0200000-0000-0000-0000-000000000000"), "JAKIM-2026-0020", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2027, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0390000-0000-0000-0000-000000000000"), "Certification for Blueberry Muffin", 1 },
                    { new Guid("c0210000-0000-0000-0000-000000000000"), "JAKIM-2024-0021", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0410000-0000-0000-0000-000000000000"), "Certification for Pia Durian", 1 },
                    { new Guid("c0220000-0000-0000-0000-000000000000"), "JAKIM-2025-0022", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0430000-0000-0000-0000-000000000000"), "Certification for Vitamin C Serum", 1 },
                    { new Guid("c0230000-0000-0000-0000-000000000000"), "JAKIM-2026-0023", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0450000-0000-0000-0000-000000000000"), "Certification for Aloe Moisturizer", 1 },
                    { new Guid("c0240000-0000-0000-0000-000000000000"), "JAKIM-2024-0024", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0470000-0000-0000-0000-000000000000"), "Certification for Palm Shampoo", 1 },
                    { new Guid("c0250000-0000-0000-0000-000000000000"), "JAKIM-2025-0025", "JAKIM", new DateTimeOffset(new DateTime(2025, 10, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", new Guid("b0490000-0000-0000-0000-000000000000"), "Certification for Hijab Conditioner", 1 }
                });

            migrationBuilder.InsertData(
                table: "HalalVerifications",
                columns: new[] { "Id", "CertificateId", "ComplianceStatus", "Jurisdiction", "MissingEvidence", "PolicyVersion", "ProductId", "ReasonCodes", "RequiresHumanReview", "VerifiedAt" },
                values: new object[,]
                {
                    { new Guid("d0010000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0010000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0020000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0030000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0030000-0000-0000-0000-000000000000"), null, 0, "ID", "[]", "MY-v3", new Guid("b0050000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0040000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0070000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0050000-0000-0000-0000-000000000000"), null, 0, "AE", "[]", "MY-v3", new Guid("b0090000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0060000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0110000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0070000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0130000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0080000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0150000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0090000-0000-0000-0000-000000000000"), null, 0, "AE", "[]", "MY-v3", new Guid("b0170000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0100000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0190000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0110000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0210000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0120000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0230000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0130000-0000-0000-0000-000000000000"), null, 0, "ID", "[]", "MY-v3", new Guid("b0250000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0140000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0270000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0150000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0290000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0160000-0000-0000-0000-000000000000"), null, 0, "AE", "[]", "MY-v3", new Guid("b0310000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0170000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0330000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0180000-0000-0000-0000-000000000000"), null, 0, "MY", "[]", "MY-v3", new Guid("b0350000-0000-0000-0000-000000000000"), "[]", false, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0190000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0370000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0200000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0390000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0210000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0410000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0220000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0430000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0230000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0450000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0240000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0470000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d0250000-0000-0000-0000-000000000000"), null, 1, "MY", "[\"supplier_declaration\"]", "MY-v3", new Guid("b0490000-0000-0000-0000-000000000000"), "[\"CERT_UNDER_REVIEW\"]", true, new DateTimeOffset(new DateTime(2025, 12, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "TaxonomyProductTypes",
                columns: new[] { "Id", "Description", "IsActive", "Name", "PathSlug", "Slug", "SortOrder", "SubcategoryId" },
                values: new object[,]
                {
                    { new Guid("009bbdd3-e3dc-a57e-a376-862fa6c4e353"), null, true, "Muesli", "food-beverage/rice-grains-staples/cereals/muesli", "muesli", 114, new Guid("036c3ad2-b97c-4bcb-0ece-e1be8d0bb0df") },
                    { new Guid("00cf73d3-9434-0379-6662-6d6ee6ca55cb"), null, true, "Lip gloss", "personal-care/cosmetics-makeup/lips/lip-gloss", "lip-gloss", 339, new Guid("e47115d2-f163-dc61-232d-96825d1f1f7f") },
                    { new Guid("016632d3-4178-23c7-bf80-daf2a40fed70"), null, true, "Tortilla", "food-beverage/rice-grains-staples/bread-wraps/tortilla", "tortilla", 117, new Guid("99dddfd2-9738-7b86-9f78-b555b77d130c") },
                    { new Guid("019f43d3-6027-aa03-eb99-2cace390b334"), null, true, "Pads", "personal-care/bath-body/feminine/pads", "pads", 363, new Guid("d81fdbd2-0284-97c3-41ca-677a4e85a95d") },
                    { new Guid("01cb72d3-b92a-0c29-604c-65f1ac9f3eda"), null, true, "Cuts", "food-beverage/meat-poultry/duck/cuts", "cuts", 22, new Guid("fe9fc9d2-3128-c77c-2a28-e6e17a0585b8") },
                    { new Guid("01fcb1d3-c1f1-a5bb-af3a-49ea96df7083"), null, true, "Smoked", "food-beverage/seafood/fish/smoked", "smoked", 31, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("022be0d3-bdf2-e0d0-f991-cbe39f200deb"), null, true, "Dresses", "modest-fashion/womens-clothing/modest-everyday/dresses", "dresses", 484, new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6") },
                    { new Guid("0309c9d3-bf82-9137-4fef-8e9a341b110e"), null, true, "Black", "food-beverage/beverages/tea/black", "black", 275, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("036187d3-3b6a-c0c5-cae4-391d44786879"), null, true, "Large print", "islamic-religious/quran/qurans/large-print", "large-print", 535, new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf") },
                    { new Guid("03813ed3-d0b1-eb68-0f7a-8b1e12972a99"), null, true, "Toothpaste", "personal-care/oral-care/toothpaste/toothpaste", "toothpaste", 364, new Guid("d05671d2-1cb3-0bdc-1e49-e5e189e4dc51") },
                    { new Guid("03f343d3-93c2-b45c-563d-0d7f0574aa5b"), null, true, "Milk", "food-beverage/baby-food/children-drinks/milk", "milk", 304, new Guid("4108d4d2-d111-f301-c2c6-231985a2b227") },
                    { new Guid("040229d3-dc60-e01f-9fa5-8ba86073a6fd"), null, true, "Medjool", "food-beverage/fresh-produce/dates-dried/medjool", "medjool", 142, new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142") },
                    { new Guid("04f370d3-4d25-eeab-564d-8890dea8559b"), null, true, "Flavoured", "food-beverage/dairy-eggs/milk/flavoured", "flavoured", 56, new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff") },
                    { new Guid("04f664d3-4f84-cd3c-4d3f-d670ce3adf23"), null, true, "Tualang honey", "health-wellness/herbal-traditional/traditional/tualang-honey", "tualang-honey", 391, new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429") },
                    { new Guid("04f723d3-be2b-0592-f052-8e65057cb303"), null, true, "Tomato sauce", "food-beverage/sauces-spices/sauces/tomato-sauce", "tomato-sauce", 235, new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817") },
                    { new Guid("06838cd3-c564-f767-2f0e-0c68762f5894"), null, true, "Honey", "health-wellness/herbal-traditional/traditional/honey", "honey", 390, new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429") },
                    { new Guid("07c189d3-3d15-bce0-5472-3e17af943342"), null, true, "Pieces", "food-beverage/seafood/crab/pieces", "pieces", 39, new Guid("400cf4d2-820e-70fa-0802-540d54203b6e") },
                    { new Guid("07dbc5d3-4877-3e0b-d68c-ed4977090806"), null, true, "Import/export", "b2b/wholesale/by-logistics/import-export", "import-export", 665, new Guid("be412ad2-16ea-0a0b-80cc-e870767baf55") },
                    { new Guid("080a76d3-d91b-a754-a435-0052e4f8f52e"), null, true, "Fruit extracts", "b2b/cosmetic-ingredients/extracts/fruit", "fruit", 636, new Guid("c278d2d2-6301-8192-242e-145233b94bfa") },
                    { new Guid("0874b0d3-2bf0-4fed-2342-04c152d83bf1"), null, true, "Hijabs", "baby-mother/maternity/maternity-clothing/hijabs", "hijabs", 588, new Guid("271a15d2-2ac1-fafb-cb6d-068d7133640e") },
                    { new Guid("088dc6d3-e5d9-a323-1932-747a0267cef5"), null, true, "Mozzarella", "food-beverage/dairy-eggs/cheese/mozzarella", "mozzarella", 61, new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5") },
                    { new Guid("089277d3-acce-93aa-9434-1dd08f55d0c5"), null, true, "Lemongrass", "food-beverage/fresh-produce/fresh-herbs/lemongrass", "lemongrass", 137, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("08ac57d3-dd43-03ae-d353-e5170f0ad81d"), null, true, "Bakery ingredients", "b2b/horeca/restaurant/bakery-ingredients", "bakery-ingredients", 646, new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f") },
                    { new Guid("08c419d3-ddea-4b51-fdf2-66e0728a164a"), null, true, "Meal kits", "food-beverage/ready-to-eat/instant-meals/meal-kits", "meal-kits", 186, new Guid("771918d2-92fb-f616-b356-33dc5d6ad7b8") },
                    { new Guid("092268d3-e4c5-3904-4272-d661c2319aa1"), null, true, "Mineral", "food-beverage/beverages/water/mineral", "mineral", 263, new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c") },
                    { new Guid("092f51d3-0443-549d-9ce6-bd8b3d9a8781"), null, true, "Moulds", "household/kitchen/bakeware/moulds", "moulds", 448, new Guid("148f29d2-a1da-5ee6-c4cc-e7af1ea771a9") },
                    { new Guid("096b7ad3-0ce2-fa8c-cdd6-6aca2798f851"), null, true, "Aquaculture feed", "b2b/pet-animal-b2b/animal-feed/aquaculture-feed", "aquaculture-feed", 668, new Guid("801120d2-55d3-1b47-cc42-72a206df3883") },
                    { new Guid("098328d3-ab33-2e0d-d97c-6499d48b6e62"), null, true, "Powder", "baby-mother/baby-care/bath-skin/powder", "powder", 580, new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd") },
                    { new Guid("098e7cd3-a063-8b3a-5fcc-647f99a592d1"), null, true, "Lunch boxes", "household/kitchen/storage/lunch-boxes", "lunch-boxes", 456, new Guid("1d8b41d2-f41e-6176-dc88-350139fe2a74") },
                    { new Guid("09d071d3-fff1-0173-dad3-5a0d38d17605"), null, true, "Steaks", "food-beverage/seafood/fish/steaks", "steaks", 30, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("0a71d2d3-2c39-df09-c5cc-206ef1827ee6"), null, true, "Mouthwash", "personal-care/oral-care/toothpaste/mouthwash", "mouthwash", 365, new Guid("d05671d2-1cb3-0bdc-1e49-e5e189e4dc51") },
                    { new Guid("0a874dd3-0c30-7bf1-e9fe-46bedde78b78"), null, true, "Bars", "food-beverage/snacks-confectionery/chocolate/bars", "bars", 199, new Guid("058e65d2-fcd1-59a1-12be-14e797000487") },
                    { new Guid("0b1fb0d3-503a-ac0a-1d8e-940c834da402"), null, true, "Popcorn", "food-beverage/snacks-confectionery/nuts-seeds/popcorn", "popcorn", 212, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("0c945cd3-01f9-c2ae-ad03-f9139e49471f"), null, true, "Pistachios", "food-beverage/snacks-confectionery/nuts-seeds/pistachios", "pistachios", 209, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("0d1145d3-73d0-5c6d-aa43-9113c288fc9c"), null, true, "Soy milk", "food-beverage/beverages/plant-drinks/soy-milk", "soy-milk", 294, new Guid("aa7cacd2-f2a0-11fe-8505-69723bb65d6a") },
                    { new Guid("0d6dfed3-fbcf-0b76-23d7-5ff779407acc"), null, true, "Cuts", "food-beverage/meat-poultry/goat/cuts", "cuts", 11, new Guid("1bbd4dd2-ab32-53a3-04d0-e004a74e16f8") },
                    { new Guid("0f6338d3-3c8e-50e7-c8c1-55071a1f1828"), null, true, "Attar", "personal-care/fragrance/oud-attar/attar", "attar", 349, new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6") },
                    { new Guid("0f74e2d3-dc88-3be4-c447-890416b4e5ad"), null, true, "Essential oils", "b2b/cosmetic-ingredients/fragrance-ingredients/essential-oils", "essential-oils", 639, new Guid("3cad13d2-6a60-bd9c-c433-3342228d351f") },
                    { new Guid("0ff14dd3-f04b-9c2a-ff04-5781f48f4dbe"), null, true, "Peas", "food-beverage/frozen-food/frozen-veg/peas", "peas", 160, new Guid("2e6b16d2-a533-72a3-5ecb-d4258815635a") },
                    { new Guid("10622dd3-e566-0556-7784-5fb58e063189"), null, true, "Loaf", "food-beverage/bakery/bread/loaf", "loaf", 217, new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3") },
                    { new Guid("10c71ad3-68b8-b096-090b-37a1e81c7c2b"), null, true, "Tongs", "household/kitchen/utensils/tongs", "tongs", 450, new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380") },
                    { new Guid("10da9ed3-a28a-7492-91e7-780dc5104a0c"), null, true, "Granola", "food-beverage/rice-grains-staples/cereals/granola", "granola", 113, new Guid("036c3ad2-b97c-4bcb-0ece-e1be8d0bb0df") },
                    { new Guid("11044fd3-0f4c-6285-f636-d9a80ce2c40b"), null, true, "Baking trays", "household/kitchen/bakeware/trays", "trays", 446, new Guid("148f29d2-a1da-5ee6-c4cc-e7af1ea771a9") },
                    { new Guid("110ef1d3-bc84-7ee4-3d0c-398df5180b80"), null, true, "Artificial flavours", "b2b/halal-ingredients/flavour/artificial-flavours", "artificial-flavours", 617, new Guid("7b3aa8d2-4cdc-c22a-f308-77485f6ff511") },
                    { new Guid("11567dd3-3293-570d-f32f-4e710350ab10"), null, true, "Bulk halal meat", "b2b/horeca/restaurant/bulk-meat", "bulk-meat", 642, new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f") },
                    { new Guid("119461d3-0bbe-1f4f-2b8f-63a29342e2ce"), null, true, "Toners", "personal-care/skincare/facial/toners", "toners", 307, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("11fecfd3-59f3-0464-be3e-01106589aa4b"), null, true, "Jerky", "food-beverage/meat-poultry/processed-meat/jerky", "jerky", 27, new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6") },
                    { new Guid("121ad5d3-4829-d536-485f-64b29a7d4ed1"), null, true, "Asian", "food-beverage/frozen-food/frozen-meals/asian", "asian", 168, new Guid("272b18d2-b8d2-297b-7e32-94c38f278611") },
                    { new Guid("128eabd3-1c7b-a250-6ea5-ef859ddfb150"), null, true, "Incense", "household/home-fragrance/traditional-fragrance/incense", "incense", 439, new Guid("927f1bd2-f1c8-b1ea-002a-d7eb533544f8") },
                    { new Guid("129af8d3-03ae-0353-0c6c-69a2b121488d"), null, true, "Omega oils", "health-wellness/vitamins/specialty/omega-oils", "omega-oils", 381, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("12aa16d3-9fa3-be30-d8b3-a05c633362e5"), null, true, "Pasta", "food-beverage/frozen-food/frozen-meals/pasta", "pasta", 166, new Guid("272b18d2-b8d2-297b-7e32-94c38f278611") },
                    { new Guid("12ac24d3-2a0a-e2a5-119c-007cd443f11d"), null, true, "Shoes", "modest-fashion/footwear/by-type/shoes", "shoes", 524, new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e") },
                    { new Guid("135742d3-78ff-6ab1-eda6-33e7f978c631"), null, true, "Quran stories", "islamic-religious/islamic-books/children-islamic/quran-stories", "quran-stories", 553, new Guid("99bab0d2-8e11-eafa-8f08-3c719acdca01") },
                    { new Guid("136646d3-5fcf-6503-e9e8-c922db2ae77d"), null, true, "Hair colour", "personal-care/haircare/treatments-styling/hair-colour", "hair-colour", 327, new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a") },
                    { new Guid("13b831d3-33b7-88b5-ee41-77b068fdc019"), null, true, "Eye care", "personal-care/skincare/facial/eye-care", "eye-care", 312, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("13f035d3-665b-944c-fafb-585be935a073"), null, true, "Plant-based", "baby-mother/baby-food-d7/formula-milk/plant-based", "plant-based", 570, new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd") },
                    { new Guid("1461a1d3-39d5-5443-c3e4-28c80afd7535"), null, true, "Tea tree", "health-wellness/herbal-traditional/essential-oils/tea-tree", "tea-tree", 396, new Guid("08a7a9d2-8e75-3cda-513e-9cc91b3b6c3e") },
                    { new Guid("148a9cd3-936f-53e4-0368-dd7256d2f9e5"), null, true, "Turmeric", "health-wellness/herbal-traditional/herbal-supps/turmeric", "turmeric", 387, new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f") },
                    { new Guid("15f217d3-91d8-55f8-4a13-740c6fcf2d94"), null, true, "Miswak", "personal-care/oral-care/toothpaste/miswak", "miswak", 366, new Guid("d05671d2-1cb3-0bdc-1e49-e5e189e4dc51") },
                    { new Guid("162dfcd3-92a3-8618-74bb-8508f61b4682"), null, true, "Sesame oil", "food-beverage/oils-fats/cooking-oils/sesame-oil", "sesame-oil", 253, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("165533d3-cceb-80bb-7ad0-0c6479da81da"), null, true, "Eau de toilette", "personal-care/fragrance/perfume/edt", "edt", 345, new Guid("b779ecd2-8416-1ca5-589c-54e76e66565a") },
                    { new Guid("170291d3-42e0-008d-dc19-aa4f33ca1c02"), null, true, "Sliced", "food-beverage/bakery/bread/sliced", "sliced", 218, new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3") },
                    { new Guid("183b94d3-4560-69f3-b3de-6f60ffcf0563"), null, true, "Custard", "food-beverage/dairy-eggs/dairy-desserts/custard", "custard", 88, new Guid("aab9bfd2-a883-6348-6215-92e1aaca2a0c") },
                    { new Guid("18f81dd3-182d-68bb-7c59-d672423f3906"), null, true, "Dish soap", "household/cleaning/dishwashing/dish-soap", "dish-soap", 430, new Guid("d213fad2-d5e5-a59a-a642-5c63c488948d") },
                    { new Guid("19030fd3-e2d1-5f97-7b7a-e68e7e1bedfc"), null, true, "Mixed", "food-beverage/beverages/juice/mixed", "mixed", 269, new Guid("5592e0d2-546b-c966-3827-db3843f75cc0") },
                    { new Guid("1981fbd3-c05b-873c-56ca-a1ade99191d5"), null, true, "Trousers", "baby-mother/maternity/maternity-clothing/trousers", "trousers", 587, new Guid("271a15d2-2ac1-fafb-cb6d-068d7133640e") },
                    { new Guid("19925bd3-135d-8520-0bfe-db22df038913"), null, true, "Berries", "food-beverage/frozen-food/frozen-fruit/berries", "berries", 162, new Guid("18fb36d2-b5a9-258a-ed9c-01cb0738e9ab") },
                    { new Guid("1a0028d3-60dd-500d-4fc0-7ea3a4da25f5"), null, true, "Dresses", "baby-mother/maternity/maternity-clothing/dresses", "dresses", 585, new Guid("271a15d2-2ac1-fafb-cb6d-068d7133640e") },
                    { new Guid("1a80e0d3-f4e3-abb6-84a6-572cc6c0bfa4"), null, true, "Spaghetti", "food-beverage/rice-grains-staples/pasta-noodles/spaghetti", "spaghetti", 104, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("1ad651d3-7f4d-e56a-b9a7-466341357e91"), null, true, "Fillet", "food-beverage/seafood/fish/fillet", "fillet", 29, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("1aea53d3-4827-d188-27a3-c6d41e4b2b3b"), null, true, "Marinade", "food-beverage/sauces-spices/sauces/marinade", "marinade", 234, new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817") },
                    { new Guid("1b058ed3-78f7-1131-2174-ecef905cc98a"), null, true, "Ground", "food-beverage/beverages/coffee/ground", "ground", 283, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("1bd32ed3-8160-ba0b-943c-1f49fe614bd1"), null, true, "Greek", "food-beverage/dairy-eggs/yogurt/greek", "greek", 65, new Guid("fe0861d2-40c3-d2b1-0b67-1d20ee82a28f") },
                    { new Guid("1c01c1d3-fe96-20ea-ab4e-fb607e0bd159"), null, true, "Ginger tea", "health-wellness/herbal-traditional/herbal-teas/ginger-tea", "ginger-tea", 401, new Guid("9be6d2d2-078d-fa70-816b-665c204e5482") },
                    { new Guid("1c4bced3-649a-7fab-b7d7-d9953c2ab8f6"), null, true, "Cinnamon", "food-beverage/sauces-spices/spices/cinnamon", "cinnamon", 240, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("1c5a12d3-6f30-da45-4431-79368c727af4"), null, true, "Concealer", "personal-care/cosmetics-makeup/face/concealer", "concealer", 330, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("1c5fc2d3-d3a8-2e82-cbc7-9aad277c66e3"), null, true, "Modest shirts", "modest-fashion/mens-clothing/shirts-trousers/modest-shirts", "modest-shirts", 494, new Guid("2eecc8d2-1b9e-5b16-ad0f-75617186a9b0") },
                    { new Guid("1ddaf1d3-b120-ddf7-defd-52a397300e6d"), null, true, "Atta", "food-beverage/rice-grains-staples/wheat-flour/atta", "atta", 96, new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be") },
                    { new Guid("1ed979d3-ba6f-6782-8c1b-e0f2d870cafa"), null, true, "Lavender", "health-wellness/herbal-traditional/essential-oils/lavender", "lavender", 395, new Guid("08a7a9d2-8e75-3cda-513e-9cc91b3b6c3e") },
                    { new Guid("1f3063d3-0a59-a40c-2a9e-d3072a4e84df"), null, true, "Chocolate chip", "food-beverage/snacks-confectionery/biscuits/chocolate-chip", "chocolate-chip", 197, new Guid("46a81bd2-a65c-b762-cb53-60d7efb6a019") },
                    { new Guid("1f49abd3-e658-cc7d-1663-a0f9878dbd70"), null, true, "Lasagne", "food-beverage/rice-grains-staples/pasta-noodles/lasagne", "lasagne", 107, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("1fa34fd3-e515-3754-35a3-a35518f642b9"), null, true, "Butter", "food-beverage/dairy-eggs/butter-ghee/butter", "butter", 67, new Guid("9b492ad2-d17e-a059-c279-80c4654425d6") },
                    { new Guid("208a08d3-d56a-90d4-55a0-d32c2fafd510"), null, true, "Cardamom", "food-beverage/sauces-spices/spices/cardamom", "cardamom", 242, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("20f47ed3-a8a3-d430-5e78-0c51ebedf2db"), null, true, "Cuts", "food-beverage/meat-poultry/chicken/cuts", "cuts", 14, new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4") },
                    { new Guid("213295d3-c582-7f42-eb76-5f9df111e4d4"), null, true, "Undercaps", "modest-fashion/fashion-accessories/hijab-accessories/undercaps", "undercaps", 513, new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0") },
                    { new Guid("213a66d3-7dd2-e8ad-19f0-b16878bcf39f"), null, true, "Sour", "food-beverage/dairy-eggs/cream/sour", "sour", 72, new Guid("19b8c5d2-14a6-7f91-f997-7fc8edd229cb") },
                    { new Guid("219bd2d3-a1ff-6529-b0ff-63c33f23367f"), null, true, "Occasion", "modest-fashion/womens-clothing/abayas/occasion", "occasion", 469, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("21c8ecd3-8226-557a-ca27-c230216d0242"), null, true, "Salads", "food-beverage/ready-to-eat/sandwiches-salads/salads", "salads", 189, new Guid("6dc32ad2-10b9-7c94-7de5-f45feb94e073") },
                    { new Guid("21f801d3-9057-ea01-a19a-bf93a739d9af"), null, true, "Oud chips", "household/home-fragrance/traditional-fragrance/oud-chips", "oud-chips", 438, new Guid("927f1bd2-f1c8-b1ea-002a-d7eb533544f8") },
                    { new Guid("223c09d3-50bc-8b1f-7c45-d37a4a03d5f9"), null, true, "Telekung", "modest-fashion/womens-clothing/prayer-wear-women/telekung", "telekung", 480, new Guid("925043d2-44cd-d364-0cb2-08cd7d1e7d4b") },
                    { new Guid("2273fbd3-3bce-5793-5036-b4647949cfc8"), null, true, "Date-filled", "food-beverage/snacks-confectionery/chocolate/date-filled", "date-filled", 202, new Guid("058e65d2-fcd1-59a1-12be-14e797000487") },
                    { new Guid("227f82d3-5541-520b-101a-1f65dd156ea2"), null, true, "Still", "food-beverage/beverages/water/still", "still", 261, new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c") },
                    { new Guid("22937ed3-de03-cedd-9cd9-a719f73a2750"), null, true, "Towels", "household/home-essentials/textiles/towels", "towels", 459, new Guid("5e4f2ad2-aad9-95d2-dcda-da060568463d") },
                    { new Guid("232014d3-6c2e-b3ff-b247-c0f202ae50f4"), null, true, "Sardines", "food-beverage/ready-to-eat/canned-meals/sardines", "sardines", 180, new Guid("b2ea6bd2-3ee3-0222-1343-8abc7030d643") },
                    { new Guid("23ddf1d3-1cb4-4df9-531f-9a2252d2990c"), null, true, "Tomatoes", "food-beverage/fresh-produce/fresh-vegetables/tomatoes", "tomatoes", 129, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("24204ad3-061c-da05-1ce4-f3def8a4981d"), null, true, "Agar", "b2b/halal-ingredients/functional-ingredients/agar", "agar", 611, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("243f12d3-b3c6-f2e4-9900-de68b2da8deb"), null, true, "Marinated", "food-beverage/meat-poultry/beef/marinated", "marinated", 4, new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda") },
                    { new Guid("24aaf9d3-8192-9ce3-b2e9-eb53370014e8"), null, true, "Mukena", "modest-fashion/womens-clothing/prayer-wear-women/mukena", "mukena", 479, new Guid("925043d2-44cd-d364-0cb2-08cd7d1e7d4b") },
                    { new Guid("25c1e8d3-82e3-96ef-4550-02dde4b98d40"), null, true, "Fresh", "food-beverage/seafood/squid-octopus/fresh", "fresh", 43, new Guid("f12708d2-d7dd-f11c-ab9d-1fb251073f15") },
                    { new Guid("25c8b7d3-b697-dcec-b812-1c56f54ec96c"), null, true, "Goat milk", "baby-mother/baby-food-d7/formula-milk/goat-milk", "goat-milk", 569, new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd") },
                    { new Guid("2644a8d3-3e63-e079-c4be-dc0bf41e2e77"), null, true, "Tropical", "food-beverage/fresh-produce/fresh-fruits/tropical", "tropical", 119, new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99") },
                    { new Guid("264f42d3-8f9b-af15-f9ae-5c95010a3745"), null, true, "Kitchen", "household/cleaning/surface/kitchen", "kitchen", 422, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("269ca5d3-9f3b-49e9-90ee-00fd6ad0ba71"), null, true, "Shakes", "health-wellness/specialty-nutrition/meal-replacement/shakes", "shakes", 416, new Guid("9dd85cd2-edb8-7602-3bb7-46deb09f5ed9") },
                    { new Guid("26e3a1d3-7e9b-8368-b230-ca7ce65209cd"), null, true, "Sparkling", "food-beverage/beverages/water/sparkling", "sparkling", 262, new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c") },
                    { new Guid("28703ed3-a993-a9a5-de1d-a901343a4d0e"), null, true, "Cola", "food-beverage/beverages/soft-drinks/cola", "cola", 271, new Guid("ad02f5d2-9f01-75f9-35b3-1812a5c4f337") },
                    { new Guid("28c4a6d3-b473-9406-2c2d-35a5b6e624a8"), null, true, "Mint", "food-beverage/fresh-produce/fresh-herbs/mint", "mint", 134, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("29a3bfd3-89d0-e321-c566-e9b9373cf546"), null, true, "Halva", "food-beverage/bakery/traditional-desserts/halva", "halva", 230, new Guid("619bf8d2-13b6-8c7b-f4be-1292530d6762") },
                    { new Guid("2a0c9bd3-03f4-2d71-4ed4-6fe14f26050d"), null, true, "Oyster", "food-beverage/fresh-produce/mushrooms/oyster", "oyster", 140, new Guid("b2840bd2-908f-8c33-828d-79bad56d006c") },
                    { new Guid("2aa65ed3-f4d7-0bb9-1c5e-b6f9c2de17c6"), null, true, "Iron", "health-wellness/vitamins/by-type/iron", "iron", 375, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("2abc5bd3-b167-b84e-daaf-2b223c6d2180"), null, true, "Buckwheat", "food-beverage/rice-grains-staples/oats-grains/buckwheat", "buckwheat", 103, new Guid("1d30e6d2-c067-2177-403e-f82269279ff4") },
                    { new Guid("2ae300d3-ec1e-6b09-3b47-46b66d20ead7"), null, true, "Plant wax", "b2b/cosmetic-ingredients/waxes/plant-wax", "plant-wax", 633, new Guid("aab407d2-b3b4-55dc-b2e4-86cd8f991836") },
                    { new Guid("2b40d4d3-361f-5e4d-ee7a-c2f243401501"), null, true, "Hooks", "household/home-essentials/home-accessories/hooks", "hooks", 464, new Guid("18658ed2-2781-1d0f-7b71-645486a75898") },
                    { new Guid("2b9fa3d3-bb09-7b78-ca91-eafe0deb01c1"), null, true, "Bars", "health-wellness/specialty-nutrition/meal-replacement/bars", "bars", 417, new Guid("9dd85cd2-edb8-7602-3bb7-46deb09f5ed9") },
                    { new Guid("2c8af9d3-a7b0-8bb6-2a38-4809a162c012"), null, true, "Mango", "food-beverage/beverages/juice/mango", "mango", 268, new Guid("5592e0d2-546b-c966-3827-db3843f75cc0") },
                    { new Guid("2cb1d6d3-3ac7-f516-b597-a8898d7aabbb"), null, true, "Cow ghee", "food-beverage/oils-fats/ghee/cow-ghee", "cow-ghee", 256, new Guid("248af1d2-cb0e-0f0a-8f82-84c11c6caecf") },
                    { new Guid("2d48c3d3-89d9-411f-68e5-12db4568228b"), null, true, "Packet noodles", "food-beverage/ready-to-eat/instant-meals/packet-noodles", "packet-noodles", 185, new Guid("771918d2-92fb-f616-b356-33dc5d6ad7b8") },
                    { new Guid("2daa99d3-188a-a23d-f9d2-94107f4d9b19"), null, true, "Goat milk", "food-beverage/baby-food/infant-formula/goat-milk", "goat-milk", 302, new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161") },
                    { new Guid("2e4f7bd3-095e-4a66-e302-0e04e754e1eb"), null, true, "Food-grade oils", "b2b/halal-ingredients/processing/food-grade-oils", "food-grade-oils", 624, new Guid("e43f39d2-399a-bbbd-dddd-8c751a24b299") },
                    { new Guid("2e6c1ad3-3f3b-0f54-2f28-e88b917a48e4"), null, true, "Cleansers", "personal-care/skincare/facial/cleansers", "cleansers", 306, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("2e9aa6d3-389f-5dff-9e6c-d2dabcccbb51"), null, true, "Electric diffusers", "household/home-fragrance/active/electric", "electric", 436, new Guid("b455e9d2-1637-96bc-1d91-17eccff9ab22") },
                    { new Guid("2ed80ed3-e95e-c170-79ab-b72177100d74"), null, true, "Interdental", "personal-care/oral-care/tools/interdental", "interdental", 369, new Guid("7a4c51d2-3a04-a071-54c2-682511d05fb6") },
                    { new Guid("2ef526d3-8551-d80d-dc15-467da950cbe4"), null, true, "Wallets", "modest-fashion/fashion-accessories/bags-wallets/wallets", "wallets", 519, new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9") },
                    { new Guid("2f2f35d3-c2cf-0ade-278f-1a563a00057e"), null, true, "Baju Melayu", "modest-fashion/kids-clothing/boys/baju-melayu", "baju-melayu", 504, new Guid("436179d2-bcdc-5c7d-1b3b-dcee8c42358b") },
                    { new Guid("2f3241d3-449f-8489-66c5-bedc67a5fa75"), null, true, "Quran holder", "islamic-religious/quran/quran-accessories/quran-holder", "quran-holder", 539, new Guid("915801d2-844d-ec5d-fcc2-656ec7e8910e") },
                    { new Guid("2fcc9ed3-4af8-e24f-0ef4-0e0668378725"), null, true, "Whole", "food-beverage/meat-poultry/turkey/whole", "whole", 18, new Guid("ba652ad2-de24-d0b3-e225-cbdeb99d1e5f") },
                    { new Guid("2ff9ded3-01f7-2b3e-d755-efb290396a32"), null, true, "Dried", "food-beverage/fresh-produce/mushrooms/dried", "dried", 141, new Guid("b2840bd2-908f-8c33-828d-79bad56d006c") },
                    { new Guid("30befdd3-3a8a-705e-832b-6fbc8ea0a782"), null, true, "Brushes", "personal-care/cosmetics-makeup/tools/brushes", "brushes", 342, new Guid("5e90d9d2-661a-aeb2-6ba2-3904d1106e2f") },
                    { new Guid("30edecd3-d665-b533-80cf-4fb0211ab95a"), null, true, "Energy drink", "food-beverage/beverages/functional/energy-drink", "energy-drink", 289, new Guid("e3d391d2-ab2e-d995-1ae6-1b4059e0c915") },
                    { new Guid("30efb2d3-6fe2-c356-6810-b733fe7c1c6f"), null, true, "Velvet", "islamic-religious/prayer/prayer-mats/velvet", "velvet", 540, new Guid("bbb126d2-7508-6bb6-9881-5bc2d6be57d6") },
                    { new Guid("30ff1ad3-f490-54f8-4b00-59e5f065ab4a"), null, true, "Curry powder", "food-beverage/sauces-spices/spices/curry-powder", "curry-powder", 237, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("31f018d3-ba97-e0da-f1cd-0114ae06d8d3"), null, true, "Incense", "personal-care/fragrance/oud-attar/incense", "incense", 351, new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6") },
                    { new Guid("323e9ad3-4e11-a766-9ae8-d6aea8199641"), null, true, "Sports", "modest-fashion/womens-clothing/hijabs/sports", "sports", 475, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("3286e7d3-863d-b77d-e9d5-1e459ee9b8a1"), null, true, "Blood pressure", "health-wellness/healthcare/medical-devices/blood-pressure", "blood-pressure", 408, new Guid("a63cddd2-fee7-1d9f-eacc-a5d6a951ca38") },
                    { new Guid("32884fd3-d556-54c4-66ab-fde73692c8e9"), null, true, "Hair masks", "personal-care/haircare/treatments-styling/hair-masks", "hair-masks", 324, new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a") },
                    { new Guid("32ffa5d3-22bd-c9d4-2acd-398e08177ec6"), null, true, "Handbags", "modest-fashion/fashion-accessories/bags-wallets/handbags", "handbags", 515, new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9") },
                    { new Guid("330583d3-fd4c-d997-175d-be57cedce10f"), null, true, "Bandages", "health-wellness/healthcare/first-aid/bandages", "bandages", 403, new Guid("49e85fd2-dc0c-2e5c-6284-5669988a659f") },
                    { new Guid("335476d3-ea27-6a9a-af7b-355b82c0eb98"), null, true, "BCAAs", "health-wellness/specialty-nutrition/sports/bcaas", "bcaas", 414, new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb") },
                    { new Guid("336310d3-2d3e-513f-803d-39bdbc4e4592"), null, true, "Ice cream bars", "food-beverage/dairy-eggs/ice-cream/bars", "bars", 75, new Guid("2ea98cd2-0b7e-5ed3-6c9d-22c5096ae03a") },
                    { new Guid("339aacd3-709d-f728-88d7-1d9e19bdb104"), null, true, "Barley", "food-beverage/rice-grains-staples/oats-grains/barley", "barley", 101, new Guid("1d30e6d2-c067-2177-403e-f82269279ff4") },
                    { new Guid("3414a0d3-6a49-e45f-65c3-b4d9849715af"), null, true, "Beef stock", "food-beverage/sauces-spices/stock/beef-stock", "beef-stock", 246, new Guid("3120ffd2-9ab4-a2ce-5185-1bb4c691f055") },
                    { new Guid("3519b3d3-8e43-deac-d0d2-a36df6029d46"), null, true, "Tongkat ali", "health-wellness/herbal-traditional/herbal-supps/tongkat-ali", "tongkat-ali", 386, new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f") },
                    { new Guid("3539d9d3-3f83-0968-79a2-568220d20de6"), null, true, "UHT / long-life", "food-beverage/dairy-eggs/milk/uht", "uht", 54, new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff") },
                    { new Guid("3585f9d3-fb01-ee93-20d4-efcdbc2efbc0"), null, true, "Soup", "food-beverage/ready-to-eat/ready-meals/soup", "soup", 179, new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4") },
                    { new Guid("36726ed3-2b28-0f0f-daff-e1b631e1f46c"), null, true, "Plain", "food-beverage/dairy-eggs/yogurt/plain", "plain", 63, new Guid("fe0861d2-40c3-d2b1-0b67-1d20ee82a28f") },
                    { new Guid("374c0dd3-3102-39a1-879e-a54f4995c541"), null, true, "Zinc", "health-wellness/vitamins/by-type/zinc", "zinc", 377, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("377dfdd3-4d53-a618-0fa4-2128969e0cf3"), null, true, "Bulk food", "b2b/horeca/catering/bulk-food", "bulk-food", 647, new Guid("6d5f2dd2-bc68-2e75-a3fe-adac70a08b37") },
                    { new Guid("380cadd3-98f5-3e07-6c6b-52619bac0239"), null, true, "Utensils", "baby-mother/baby-essentials/feeding/utensils", "utensils", 598, new Guid("ad53b4d2-044d-b4ad-4755-0ed5f1105690") },
                    { new Guid("38b00bd3-95db-f9ce-ee96-02e829698193"), null, true, "Cultured", "food-beverage/dairy-eggs/butter-ghee/cultured", "cultured", 69, new Guid("9b492ad2-d17e-a059-c279-80c4654425d6") },
                    { new Guid("38bfdcd3-3eac-b1e5-0757-4f76abe8d510"), null, true, "Beef", "food-beverage/frozen-food/frozen-meat/beef", "beef", 150, new Guid("15119fd2-bb73-0f1c-212d-ef653e83fa23") },
                    { new Guid("38e6f6d3-0371-7b69-b507-4306f2e89a18"), null, true, "Apple", "food-beverage/beverages/juice/apple", "apple", 267, new Guid("5592e0d2-546b-c966-3827-db3843f75cc0") },
                    { new Guid("394913d3-982c-e650-147a-e2c2032154ee"), null, true, "Vegetable", "food-beverage/snacks-confectionery/chips/vegetable", "vegetable", 194, new Guid("328929d2-a919-8811-3c80-20c5fd6981ff") },
                    { new Guid("3956e1d3-548e-03f4-3b2c-5f03eef65c97"), null, true, "Peppermint", "health-wellness/herbal-traditional/herbal-teas/peppermint", "peppermint", 400, new Guid("9be6d2d2-078d-fa70-816b-665c204e5482") },
                    { new Guid("397669d3-b6fe-1765-eabe-532e806680cf"), null, true, "Disinfectant", "household/cleaning/surface/disinfectant", "disinfectant", 426, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("3a36b0d3-cdd3-6afb-7bdb-d95de8bcb938"), null, true, "Protein", "food-beverage/snacks-confectionery/snack-bars/protein", "protein", 214, new Guid("5c9b1dd2-9b94-dcc8-73c1-42d054df30d5") },
                    { new Guid("3a930bd3-7827-3639-0514-2045a09eed60"), null, true, "Ghee", "food-beverage/dairy-eggs/butter-ghee/ghee", "ghee", 68, new Guid("9b492ad2-d17e-a059-c279-80c4654425d6") },
                    { new Guid("3af760d3-c8e6-0137-fe8e-eb434f4feb7f"), null, true, "Chicken", "food-beverage/frozen-food/frozen-meat/chicken", "chicken", 151, new Guid("15119fd2-bb73-0f1c-212d-ef653e83fa23") },
                    { new Guid("3b2c43d3-8a6c-a437-016f-a4fec3417a6c"), null, true, "Canned", "food-beverage/seafood/fish/canned", "canned", 32, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("3bc531d3-7433-9b85-0b54-8c9bd1699c2f"), null, true, "Detergent", "household/cleaning/laundry/detergent", "detergent", 427, new Guid("6b4e2fd2-fb7e-d6b7-99e7-1348fa428a43") },
                    { new Guid("3bcae0d3-c5d8-5232-1093-7773d509fbf8"), null, true, "Ma'amoul", "food-beverage/bakery/traditional-desserts/maamoul", "maamoul", 231, new Guid("619bf8d2-13b6-8c7b-f4be-1292530d6762") },
                    { new Guid("3c0727d3-fc0f-f8ff-d2c3-e615ce7170f4"), null, true, "Niacinamide", "b2b/cosmetic-ingredients/extracts/niacinamide", "niacinamide", 638, new Guid("c278d2d2-6301-8192-242e-145233b94bfa") },
                    { new Guid("3cabd5d3-0258-9dca-0062-bf878eeba8bb"), null, true, "Orange", "food-beverage/beverages/juice/orange", "orange", 266, new Guid("5592e0d2-546b-c966-3827-db3843f75cc0") },
                    { new Guid("3cd1c8d3-0bf7-f009-b181-62f013723b17"), null, true, "Ginger", "health-wellness/herbal-traditional/herbal-supps/ginger", "ginger", 388, new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f") },
                    { new Guid("3d2e71d3-4f70-dcbd-24fc-91d77166a31d"), null, true, "Tasbih", "islamic-religious/prayer/prayer-accessories/tasbih", "tasbih", 544, new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5") },
                    { new Guid("3d537fd3-e8b5-9cac-9b61-9e255c7641be"), null, true, "Chiffon", "modest-fashion/womens-clothing/hijabs/chiffon", "chiffon", 471, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("3eb72fd3-7f39-5471-d977-06ee4ab5b75f"), null, true, "Sorbet", "food-beverage/dairy-eggs/ice-cream/sorbet", "sorbet", 76, new Guid("2ea98cd2-0b7e-5ed3-6c9d-22c5096ae03a") },
                    { new Guid("3ee673d3-6ac7-8f98-de4e-0a6744821e61"), null, true, "Digital tasbih", "islamic-religious/prayer/prayer-accessories/digital-tasbih", "digital-tasbih", 545, new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5") },
                    { new Guid("3eef20d3-70d6-5288-7ad6-469c322a3a71"), null, true, "Saffron", "food-beverage/sauces-spices/spices/saffron", "saffron", 243, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("3f1b09d3-c319-e112-346c-490b4ca37730"), null, true, "Feed additives", "b2b/pet-animal-b2b/animal-feed/feed-additives", "feed-additives", 669, new Guid("801120d2-55d3-1b47-cc42-72a206df3883") },
                    { new Guid("3f47dad3-450b-2214-8650-1db5d96e41bf"), null, true, "Oat milk", "food-beverage/beverages/plant-drinks/oat-milk", "oat-milk", 292, new Guid("aa7cacd2-f2a0-11fe-8505-69723bb65d6a") },
                    { new Guid("3f8720d3-a726-2d09-005b-5d84c1f7d3fd"), null, true, "Kufi", "modest-fashion/mens-clothing/prayer-wear-men/kufi", "kufi", 497, new Guid("669517d2-a3ae-d7e1-f93f-536676cb582e") },
                    { new Guid("3f8f75d3-2b03-9ecc-e843-131635bc3c2b"), null, true, "Swaddles", "baby-mother/baby-essentials/sleep-comfort/swaddles", "swaddles", 601, new Guid("1b553bd2-c435-79ce-5d72-e4de2539c86c") },
                    { new Guid("40529ed3-2fd6-66e0-4b2f-9158fc9a7984"), null, true, "Bath salts", "personal-care/bath-body/spa/bath-salts", "bath-salts", 358, new Guid("f1ea79d2-5a1b-9032-e4da-39a839c285fa") },
                    { new Guid("40676cd3-cf5a-3c61-b552-6a11b317825a"), null, true, "Pots", "household/kitchen/cookware/pots", "pots", 441, new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652") },
                    { new Guid("40899ed3-7ee1-3329-298e-2334d1debd5c"), null, true, "Palm oil", "food-beverage/oils-fats/cooking-oils/palm-oil", "palm-oil", 254, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("409e27d3-d408-ad50-9053-b4758097998b"), null, true, "Children's", "islamic-religious/prayer/prayer-mats/children", "children", 543, new Guid("bbb126d2-7508-6bb6-9881-5bc2d6be57d6") },
                    { new Guid("416664d3-4b90-7140-a366-30653a6d2fdd"), null, true, "Eucalyptus", "health-wellness/herbal-traditional/essential-oils/eucalyptus", "eucalyptus", 397, new Guid("08a7a9d2-8e75-3cda-513e-9cc91b3b6c3e") },
                    { new Guid("41e72ed3-a43c-a63e-8e3d-4bdb867ec6ba"), null, true, "Wipes warmers", "baby-mother/baby-essentials/baby-accessories/wipes-warmers", "wipes-warmers", 605, new Guid("68bad0d2-8a96-04b9-1552-1cd110ab4a02") },
                    { new Guid("42b15ed3-8905-7426-bb43-7bca4d4ab476"), null, true, "Minced", "food-beverage/meat-poultry/lamb-mutton/minced", "minced", 8, new Guid("49b9e3d2-e1f2-e025-e046-dec8531eb5b6") },
                    { new Guid("430af7d3-d0a7-a79a-51e4-1afdbed61a6c"), null, true, "Oat", "food-beverage/dairy-eggs/milk-alt/oat", "oat", 78, new Guid("e67ff7d2-9d3d-e8b9-e8b9-388ad6a76031") },
                    { new Guid("432567d3-bbe4-00b3-92cb-6e7db924d534"), null, true, "Whole", "food-beverage/seafood/lobster/whole", "whole", 41, new Guid("63295ad2-f567-52ad-739d-a4ba5fb7d082") },
                    { new Guid("433f0cd3-cfa2-512b-5221-fc984f87e1f4"), null, true, "Storage boxes", "household/home-essentials/home-accessories/storage-boxes", "storage-boxes", 462, new Guid("18658ed2-2781-1d0f-7b71-645486a75898") },
                    { new Guid("444b28d3-326a-c824-dbbd-8f0e1c90922f"), null, true, "Sweeteners", "b2b/halal-ingredients/additives/sweeteners", "sweeteners", 622, new Guid("f62090d2-8c0b-d9c7-2cdc-a63d3ec59e8c") },
                    { new Guid("44d1edd3-874a-2726-c1d6-abd93ffae36b"), null, true, "Follow-on", "baby-mother/baby-food-d7/formula-milk/follow-on", "follow-on", 567, new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd") },
                    { new Guid("4592c3d3-d821-067b-1d5a-60d5aedf4f00"), null, true, "Muslim-friendly toiletries", "b2b/horeca/hotel/toiletries", "toiletries", 651, new Guid("47d502d2-644a-bca0-fa90-41882f752db5") },
                    { new Guid("45ec8bd3-f6fe-d393-118e-01f6932f5913"), null, true, "Rings", "food-beverage/seafood/squid-octopus/rings", "rings", 45, new Guid("f12708d2-d7dd-f11c-ab9d-1fb251073f15") },
                    { new Guid("46d65ad3-0315-ea2b-48f1-56b7164f2556"), null, true, "Lemonade", "food-beverage/beverages/soft-drinks/lemonade", "lemonade", 272, new Guid("ad02f5d2-9f01-75f9-35b3-1812a5c4f337") },
                    { new Guid("46f6c4d3-1271-2a89-ec8e-f1de3e0d8350"), null, true, "Sponges", "household/cleaning/dishwashing/sponges", "sponges", 432, new Guid("d213fad2-d5e5-a59a-a642-5c63c488948d") },
                    { new Guid("479132d3-cf06-b379-9a6b-bf081c010fbf"), null, true, "Vegan", "food-beverage/dairy-eggs/cheese/vegan", "vegan", 62, new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5") },
                    { new Guid("47a3dbd3-04c1-bc0a-00e4-4d510cc56705"), null, true, "Foam", "islamic-religious/prayer/prayer-mats/foam", "foam", 541, new Guid("bbb126d2-7508-6bb6-9881-5bc2d6be57d6") },
                    { new Guid("47bfbad3-c5ee-2218-0211-e1871c56f682"), null, true, "Beeswax", "b2b/cosmetic-ingredients/waxes/beeswax", "beeswax", 632, new Guid("aab407d2-b3b4-55dc-b2e4-86cd8f991836") },
                    { new Guid("47cc08d3-e7bf-9d08-2135-ec9463612eba"), null, true, "Teethers", "baby-mother/baby-essentials/baby-accessories/teethers", "teethers", 604, new Guid("68bad0d2-8a96-04b9-1552-1cd110ab4a02") },
                    { new Guid("48e168d3-9265-139f-a429-1d36886b3770"), null, true, "Wash", "personal-care/bath-body/feminine/wash", "wash", 362, new Guid("d81fdbd2-0284-97c3-41ca-677a4e85a95d") },
                    { new Guid("4916e5d3-cd3d-9742-a048-4c37626be250"), null, true, "Pacifiers", "baby-mother/baby-essentials/feeding/pacifiers", "pacifiers", 597, new Guid("ad53b4d2-044d-b4ad-4755-0ed5f1105690") },
                    { new Guid("49194ed3-9f7a-3ddc-aed4-09d1d3450d48"), null, true, "Turmeric", "food-beverage/sauces-spices/spices/turmeric", "turmeric", 238, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("49203fd3-6120-569b-9986-3dbf1a8ebcdf"), null, true, "Eyeliner", "personal-care/cosmetics-makeup/eyes/eyeliner", "eyeliner", 335, new Guid("3a5915d2-dcfd-9506-e745-953bab655b81") },
                    { new Guid("49224cd3-294e-3931-1100-32da9640561e"), null, true, "Cold chain", "b2b/wholesale/by-logistics/cold-chain", "cold-chain", 663, new Guid("be412ad2-16ea-0a0b-80cc-e870767baf55") },
                    { new Guid("499b47d3-d210-da4c-cf99-086e241f7e37"), null, true, "Amino acids", "health-wellness/vitamins/specialty/amino-acids", "amino-acids", 380, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("49a719d3-a17d-1ccf-23bd-789a21c93019"), null, true, "Magnets", "modest-fashion/fashion-accessories/hijab-accessories/magnets", "magnets", 512, new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0") },
                    { new Guid("49eceed3-9054-35b7-3b52-974b9ffbf29c"), null, true, "Clams", "food-beverage/seafood/shellfish/clams", "clams", 48, new Guid("97a1a7d2-a92e-9df3-383d-53872a3dd6a6") },
                    { new Guid("4a2c69d3-26c1-a560-96fc-faec5205d2d6"), null, true, "Brightening", "personal-care/skincare/treatments/brightening", "brightening", 319, new Guid("269c54d2-b716-467c-5d4f-9b5afacd0ff2") },
                    { new Guid("4a5b1bd3-e5ae-e190-618d-afdae8284040"), null, true, "Whole", "food-beverage/seafood/crab/whole", "whole", 38, new Guid("400cf4d2-820e-70fa-0802-540d54203b6e") },
                    { new Guid("4ad177d3-f7dc-8535-7ce6-82edf3fba324"), null, true, "Donuts", "food-beverage/bakery/cakes/donuts", "donuts", 226, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("4b75fdd3-8db6-7a07-bb2f-0ce17e45dd8f"), null, true, "Layer cake", "food-beverage/bakery/cakes/layer-cake", "layer-cake", 222, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("4b9551d3-dfe8-0234-8521-410d17bfb5a5"), null, true, "Crossbody", "modest-fashion/fashion-accessories/bags-wallets/crossbody", "crossbody", 516, new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9") },
                    { new Guid("4c169bd3-624a-a5cc-db43-a78de789c18c"), null, true, "Mixed veg", "food-beverage/frozen-food/frozen-veg/mixed-veg", "mixed-veg", 159, new Guid("2e6b16d2-a533-72a3-5ecb-d4258815635a") },
                    { new Guid("4c496fd3-08f0-d079-0bb4-9063bc25047a"), null, true, "Sports drink", "food-beverage/beverages/functional/sports-drink", "sports-drink", 288, new Guid("e3d391d2-ab2e-d995-1ae6-1b4059e0c915") },
                    { new Guid("4cac22d3-926e-c802-5c1a-c04af1795726"), null, true, "Gummies", "food-beverage/snacks-confectionery/candy/gummies", "gummies", 203, new Guid("4648f2d2-08d7-49f0-6a55-f68e9604a94b") },
                    { new Guid("4d0881d3-551b-c214-bef0-f40168e7e061"), null, true, "Styling", "personal-care/haircare/treatments-styling/styling", "styling", 326, new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a") },
                    { new Guid("4d7435d3-2fe8-c4e2-e8b5-64776a52a8cb"), null, true, "Lamb", "food-beverage/frozen-food/frozen-meat/lamb", "lamb", 152, new Guid("15119fd2-bb73-0f1c-212d-ef653e83fa23") },
                    { new Guid("4dbfe0d3-3723-38b0-102f-bf7d4d15431b"), null, true, "Cubed", "food-beverage/meat-poultry/lamb-mutton/cubed", "cubed", 7, new Guid("49b9e3d2-e1f2-e025-e046-dec8531eb5b6") },
                    { new Guid("4e35cfd3-eb17-11ac-7bde-071d4de500dc"), null, true, "Basmati", "food-beverage/rice-grains-staples/rice/basmati", "basmati", 89, new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926") },
                    { new Guid("4e7a9dd3-f899-7067-c6dc-b1164867f567"), null, true, "Dental floss", "personal-care/oral-care/tools/floss", "floss", 368, new Guid("7a4c51d2-3a04-a071-54c2-682511d05fb6") },
                    { new Guid("4ed1a4d3-f8f7-0b24-9f06-619a3e7595cd"), null, true, "Bakhoor", "household/home-fragrance/traditional-fragrance/bakhoor", "bakhoor", 437, new Guid("927f1bd2-f1c8-b1ea-002a-d7eb533544f8") },
                    { new Guid("4f0728d3-eb28-06b5-1be1-be6bc4f02f18"), null, true, "Activity books", "islamic-religious/islamic-books/children-islamic/activity-books", "activity-books", 554, new Guid("99bab0d2-8e11-eafa-8f08-3c719acdca01") },
                    { new Guid("4f2120d3-6e85-3fa9-e37a-9f3ad2356459"), null, true, "Qibla products", "b2b/horeca/hotel/qibla-products", "qibla-products", 653, new Guid("47d502d2-644a-bca0-fa90-41882f752db5") },
                    { new Guid("4f5776d3-83e5-5bef-bdd4-74c6c3f83beb"), null, true, "Cultures", "b2b/halal-ingredients/processing/cultures", "cultures", 625, new Guid("e43f39d2-399a-bbbd-dddd-8c751a24b299") },
                    { new Guid("4f8c89d3-87c3-3b1a-4ce7-e821fdca0e38"), null, true, "Rice vermicelli", "food-beverage/rice-grains-staples/pasta-noodles/rice-vermicelli", "rice-vermicelli", 109, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("4fc63bd3-0d3a-e3ce-f8d2-c90f41489448"), null, true, "Pasta", "food-beverage/ready-to-eat/ready-meals/pasta", "pasta", 178, new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4") },
                    { new Guid("50495fd3-c7c6-e12d-d147-40728e65c61d"), null, true, "T-shirts", "modest-fashion/kids-clothing/boys/t-shirts", "t-shirts", 506, new Guid("436179d2-bcdc-5c7d-1b3b-dcee8c42358b") },
                    { new Guid("51e89bd3-b937-c2c4-f316-2a4bba9d1b2c"), null, true, "Chicken stock", "food-beverage/sauces-spices/stock/chicken-stock", "chicken-stock", 245, new Guid("3120ffd2-9ab4-a2ce-5185-1bb4c691f055") },
                    { new Guid("526c1ed3-63f4-9d54-4f42-9f977ea9b65b"), null, true, "Quran cover", "islamic-religious/quran/quran-accessories/quran-cover", "quran-cover", 537, new Guid("915801d2-844d-ec5d-fcc2-656ec7e8910e") },
                    { new Guid("52bfe5d3-2f6c-5e28-f810-638a7427eb9f"), null, true, "Cereal", "food-beverage/baby-food/baby-food/cereal", "cereal", 296, new Guid("d4d96bd2-6008-bd2c-4d7e-2066a922817b") },
                    { new Guid("52e53cd3-0f4d-5bae-d917-4613b3b342bc"), null, true, "Capsules", "food-beverage/beverages/coffee/capsules", "capsules", 285, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("52e832d3-a703-a4fd-1047-8fddf92eff33"), null, true, "Marshmallows", "food-beverage/snacks-confectionery/candy/marshmallows", "marshmallows", 206, new Guid("4648f2d2-08d7-49f0-6a55-f68e9604a94b") },
                    { new Guid("5302cad3-1115-7a15-ae64-237cb25ececc"), null, true, "Buns & rolls", "food-beverage/bakery/bread/buns-rolls", "buns-rolls", 221, new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3") },
                    { new Guid("53c12dd3-f0e6-1c1a-76bc-bcd08a46c113"), null, true, "Plain", "food-beverage/snacks-confectionery/biscuits/plain", "plain", 195, new Guid("46a81bd2-a65c-b762-cb53-60d7efb6a019") },
                    { new Guid("53dff5d3-3205-fe29-e79e-b51ad8ee3a00"), null, true, "Shiitake", "food-beverage/fresh-produce/mushrooms/shiitake", "shiitake", 139, new Guid("b2840bd2-908f-8c33-828d-79bad56d006c") },
                    { new Guid("54a234d3-93f3-2580-1b28-53b2638ea805"), null, true, "Qibla compass", "islamic-religious/prayer/prayer-accessories/qibla-compass", "qibla-compass", 546, new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5") },
                    { new Guid("552f81d3-408c-856f-5768-152acb0c4610"), null, true, "Thermometers", "health-wellness/healthcare/medical-devices/thermometers", "thermometers", 407, new Guid("a63cddd2-fee7-1d9f-eacc-a5d6a951ca38") },
                    { new Guid("55b9ebd3-cd46-cda7-da43-5751ee070ba3"), null, true, "Scalp care", "personal-care/haircare/treatments-styling/scalp-care", "scalp-care", 328, new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a") },
                    { new Guid("564957d3-ef8a-dfad-bfa4-07469b20730c"), null, true, "Mukena", "modest-fashion/kids-clothing/kids-prayer/mukena", "mukena", 508, new Guid("f3c106d2-bdc8-4e80-9e7a-1baf621d9d0e") },
                    { new Guid("56d717d3-9bd5-3f17-756e-21fb1fc2a88e"), null, true, "Nursing wear", "baby-mother/maternity/nursing/nursing-wear", "nursing-wear", 589, new Guid("69bea1d2-68d0-38f0-464f-17bbc431cef0") },
                    { new Guid("570887d3-eec5-ef9f-f7b9-a7f562243e05"), null, true, "Frozen", "food-beverage/meat-poultry/chicken/frozen", "frozen", 15, new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4") },
                    { new Guid("57328cd3-3cec-cc45-0da1-bafeeff8fa49"), null, true, "Emirati", "modest-fashion/mens-clothing/thobes/emirati", "emirati", 487, new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f") },
                    { new Guid("574abbd3-24c6-dfe5-8592-ef6dfeea2df5"), null, true, "Double", "food-beverage/dairy-eggs/cream/double", "double", 71, new Guid("19b8c5d2-14a6-7f91-f997-7fc8edd229cb") },
                    { new Guid("5794e0d3-6cd5-d57e-7e53-c0ff2a89e613"), null, true, "Bronzer", "personal-care/cosmetics-makeup/face/bronzer", "bronzer", 333, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("580e7cd3-19d4-cc8d-f00d-147b2cbc0efe"), null, true, "Avocado oil", "food-beverage/oils-fats/cooking-oils/avocado-oil", "avocado-oil", 255, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("581f3ad3-7a86-1e60-1d49-a943116258ff"), null, true, "Tote", "modest-fashion/fashion-accessories/bags-wallets/tote", "tote", 517, new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9") },
                    { new Guid("5987d4d3-cb80-8aa4-2387-8e46d396752e"), null, true, "Tonic", "food-beverage/beverages/soft-drinks/tonic", "tonic", 273, new Guid("ad02f5d2-9f01-75f9-35b3-1812a5c4f337") },
                    { new Guid("59981dd3-bebc-3ce9-e5a7-a25aee6f8b98"), null, true, "Fermentation", "b2b/halal-ingredients/processing/fermentation", "fermentation", 626, new Guid("e43f39d2-399a-bbbd-dddd-8c751a24b299") },
                    { new Guid("59a0f9d3-338c-e9aa-bbf2-f1d2636ac5a1"), null, true, "Body wash", "personal-care/bath-body/cleansing/body-wash", "body-wash", 353, new Guid("757aeed2-5da0-eb6a-e049-17f920363d1c") },
                    { new Guid("59d287d3-3b7f-ec75-a096-5fb543c21e36"), null, true, "Potato", "food-beverage/snacks-confectionery/chips/potato", "potato", 191, new Guid("328929d2-a919-8811-3c80-20c5fd6981ff") },
                    { new Guid("59e5c1d3-f8cb-98b6-87b5-f63043ce28f8"), null, true, "Tails", "food-beverage/seafood/lobster/tails", "tails", 42, new Guid("63295ad2-f567-52ad-739d-a4ba5fb7d082") },
                    { new Guid("5a2c49d3-fee5-16ea-aa04-3d73f412a13f"), null, true, "Sandwich", "food-beverage/snacks-confectionery/biscuits/sandwich", "sandwich", 198, new Guid("46a81bd2-a65c-b762-cb53-60d7efb6a019") },
                    { new Guid("5a5db5d3-b0fb-b267-df0d-06c7745d4b37"), null, true, "Whisks", "household/kitchen/utensils/whisks", "whisks", 451, new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380") },
                    { new Guid("5b0503d3-4cd5-6054-cd0e-a7b2b0191286"), null, true, "Baklava", "food-beverage/bakery/traditional-desserts/baklava", "baklava", 228, new Guid("619bf8d2-13b6-8c7b-f4be-1292530d6762") },
                    { new Guid("5bc524d3-a780-d003-819c-fec9f476dd70"), null, true, "Everyday", "modest-fashion/womens-clothing/abayas/everyday", "everyday", 468, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("5c1322d3-c440-05bf-984f-fa58b911d351"), null, true, "Saudi", "modest-fashion/mens-clothing/thobes/saudi", "saudi", 486, new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f") },
                    { new Guid("5d5f62d3-6517-3c99-eb88-aeb9737348f7"), null, true, "Body wash", "baby-mother/baby-care/bath-skin/body-wash", "body-wash", 577, new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd") },
                    { new Guid("5de9cad3-892e-efb9-4da3-cb91e70159e4"), null, true, "Sarong", "modest-fashion/kids-clothing/kids-prayer/sarong", "sarong", 507, new Guid("f3c106d2-bdc8-4e80-9e7a-1baf621d9d0e") },
                    { new Guid("5ee59ed3-51c3-4319-18c5-9aae20811c09"), null, true, "Frankincense", "health-wellness/herbal-traditional/essential-oils/frankincense", "frankincense", 398, new Guid("08a7a9d2-8e75-3cda-513e-9cc91b3b6c3e") },
                    { new Guid("6094afd3-f6d3-ad14-d148-10c8207025d4"), null, true, "Foot care", "personal-care/skincare/body-care/foot-care", "foot-care", 316, new Guid("1e9766d2-8e14-bc32-0e66-d58542d25d58") },
                    { new Guid("60e534d3-f349-d4c1-bc3b-0f50666a2c29"), null, true, "Cashews", "food-beverage/snacks-confectionery/nuts-seeds/cashews", "cashews", 208, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("6218a5d3-9b43-0e73-bdd4-4143e59d6a93"), null, true, "Wraps", "food-beverage/ready-to-eat/sandwiches-salads/wraps", "wraps", 188, new Guid("6dc32ad2-10b9-7c94-7de5-f45feb94e073") },
                    { new Guid("62772bd3-d952-dcdf-416d-95ff49e02eb2"), null, true, "Packaging design", "b2b/oem/by-service/packaging-design", "packaging-design", 659, new Guid("980928d2-75e3-08f0-1ccd-a25d3962cc24") },
                    { new Guid("62a46ad3-a730-071f-98d0-9d7fa09bb8bf"), null, true, "2-in-1", "personal-care/haircare/shampoo-conditioner/2-in-1", "2-in-1", 322, new Guid("401953d2-1437-5628-d42c-e4c6f4a1a243") },
                    { new Guid("62eaf1d3-8bff-fbb4-921f-9ad2d6b33387"), null, true, "Fresh cuts", "food-beverage/meat-poultry/beef/fresh-cuts", "fresh-cuts", 1, new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda") },
                    { new Guid("6300d6d3-e477-5e60-cd53-b2acc0287ce4"), null, true, "On the bone", "food-beverage/meat-poultry/lamb-mutton/on-the-bone", "on-the-bone", 9, new Guid("49b9e3d2-e1f2-e025-e046-dec8531eb5b6") },
                    { new Guid("630463d3-046e-cec3-ffa5-0cd7f5a88f6b"), null, true, "Protein", "health-wellness/vitamins/specialty/protein", "protein", 379, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("630551d3-2916-9440-ab70-de8ca24b59b5"), null, true, "Sunglasses", "modest-fashion/fashion-accessories/belts/sunglasses", "sunglasses", 521, new Guid("66c580d2-4291-9f23-abdb-048c5fb2a2ed") },
                    { new Guid("630f0bd3-e351-1c14-4d5c-9cc76eb51566"), null, true, "Scallops", "food-beverage/seafood/shellfish/scallops", "scallops", 49, new Guid("97a1a7d2-a92e-9df3-383d-53872a3dd6a6") },
                    { new Guid("632402d3-a7df-ef8a-7b91-c1392330f680"), null, true, "Panna cotta", "food-beverage/dairy-eggs/dairy-desserts/panna-cotta", "panna-cotta", 86, new Guid("aab9bfd2-a883-6348-6215-92e1aaca2a0c") },
                    { new Guid("6351f7d3-2848-9695-d704-d9cb36438703"), null, true, "Arabic", "islamic-religious/quran/qurans/arabic", "arabic", 531, new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf") },
                    { new Guid("63da27d3-90fd-ae70-1aa8-4510c1deff35"), null, true, "Metal", "islamic-religious/islamic-gifts/wall-art/metal", "metal", 559, new Guid("a90d41d2-3ae5-4939-f0d5-4cff704ba5ce") },
                    { new Guid("63ee35d3-fdff-cf96-4c78-e0233309ca4d"), null, true, "Kimono-cut", "modest-fashion/womens-clothing/abayas/kimono-cut", "kimono-cut", 470, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("647093d3-8ac1-7b1d-20fc-deb565aeea5a"), null, true, "Digital Quran", "islamic-religious/quran/quran-accessories/digital-quran", "digital-quran", 538, new Guid("915801d2-844d-ec5d-fcc2-656ec7e8910e") },
                    { new Guid("647e33d3-eb0c-3b58-126d-20dc6f6a541a"), null, true, "Rolled oats", "food-beverage/rice-grains-staples/oats-grains/rolled-oats", "rolled-oats", 99, new Guid("1d30e6d2-c067-2177-403e-f82269279ff4") },
                    { new Guid("6481a1d3-a830-a088-54c0-c6269e1a8dc6"), null, true, "Hard candy", "food-beverage/snacks-confectionery/candy/hard-candy", "hard-candy", 204, new Guid("4648f2d2-08d7-49f0-6a55-f68e9604a94b") },
                    { new Guid("649d56d3-78cd-3f9d-b7bd-67eba81537e5"), null, true, "Canned fish", "food-beverage/seafood/canned-preserved/canned-fish", "canned-fish", 50, new Guid("b3ecf0d2-691f-ff7d-507a-e934cf8e93dc") },
                    { new Guid("654569d3-f129-990c-23d0-198f2547f0f1"), null, true, "Sterilisers", "baby-mother/baby-essentials/feeding/sterilisers", "sterilisers", 599, new Guid("ad53b4d2-044d-b4ad-4755-0ed5f1105690") },
                    { new Guid("65c08ad3-a31b-fdaf-23fd-d9e4513c9535"), null, true, "Scented candles", "household/home-fragrance/active/candles", "candles", 435, new Guid("b455e9d2-1637-96bc-1d91-17eccff9ab22") },
                    { new Guid("668522d3-5c58-1763-e437-2b021c75be69"), null, true, "Wholemeal", "food-beverage/bakery/bread/wholemeal", "wholemeal", 219, new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3") },
                    { new Guid("671d18d3-4cce-f70a-1537-fcb02e691c95"), null, true, "Open", "modest-fashion/womens-clothing/abayas/open", "open", 466, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("67398fd3-c52e-df4c-781f-13f34c93b6e7"), null, true, "Sambal", "food-beverage/sauces-spices/sauces/sambal", "sambal", 236, new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817") },
                    { new Guid("677ec7d3-3dda-ca71-71ca-866235c693d8"), null, true, "Hand wash", "personal-care/bath-body/cleansing/hand-wash", "hand-wash", 354, new Guid("757aeed2-5da0-eb6a-e049-17f920363d1c") },
                    { new Guid("67c218d3-d53b-0be6-0b90-a387c4315435"), null, true, "Bar soap", "personal-care/bath-body/cleansing/bar-soap", "bar-soap", 355, new Guid("757aeed2-5da0-eb6a-e049-17f920363d1c") },
                    { new Guid("67e1b8d3-7fa9-1853-39fb-96e1be114cfb"), null, true, "Sandals", "modest-fashion/footwear/by-type/sandals", "sandals", 523, new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e") },
                    { new Guid("680154d3-ddec-d1d5-795a-75f1913169f4"), null, true, "Blush", "personal-care/cosmetics-makeup/face/blush", "blush", 332, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("68991cd3-b17a-7e70-775d-cc76bc4017dd"), null, true, "Bathroom", "household/cleaning/surface/bathroom", "bathroom", 423, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("6943a0d3-12d8-055b-e9d0-eebf12b6e6e1"), null, true, "Chili sauce", "food-beverage/sauces-spices/sauces/chili-sauce", "chili-sauce", 232, new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817") },
                    { new Guid("697657d3-847f-31e1-86f1-41a503abe50c"), null, true, "Mass gainer", "health-wellness/specialty-nutrition/sports/mass-gainer", "mass-gainer", 413, new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb") },
                    { new Guid("698ecfd3-a815-73bb-5529-dd727e3de59e"), null, true, "Pita", "food-beverage/rice-grains-staples/bread-wraps/pita", "pita", 116, new Guid("99dddfd2-9738-7b86-9f78-b555b77d130c") },
                    { new Guid("69a062d3-dbd1-96d9-8dd7-d3f7d324a6ec"), null, true, "Magnesium", "health-wellness/vitamins/by-type/magnesium", "magnesium", 378, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("69c968d3-7d18-8fd8-c12d-355ea4d9243b"), null, true, "Scrubs", "personal-care/bath-body/spa/scrubs", "scrubs", 357, new Guid("f1ea79d2-5a1b-9032-e4da-39a839c285fa") },
                    { new Guid("6a2145d3-4337-638e-eba1-a333b13ad71a"), null, true, "Wipes", "personal-care/bath-body/feminine/wipes", "wipes", 361, new Guid("d81fdbd2-0284-97c3-41ca-677a4e85a95d") },
                    { new Guid("6a43a2d3-25fc-368d-bf13-8246efaa5589"), null, true, "Peeled", "food-beverage/seafood/prawns-shrimp/peeled", "peeled", 36, new Guid("a16371d2-6324-444c-cebf-da38e1307dbb") },
                    { new Guid("6a5f0dd3-d5b2-a198-7288-0d46061fb1f9"), null, true, "Glucose monitors", "health-wellness/healthcare/medical-devices/glucose-monitors", "glucose-monitors", 409, new Guid("a63cddd2-fee7-1d9f-eacc-a5d6a951ca38") },
                    { new Guid("6afe3ad3-5f4d-d29e-9df7-f04b872dc07d"), null, true, "Jasmine", "food-beverage/rice-grains-staples/rice/jasmine", "jasmine", 90, new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926") },
                    { new Guid("6b77bad3-6ee5-38c0-905b-e0df14577a66"), null, true, "Cosmetic formulations", "b2b/oem/by-product/cosmetic-formulations", "cosmetic-formulations", 655, new Guid("543c5cd2-7728-3b67-2497-8c8e79b1ff89") },
                    { new Guid("6b7f82d3-ae1e-f5ce-8b89-2f399ed89168"), null, true, "Tops", "modest-fashion/womens-clothing/modest-everyday/tops", "tops", 481, new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6") },
                    { new Guid("6bc88ad3-62b0-c649-57d0-81f75a18b93a"), null, true, "Powdered", "food-beverage/dairy-eggs/milk/powdered", "powdered", 55, new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff") },
                    { new Guid("6d0f4fd3-fe30-d510-f419-4434c9762492"), null, true, "Food containers", "household/kitchen/storage/containers", "containers", 454, new Guid("1d8b41d2-f41e-6176-dc88-350139fe2a74") },
                    { new Guid("6d3047d3-257a-57ef-9138-ad1a0c8a6ce7"), null, true, "Mussels", "food-beverage/seafood/shellfish/mussels", "mussels", 46, new Guid("97a1a7d2-a92e-9df3-383d-53872a3dd6a6") },
                    { new Guid("6d35a7d3-ce3f-8bcc-60ec-694752f33bae"), null, true, "Pumps", "baby-mother/maternity/nursing/pumps", "pumps", 591, new Guid("69bea1d2-68d0-38f0-464f-17bbc431cef0") },
                    { new Guid("6e2872d3-971e-8c62-1f18-5b9554cee324"), null, true, "Steel-cut", "food-beverage/rice-grains-staples/oats-grains/steel-cut", "steel-cut", 100, new Guid("1d30e6d2-c067-2177-403e-f82269279ff4") },
                    { new Guid("6f5b17d3-ea3f-5f0f-8078-7c488377cae3"), null, true, "Snacks", "food-beverage/baby-food/baby-food/snacks", "snacks", 297, new Guid("d4d96bd2-6008-bd2c-4d7e-2066a922817b") },
                    { new Guid("6fd2cad3-0b8d-0ca3-da90-e9d5186df0c5"), null, true, "Conditioner", "personal-care/haircare/shampoo-conditioner/conditioner", "conditioner", 321, new Guid("401953d2-1437-5628-d42c-e4c6f4a1a243") },
                    { new Guid("6fdf59d3-6f3e-38c9-8d0f-5c66f6b9b57f"), null, true, "Story books", "islamic-religious/islamic-books/children-islamic/story-books", "story-books", 552, new Guid("99bab0d2-8e11-eafa-8f08-3c719acdca01") },
                    { new Guid("6ff682d3-d31e-72f6-d83c-19ec9f266f2f"), null, true, "Botanical extracts", "b2b/cosmetic-ingredients/extracts/botanical", "botanical", 635, new Guid("c278d2d2-6301-8192-242e-145233b94bfa") },
                    { new Guid("70374cd3-021a-fa56-1585-f2973681c21e"), null, true, "Kufi", "modest-fashion/kids-clothing/kids-prayer/kufi", "kufi", 509, new Guid("f3c106d2-bdc8-4e80-9e7a-1baf621d9d0e") },
                    { new Guid("708e56d3-e447-3450-be41-019fec472fe8"), null, true, "Flatbread", "food-beverage/rice-grains-staples/bread-wraps/flatbread", "flatbread", 115, new Guid("99dddfd2-9738-7b86-9f78-b555b77d130c") },
                    { new Guid("70daf8d3-e214-5a99-563c-d2ad78fe339d"), null, true, "Wood", "islamic-religious/islamic-gifts/wall-art/wood", "wood", 560, new Guid("a90d41d2-3ae5-4939-f0d5-4cff704ba5ce") },
                    { new Guid("71d3e9d3-c10d-e6a3-47e7-0909b3c9a6a7"), null, true, "Women", "modest-fashion/footwear/by-audience/women", "women", 528, new Guid("39b160d2-54bf-974b-9bda-82ac72849672") },
                    { new Guid("71e62bd3-f7e0-9779-1e8a-4bf85689578a"), null, true, "Alkaline", "food-beverage/beverages/water/alkaline", "alkaline", 264, new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c") },
                    { new Guid("724df2d3-aec5-bfce-a017-9053300d4354"), null, true, "Watches", "modest-fashion/fashion-accessories/belts/watches", "watches", 522, new Guid("66c580d2-4291-9f23-abdb-048c5fb2a2ed") },
                    { new Guid("725c96d3-c7ba-b8fc-696e-26c5a682dff3"), null, true, "Juice", "baby-mother/baby-food-d7/baby-drinks/juice", "juice", 575, new Guid("644fd3d2-6041-7f69-0c15-17f0f0c38b2b") },
                    { new Guid("737230d3-9bbb-3caa-7186-3c629299975d"), null, true, "Skincare", "baby-mother/maternity/postpartum/skincare", "skincare", 595, new Guid("0e5eced2-ca03-32b6-7af0-33c79642edfb") },
                    { new Guid("7382acd3-8d77-e0a2-07f5-757532bccdd4"), null, true, "Instant", "food-beverage/beverages/tea/instant", "instant", 281, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("739c21d3-2de0-e698-84d9-e8e3e685a229"), null, true, "Whey", "health-wellness/specialty-nutrition/sports/whey", "whey", 411, new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb") },
                    { new Guid("743109d3-1a8a-c7bb-ba03-338548fec51b"), null, true, "Canvas", "islamic-religious/islamic-gifts/wall-art/canvas", "canvas", 558, new Guid("a90d41d2-3ae5-4939-f0d5-4cff704ba5ce") },
                    { new Guid("7521bbd3-adfe-96ae-2e20-8f88b85ea445"), null, true, "Shampoo", "personal-care/haircare/shampoo-conditioner/shampoo", "shampoo", 320, new Guid("401953d2-1437-5628-d42c-e4c6f4a1a243") },
                    { new Guid("756346d3-7021-090a-3f15-90f35c7210f1"), null, true, "Pre-workout", "health-wellness/specialty-nutrition/sports/pre-workout", "pre-workout", 415, new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb") },
                    { new Guid("75bea3d3-d50b-8f44-bf7f-70139aed82fb"), null, true, "Puree pouches", "baby-mother/baby-food-d7/cereals-meals/puree-pouches", "puree-pouches", 572, new Guid("7df5f9d2-7a91-dfde-cdbd-608238ba566c") },
                    { new Guid("75cae7d3-3572-d061-3c36-385c542b7b97"), null, true, "Frozen", "food-beverage/seafood/fish/frozen", "frozen", 33, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("7632f1d3-4fb9-5a0f-fd5e-b3444ee8b860"), null, true, "All-purpose", "household/cleaning/surface/all-purpose", "all-purpose", 421, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("763a96d3-f0e0-f39b-603e-2fce41ef701a"), null, true, "Deglet Noor", "food-beverage/fresh-produce/dates-dried/deglet-noor", "deglet-noor", 144, new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142") },
                    { new Guid("76c608d3-5367-4b87-07a9-462c7cc02fae"), null, true, "Whipped", "food-beverage/dairy-eggs/cream/w whipped", "w whipped", 73, new Guid("19b8c5d2-14a6-7f91-f997-7fc8edd229cb") },
                    { new Guid("77758bd3-ee07-33d8-0514-150fc81278f1"), null, true, "Whole", "food-beverage/frozen-food/frozen-poultry/whole", "whole", 154, new Guid("983f12d2-694d-8266-346d-a947ef94cbeb") },
                    { new Guid("779770d3-57e3-eb33-3caa-3b1d6d4cb2d1"), null, true, "Woks", "household/kitchen/cookware/woks", "woks", 443, new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652") },
                    { new Guid("77f32ad3-3a3c-2531-47ee-8da961d537db"), null, true, "Berries", "food-beverage/fresh-produce/fresh-fruits/berries", "berries", 120, new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99") },
                    { new Guid("780b0bd3-c9a8-6c80-8e09-5bfc7bc208a4"), null, true, "Frozen", "food-beverage/seafood/crab/frozen", "frozen", 40, new Guid("400cf4d2-820e-70fa-0802-540d54203b6e") },
                    { new Guid("780f89d3-fa87-e716-73b7-e74366bb25c2"), null, true, "Kurta", "modest-fashion/mens-clothing/kurta-baju/kurta", "kurta", 491, new Guid("83e7a0d2-f880-4239-6489-b22aff95394c") },
                    { new Guid("786e54d3-02a3-6a31-bf09-4345220f35bc"), null, true, "Khimar", "modest-fashion/womens-clothing/hijabs/khimar", "khimar", 477, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("78aa51d3-0c39-fd4c-8648-b28fb514f8be"), null, true, "Basmati & brown blends", "food-beverage/rice-grains-staples/rice/blends", "blends", 93, new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926") },
                    { new Guid("78be49d3-6713-8acb-296e-864d0ed150e2"), null, true, "Canola oil", "food-beverage/oils-fats/cooking-oils/canola-oil", "canola-oil", 251, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("78d851d3-55e5-25c8-1578-2699461f09eb"), null, true, "Frozen bulk meat", "b2b/halal-ingredients/meat-ingredients/frozen-bulk", "frozen-bulk", 606, new Guid("1643ced2-bd0c-1ddd-e61b-57c9b963115e") },
                    { new Guid("78e1cfd3-ff56-42fa-7b7a-43ed16032689"), null, true, "Modest swimwear", "modest-fashion/womens-clothing/modest-everyday/modest-swimwear", "modest-swimwear", 485, new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6") },
                    { new Guid("798495d3-e41a-fc5e-4486-df8064e49c2c"), null, true, "Cake", "food-beverage/frozen-food/frozen-desserts/cake", "cake", 173, new Guid("195badd2-1777-8b8b-076e-578874a8f7d4") },
                    { new Guid("7a3c29d3-e5ff-6fc9-f522-79bd7d2591d0"), null, true, "Whole fish", "food-beverage/seafood/fish/whole-fish", "whole-fish", 28, new Guid("fce9d2d2-92d3-9775-401f-a76309fae629") },
                    { new Guid("7b0e04d3-5d24-7a3e-76fd-df0b282936d3"), null, true, "Custom branding", "b2b/oem/by-service/custom-branding", "custom-branding", 657, new Guid("980928d2-75e3-08f0-1ccd-a25d3962cc24") },
                    { new Guid("7bd69ad3-db49-72ee-3ae4-0be8d0c0f601"), null, true, "Coconut water", "food-beverage/beverages/water/coconut-water", "coconut-water", 265, new Guid("12f423d2-e787-f93e-4836-0d3f77afa33c") },
                    { new Guid("7c975bd3-9c4c-a9dc-b00b-4a31728fd0df"), null, true, "Herbal", "food-beverage/beverages/tea/herbal", "herbal", 277, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("7cb340d3-96bf-0283-87a5-2fd2d68927d4"), null, true, "Cold-pressed", "food-beverage/beverages/juice/cold-pressed", "cold-pressed", 270, new Guid("5592e0d2-546b-c966-3827-db3843f75cc0") },
                    { new Guid("7cd914d3-58ee-7139-322b-2608ce1aa80e"), null, true, "Bags", "food-beverage/beverages/tea/bags", "bags", 280, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("7cff7ad3-abc9-974d-a76a-eda3dbdb50c3"), null, true, "Buffalo ghee", "food-beverage/oils-fats/ghee/buffalo-ghee", "buffalo-ghee", 257, new Guid("248af1d2-cb0e-0f0a-8f82-84c11c6caecf") },
                    { new Guid("7d6addd3-94a8-786d-d142-c65d1b853249"), null, true, "Pads", "baby-mother/maternity/nursing/pads", "pads", 592, new Guid("69bea1d2-68d0-38f0-464f-17bbc431cef0") },
                    { new Guid("7d6d9cd3-bc8a-af38-7521-34359a741d4e"), null, true, "Pastes & spreads", "food-beverage/seafood/canned-preserved/pastes-spreads", "pastes-spreads", 52, new Guid("b3ecf0d2-691f-ff7d-507a-e934cf8e93dc") },
                    { new Guid("7d9cb3d3-c423-e3e3-a6e2-4869c94ec3e0"), null, true, "Sourdough", "food-beverage/bakery/bread/sourdough", "sourdough", 220, new Guid("65266dd2-4d53-0eeb-74d2-83aff55468b3") },
                    { new Guid("7e8247d3-497d-9b33-c9bf-801e9e5cab6d"), null, true, "Cooked", "food-beverage/seafood/prawns-shrimp/cooked", "cooked", 37, new Guid("a16371d2-6324-444c-cebf-da38e1307dbb") },
                    { new Guid("7ed191d3-45ae-e9d9-4ef5-1879d383f6fa"), null, true, "Powder", "personal-care/cosmetics-makeup/face/powder", "powder", 331, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("7edf14d3-cbda-2972-35e8-b3f48fb83734"), null, true, "Pocket size", "islamic-religious/quran/qurans/pocket", "pocket", 534, new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf") },
                    { new Guid("7f1a7ad3-3488-f289-cb1e-d2eb94f3b3d5"), null, true, "Filled", "food-beverage/snacks-confectionery/biscuits/filled", "filled", 196, new Guid("46a81bd2-a65c-b762-cb53-60d7efb6a019") },
                    { new Guid("7f8c57d3-607d-5f68-aaf4-bc8d0d77bb8f"), null, true, "Hadith", "islamic-religious/islamic-books/hadith-fiqh/hadith", "hadith", 549, new Guid("779198d2-09c8-8c6b-ee20-dbc5f841c99c") },
                    { new Guid("806d3ed3-497b-0d8f-8f3e-04fd6b4857fb"), null, true, "Plant protein", "health-wellness/specialty-nutrition/sports/plant-protein", "plant-protein", 412, new Guid("5484d6d2-b1cb-3b87-10cf-1c11cc6513bb") },
                    { new Guid("80baacd3-69b3-334d-53fb-df3b83460ff9"), null, true, "Fresh cuts", "food-beverage/meat-poultry/lamb-mutton/fresh-cuts", "fresh-cuts", 6, new Guid("49b9e3d2-e1f2-e025-e046-dec8531eb5b6") },
                    { new Guid("80ea0bd3-98a1-fa63-7b42-59e5059ff352"), null, true, "Soy sauce", "food-beverage/sauces-spices/sauces/soy-sauce", "soy-sauce", 233, new Guid("9886cfd2-b818-41e1-31c0-67d9fb0ed817") },
                    { new Guid("81d098d3-70df-94af-60cd-1613a3a65cfe"), null, true, "Snacks", "baby-mother/baby-food-d7/cereals-meals/snacks", "snacks", 573, new Guid("7df5f9d2-7a91-dfde-cdbd-608238ba566c") },
                    { new Guid("82ce69d3-4402-0200-c385-bff210d9d220"), null, true, "Deli meat", "food-beverage/meat-poultry/processed-meat/deli-meat", "deli-meat", 25, new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6") },
                    { new Guid("8309dad3-bcc5-ef1c-38d7-0b2403cfa7d6"), null, true, "Malaysian", "modest-fashion/mens-clothing/thobes/malaysian", "malaysian", 488, new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f") },
                    { new Guid("831dbbd3-9b3f-d94f-5007-870bb944d03f"), null, true, "Lipstick", "personal-care/cosmetics-makeup/lips/lipstick", "lipstick", 338, new Guid("e47115d2-f163-dc61-232d-96825d1f1f7f") },
                    { new Guid("845f95d3-c2e0-dde3-b584-058732aef0fe"), null, true, "Kufi", "modest-fashion/kids-clothing/boys/kufi", "kufi", 505, new Guid("436179d2-bcdc-5c7d-1b3b-dcee8c42358b") },
                    { new Guid("84c590d3-7a1b-5a95-6270-0da6b9f936b8"), null, true, "Quail", "food-beverage/dairy-eggs/eggs/quail", "quail", 84, new Guid("034140d2-ae78-36e1-1f4a-1baecde863f8") },
                    { new Guid("855d3fd3-2b47-c182-9ce2-b26a26f74e0c"), null, true, "Embroidered", "modest-fashion/womens-clothing/abayas/embroidered", "embroidered", 467, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("85c0a0d3-9078-6007-5328-cef1750cc10a"), null, true, "Toddler milk", "baby-mother/baby-food-d7/formula-milk/toddler-milk", "toddler-milk", 568, new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd") },
                    { new Guid("86009ad3-4078-63dd-52ff-613daf35f3cb"), null, true, "Water", "baby-mother/baby-food-d7/baby-drinks/water", "water", 574, new Guid("644fd3d2-6041-7f69-0c15-17f0f0c38b2b") },
                    { new Guid("86275ad3-3a0f-525a-a3bf-c9c14a93ff03"), null, true, "Arabic", "food-beverage/beverages/coffee/arabic", "arabic", 286, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("866456d3-8795-7fd5-8ec9-43a220efef9d"), null, true, "Sarong", "modest-fashion/mens-clothing/prayer-wear-men/sarong", "sarong", 496, new Guid("669517d2-a3ae-d7e1-f93f-536676cb582e") },
                    { new Guid("869228d3-93b8-d680-6018-5ade194282d2"), null, true, "Roll-on", "personal-care/fragrance/perfume/roll-on", "roll-on", 347, new Guid("b779ecd2-8416-1ca5-589c-54e76e66565a") },
                    { new Guid("86a213d3-40f5-23dd-d4fe-7e94bfd65059"), null, true, "Packaging", "b2b/horeca/catering/packaging", "packaging", 649, new Guid("6d5f2dd2-bc68-2e75-a3fe-adac70a08b37") },
                    { new Guid("86c453d3-5855-bd89-2379-78948f920692"), null, true, "Tafsir", "islamic-religious/quran/qurans/tafsir", "tafsir", 533, new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf") },
                    { new Guid("86e89cd3-f576-38b4-1842-3345d6d96520"), null, true, "Stew", "food-beverage/ready-to-eat/canned-meals/stew", "stew", 183, new Guid("b2ea6bd2-3ee3-0222-1343-8abc7030d643") },
                    { new Guid("86ed4ad3-bf28-52a8-4823-13d10e44a670"), null, true, "Toothbrushes", "personal-care/oral-care/tools/toothbrushes", "toothbrushes", 367, new Guid("7a4c51d2-3a04-a071-54c2-682511d05fb6") },
                    { new Guid("870b60d3-e0e3-bc94-ca37-40902847da33"), null, true, "Duck", "food-beverage/dairy-eggs/eggs/duck", "duck", 83, new Guid("034140d2-ae78-36e1-1f4a-1baecde863f8") },
                    { new Guid("872ec7d3-dc99-e184-db3a-e090c0221880"), null, true, "Oysters", "food-beverage/seafood/shellfish/oysters", "oysters", 47, new Guid("97a1a7d2-a92e-9df3-383d-53872a3dd6a6") },
                    { new Guid("87724cd3-146f-9b30-f626-b7398384277e"), null, true, "Silk", "modest-fashion/womens-clothing/hijabs/silk", "silk", 473, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("87820bd3-512a-e37d-1ce7-f72f3afce43f"), null, true, "Citrus", "food-beverage/fresh-produce/fresh-fruits/citrus", "citrus", 121, new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99") },
                    { new Guid("87896fd3-9345-6a3b-59ff-49805428f747"), null, true, "Topi", "islamic-religious/prayer/prayer-accessories/topi", "topi", 548, new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5") },
                    { new Guid("88e08ed3-b2d0-1fa0-ee20-87ebabfae7e5"), null, true, "Stabilizers", "b2b/halal-ingredients/functional-ingredients/stabilizers", "stabilizers", 613, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("8946a6d3-2ad1-b9df-b36a-db824536fb32"), null, true, "Kueh", "food-beverage/bakery/traditional-desserts/kueh", "kueh", 229, new Guid("619bf8d2-13b6-8c7b-f4be-1292530d6762") },
                    { new Guid("89c89bd3-e8de-0d7a-0c91-fe52f882755f"), null, true, "Skirts", "modest-fashion/womens-clothing/modest-everyday/skirts", "skirts", 482, new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6") },
                    { new Guid("89e6e2d3-5fc1-6050-3eab-8608b383c65a"), null, true, "Infant formula", "baby-mother/baby-food-d7/formula-milk/infant-formula", "infant-formula", 566, new Guid("fb16f0d2-680c-1ccc-6a69-41f5e8efbbcd") },
                    { new Guid("8a4545d3-c82c-6a19-da48-d9c844b15b97"), null, true, "Pastries", "food-beverage/bakery/cakes/pastries", "pastries", 224, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("8a4fb7d3-4bed-90c2-dbb2-dc25b9388051"), null, true, "Gelatin alternatives", "b2b/halal-ingredients/functional-ingredients/gelatin-alt", "gelatin-alt", 609, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("8af936d3-b735-a336-881e-e91d95d4c1a9"), null, true, "Baju Melayu", "modest-fashion/mens-clothing/kurta-baju/baju-melayu", "baju-melayu", 492, new Guid("83e7a0d2-f880-4239-6489-b22aff95394c") },
                    { new Guid("8b290dd3-6a08-678e-e60a-9abbef76704f"), null, true, "Samosa", "food-beverage/frozen-food/frozen-snacks/samosa", "samosa", 169, new Guid("931af1d2-1327-3650-c921-7013f1061837") },
                    { new Guid("8b84d8d3-d0ed-6c36-4eda-8de4b92d3c34"), null, true, "Banana", "food-beverage/snacks-confectionery/chips/banana", "banana", 192, new Guid("328929d2-a919-8811-3c80-20c5fd6981ff") },
                    { new Guid("8be903d3-45ed-2353-5ee3-03dbf23d545d"), null, true, "B-complex", "health-wellness/vitamins/by-type/b-complex", "b-complex", 373, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("8c1484d3-8b00-dd30-26ac-1e0d346cf6f4"), null, true, "Closed", "modest-fashion/womens-clothing/abayas/closed", "closed", 465, new Guid("f286fdd2-07d6-e504-b28c-ccec798baee8") },
                    { new Guid("8c179bd3-543a-5ea0-b4f4-990e59d8f299"), null, true, "Baju Koko", "modest-fashion/mens-clothing/kurta-baju/baju-koko", "baju-koko", 493, new Guid("83e7a0d2-f880-4239-6489-b22aff95394c") },
                    { new Guid("8c3173d3-1c93-793a-b2d1-7ace83a2513f"), null, true, "Travel", "islamic-religious/prayer/prayer-mats/travel", "travel", 542, new Guid("bbb126d2-7508-6bb6-9881-5bc2d6be57d6") },
                    { new Guid("8c3e0fd3-c4f0-1c95-46be-55618ac06282"), null, true, "Fillets", "food-beverage/frozen-food/frozen-seafood/fillets", "fillets", 156, new Guid("e77cafd2-ec5f-39af-b161-5c8527add63f") },
                    { new Guid("8c7789d3-4a23-bede-88ac-049042b9cad7"), null, true, "Meat powder", "b2b/halal-ingredients/meat-ingredients/meat-powder", "meat-powder", 608, new Guid("1643ced2-bd0c-1ddd-e61b-57c9b963115e") },
                    { new Guid("8d3f65d3-76b4-336a-3a08-34f0b79ca101"), null, true, "Highlighter", "personal-care/cosmetics-makeup/face/highlighter", "highlighter", 334, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("8e755ed3-bb5e-50de-a329-c36a396e1a3f"), null, true, "Embroidered", "modest-fashion/mens-clothing/thobes/embroidered", "embroidered", 490, new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f") },
                    { new Guid("8f63cbd3-fc2b-a3fb-3857-7445fcc8b1a5"), null, true, "Minced", "food-beverage/meat-poultry/beef/minced", "minced", 2, new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda") },
                    { new Guid("8f6933d3-26ed-0e94-d64f-3ecdc7d7d01f"), null, true, "Men", "modest-fashion/footwear/by-audience/men", "men", 529, new Guid("39b160d2-54bf-974b-9bda-82ac72849672") },
                    { new Guid("902ef4d3-60e3-3829-2413-f83b1a33ef9f"), null, true, "Whole", "food-beverage/meat-poultry/duck/whole", "whole", 21, new Guid("fe9fc9d2-3128-c77c-2a28-e6e17a0585b8") },
                    { new Guid("90c5abd3-cf47-3067-0751-f7b5a6d0ede4"), null, true, "Loose-leaf", "food-beverage/beverages/tea/loose-leaf", "loose-leaf", 279, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("90c7cad3-11be-8f6d-132f-9d3389e6ebfa"), null, true, "Spinach", "food-beverage/frozen-food/frozen-veg/spinach", "spinach", 161, new Guid("2e6b16d2-a533-72a3-5ecb-d4258815635a") },
                    { new Guid("916501d3-6195-fb4e-03b1-cc3fea467ab7"), null, true, "Lip care", "personal-care/skincare/facial/lip-care", "lip-care", 313, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("918e63d3-b034-50fa-dbd4-245e93778489"), null, true, "Stage 1", "food-beverage/baby-food/infant-formula/stage-1", "stage-1", 298, new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161") },
                    { new Guid("925491d3-24fc-e7d3-a77a-6d7cd1f53024"), null, true, "Almond", "food-beverage/dairy-eggs/milk-alt/almond", "almond", 79, new Guid("e67ff7d2-9d3d-e8b9-e8b9-388ad6a76031") },
                    { new Guid("932cdfd3-e47d-1383-7d95-05481f762f00"), null, true, "Shea butter", "b2b/cosmetic-ingredients/oils-butters/shea-butter", "shea-butter", 630, new Guid("097a78d2-4e6b-9f77-9178-022420d06d74") },
                    { new Guid("937e1ed3-733f-aeaa-0ba6-49fede91993f"), null, true, "Ashwagandha", "health-wellness/herbal-traditional/herbal-supps/ashwagandha", "ashwagandha", 389, new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f") },
                    { new Guid("93e74cd3-bd3e-a204-bedc-38415250e725"), null, true, "Salad mixes", "food-beverage/fresh-produce/fresh-cut/salad-mixes", "salad-mixes", 147, new Guid("165b72d2-7a36-a783-4101-1a1dd081e0f6") },
                    { new Guid("93f2afd3-ab05-d85c-bd36-57504109e6d3"), null, true, "Ajwa", "food-beverage/fresh-produce/dates-dried/ajwa", "ajwa", 143, new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142") },
                    { new Guid("9454f3d3-6007-fd9a-c273-c6b082d19aee"), null, true, "Coriander", "food-beverage/fresh-produce/fresh-herbs/coriander", "coriander", 133, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("9455e5d3-e1fb-f16f-236a-c70f51ce6388"), null, true, "Mixed", "food-beverage/frozen-food/frozen-meat/mixed", "mixed", 153, new Guid("15119fd2-bb73-0f1c-212d-ef653e83fa23") },
                    { new Guid("94ce79d3-1bd8-5501-7a97-40df9f376be2"), null, true, "Spring roll", "food-beverage/frozen-food/frozen-snacks/spring-roll", "spring-roll", 170, new Guid("931af1d2-1327-3650-c921-7013f1061837") },
                    { new Guid("955e14d3-35df-12c4-2ea7-d8c4b458ae64"), null, true, "Cutting boards", "household/kitchen/utensils/cutting-boards", "cutting-boards", 452, new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380") },
                    { new Guid("962e18d3-7451-e695-6de7-46930a35f15b"), null, true, "Energy", "food-beverage/snacks-confectionery/snack-bars/energy", "energy", 215, new Guid("5c9b1dd2-9b94-dcc8-73c1-42d054df30d5") },
                    { new Guid("9792a1d3-5ece-2410-bfa6-9e66b668d308"), null, true, "Peppers & chillies", "food-beverage/fresh-produce/fresh-vegetables/peppers-chillies", "peppers-chillies", 128, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("980620d3-a2f6-9b4d-ca0d-5c112e830153"), null, true, "Single", "food-beverage/dairy-eggs/cream/single", "single", 70, new Guid("19b8c5d2-14a6-7f91-f997-7fc8edd229cb") },
                    { new Guid("98255ed3-1e19-4360-bfab-f3ff6c5aab13"), null, true, "Water bottles", "household/kitchen/storage/water-bottles", "water-bottles", 455, new Guid("1d8b41d2-f41e-6176-dc88-350139fe2a74") },
                    { new Guid("9861c4d3-d870-0b30-c5f4-6437cb10b070"), null, true, "Pectin", "b2b/halal-ingredients/functional-ingredients/pectin", "pectin", 610, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("9892ced3-9796-0585-e709-5588148b3fc6"), null, true, "Sausages", "food-beverage/meat-poultry/processed-meat/sausages", "sausages", 23, new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6") },
                    { new Guid("98c6eed3-fee9-08d1-5e41-750019333968"), null, true, "Spatulas", "household/kitchen/utensils/spatulas", "spatulas", 449, new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380") },
                    { new Guid("991d13d3-9abf-c1fe-3288-a849422ec71e"), null, true, "Chai", "food-beverage/beverages/tea/chai", "chai", 278, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("994618d3-8067-ad95-47e1-bb49fcc3215d"), null, true, "Butter ghee blend", "food-beverage/oils-fats/shortening/butter-ghee-blend", "butter-ghee-blend", 260, new Guid("46d91bd2-cec3-9851-1cd1-f89f54bb31d9") },
                    { new Guid("99c1fad3-594c-d906-eb0a-ab13b30cb253"), null, true, "Croissants", "food-beverage/bakery/cakes/croissants", "croissants", 225, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("9a653fd3-0d80-ebde-d49f-a8e2843e3f30"), null, true, "Ornaments", "islamic-religious/islamic-gifts/ornaments/ornaments", "ornaments", 561, new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5") },
                    { new Guid("9a91d5d3-38f7-3581-3151-296399992bdb"), null, true, "Prayer-friendly", "modest-fashion/footwear/by-type/prayer-friendly", "prayer-friendly", 527, new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e") },
                    { new Guid("9aa481d3-c0c5-877b-7e74-100f4d9a7349"), null, true, "Puree", "food-beverage/baby-food/baby-food/puree", "puree", 295, new Guid("d4d96bd2-6008-bd2c-4d7e-2066a922817b") },
                    { new Guid("9b334ad3-9bb0-d416-2f75-6a98c8c2cd78"), null, true, "Perfume oils", "personal-care/fragrance/oud-attar/perfume-oils", "perfume-oils", 352, new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6") },
                    { new Guid("9b7954d3-9a5e-e2a2-66b7-4dbfd6a4988c"), null, true, "Meat extract", "b2b/halal-ingredients/meat-ingredients/meat-extract", "meat-extract", 607, new Guid("1643ced2-bd0c-1ddd-e61b-57c9b963115e") },
                    { new Guid("9b96a2d3-3d7f-080a-5ab5-b5cdc08337f5"), null, true, "Natural flavours", "b2b/halal-ingredients/flavour/natural-flavours", "natural-flavours", 616, new Guid("7b3aa8d2-4cdc-c22a-f308-77485f6ff511") },
                    { new Guid("9bd06cd3-407a-f813-b1c7-13d882763d60"), null, true, "Thickeners", "b2b/halal-ingredients/functional-ingredients/thickeners", "thickeners", 614, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("9bfef8d3-3a65-8f12-2251-e2ede8721055"), null, true, "Hajj & Umrah gifts", "islamic-religious/islamic-gifts/ornaments/hajj-umrah-gifts", "hajj-umrah-gifts", 565, new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5") },
                    { new Guid("9ce047d3-3c79-f30a-a384-95ac8a63ecf7"), null, true, "Mabkharat", "household/home-fragrance/traditional-fragrance/mabkharat", "mabkharat", 440, new Guid("927f1bd2-f1c8-b1ea-002a-d7eb533544f8") },
                    { new Guid("9d0257d3-8e22-25e3-44be-9145656e8748"), null, true, "Foundation", "personal-care/cosmetics-makeup/face/foundation", "foundation", 329, new Guid("03b95ed2-3b60-3be6-b92e-f3ef10c56891") },
                    { new Guid("9e02f1d3-4f4c-16bd-cc5a-b6419dc1b779"), null, true, "Oud", "personal-care/fragrance/oud-attar/oud", "oud", 348, new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6") },
                    { new Guid("9e0816d3-d975-0bfd-bfd7-0e4e4a44ef25"), null, true, "Soups", "food-beverage/ready-to-eat/sandwiches-salads/soups", "soups", 190, new Guid("6dc32ad2-10b9-7c94-7de5-f45feb94e073") },
                    { new Guid("9f2f0cd3-9b03-7c1c-ce7b-184ad168dafe"), null, true, "Thobes", "modest-fashion/kids-clothing/boys/thobes", "thobes", 503, new Guid("436179d2-bcdc-5c7d-1b3b-dcee8c42358b") },
                    { new Guid("a01b0bd3-d159-73eb-7617-0b836779f2e1"), null, true, "Vitamin C", "health-wellness/vitamins/by-type/vitamin-c", "vitamin-c", 371, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("a02858d3-70f3-d6de-755c-d401bb61e206"), null, true, "Antiseptics", "health-wellness/healthcare/first-aid/antiseptics", "antiseptics", 404, new Guid("49e85fd2-dc0c-2e5c-6284-5669988a659f") },
                    { new Guid("a07140d3-bcdc-49dc-a332-93338870269a"), null, true, "Belts", "modest-fashion/fashion-accessories/belts/belts", "belts", 520, new Guid("66c580d2-4291-9f23-abdb-048c5fb2a2ed") },
                    { new Guid("a0b38ad3-31e5-a3ea-3fe7-121add194989"), null, true, "Oil", "baby-mother/baby-care/bath-skin/oil", "oil", 579, new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd") },
                    { new Guid("a10893d3-c195-e1a6-d843-695e305d6f58"), null, true, "Pods & beans", "food-beverage/fresh-produce/fresh-vegetables/pods-beans", "pods-beans", 131, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("a11853d3-9129-a6cf-7d7f-37b01de1efa0"), null, true, "Cake tins", "household/kitchen/bakeware/cake-tins", "cake-tins", 447, new Guid("148f29d2-a1da-5ee6-c4cc-e7af1ea771a9") },
                    { new Guid("a12c7bd3-06e2-87bd-01bb-caa5e5a95bd1"), null, true, "Manuka honey", "health-wellness/herbal-traditional/traditional/manuka-honey", "manuka-honey", 392, new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429") },
                    { new Guid("a13ac3d3-1897-524d-77a9-c4eb5afa6982"), null, true, "Frozen", "food-beverage/seafood/prawns-shrimp/frozen", "frozen", 35, new Guid("a16371d2-6324-444c-cebf-da38e1307dbb") },
                    { new Guid("a14bc0d3-c7d1-b282-1e99-4bf7151c2ff3"), null, true, "Thermos", "household/kitchen/storage/thermos", "thermos", 457, new Guid("1d8b41d2-f41e-6176-dc88-350139fe2a74") },
                    { new Guid("a16a84d3-db9a-b02a-a16e-208c1c4914a4"), null, true, "Flavoured", "food-beverage/dairy-eggs/yogurt/flavoured", "flavoured", 64, new Guid("fe0861d2-40c3-d2b1-0b67-1d20ee82a28f") },
                    { new Guid("a20405d3-2aca-d085-e4cd-471ab213dd0f"), null, true, "Minced", "food-beverage/meat-poultry/goat/minced", "minced", 12, new Guid("1bbd4dd2-ab32-53a3-04d0-e004a74e16f8") },
                    { new Guid("a22052d3-b1e7-a6fe-468d-efaa9605f1ba"), null, true, "Organic / free-range", "food-beverage/meat-poultry/chicken/organic-free-range", "organic-free-range", 17, new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4") },
                    { new Guid("a38ae2d3-aa9f-570f-a88a-63ca49e28a67"), null, true, "Modest trousers", "modest-fashion/mens-clothing/shirts-trousers/modest-trousers", "modest-trousers", 495, new Guid("2eecc8d2-1b9e-5b16-ad0f-75617186a9b0") },
                    { new Guid("a38f7dd3-6466-ab42-6282-48bd80f17547"), null, true, "Sandwiches", "food-beverage/ready-to-eat/sandwiches-salads/sandwiches", "sandwiches", 187, new Guid("6dc32ad2-10b9-7c94-7de5-f45feb94e073") },
                    { new Guid("a3e7a9d3-3ea7-572e-ca75-68f16fb38eca"), null, true, "Burgers & patties", "food-beverage/meat-poultry/processed-meat/burgers-patties", "burgers-patties", 24, new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6") },
                    { new Guid("a4686dd3-2917-ef01-f659-0e0daacc0109"), null, true, "Oud", "b2b/cosmetic-ingredients/fragrance-ingredients/oud", "oud", 640, new Guid("3cad13d2-6a60-bd9c-c433-3342228d351f") },
                    { new Guid("a46fe1d3-6751-fd64-6784-99914cb6cc7b"), null, true, "Stain remover", "household/cleaning/laundry/stain-remover", "stain-remover", 429, new Guid("6b4e2fd2-fb7e-d6b7-99e7-1348fa428a43") },
                    { new Guid("a4d3e8d3-90bf-7033-29dc-7169177cd182"), null, true, "Stone", "food-beverage/fresh-produce/fresh-fruits/stone", "stone", 123, new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99") },
                    { new Guid("a51394d3-ba3c-0e36-ac31-e0aacfd696fd"), null, true, "Pacifier clips", "baby-mother/baby-essentials/baby-accessories/pacifier-clips", "pacifier-clips", 603, new Guid("68bad0d2-8a96-04b9-1552-1cd110ab4a02") },
                    { new Guid("a515ebd3-e207-9888-d7bb-924374edce19"), null, true, "Lavash", "food-beverage/rice-grains-staples/bread-wraps/lavash", "lavash", 118, new Guid("99dddfd2-9738-7b86-9f78-b555b77d130c") },
                    { new Guid("a54315d3-29e7-7687-0a75-a68ce84a76c0"), null, true, "Bouillon cubes", "food-beverage/sauces-spices/stock/bouillon-cubes", "bouillon-cubes", 248, new Guid("3120ffd2-9ab4-a2ce-5185-1bb4c691f055") },
                    { new Guid("a5599ad3-6b4c-902e-824a-2f57963f5221"), null, true, "Supplement formulations", "b2b/oem/by-product/supplement-formulations", "supplement-formulations", 656, new Guid("543c5cd2-7728-3b67-2497-8c8e79b1ff89") },
                    { new Guid("a55cf0d3-394e-9b76-16b0-ed897a734fb1"), null, true, "Coconut oil", "b2b/cosmetic-ingredients/oils-butters/coconut-oil", "coconut-oil", 627, new Guid("097a78d2-4e6b-9f77-9178-022420d06d74") },
                    { new Guid("a572ead3-4cbe-7fa2-05d2-7f8c9faa0d42"), null, true, "Lotions", "personal-care/skincare/body-care/lotions", "lotions", 314, new Guid("1e9766d2-8e14-bc32-0e66-d58542d25d58") },
                    { new Guid("a5ffaad3-3462-a5c0-64ce-c06df303f589"), null, true, "Bakhoor", "personal-care/fragrance/oud-attar/bakhoor", "bakhoor", 350, new Guid("59ca7ed2-2b00-af52-5fee-3aa774d9b2a6") },
                    { new Guid("a60c32d3-e006-0f28-359a-89eaf030298e"), null, true, "Pies & tarts", "food-beverage/bakery/cakes/pies-tarts", "pies-tarts", 227, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("a7978dd3-651a-a191-d97b-94457a496be4"), null, true, "Preservatives", "b2b/halal-ingredients/additives/preservatives", "preservatives", 620, new Guid("f62090d2-8c0b-d9c7-2cdc-a63d3ec59e8c") },
                    { new Guid("a7bc03d3-d546-f8fa-053c-08fa398a6605"), null, true, "Bands", "modest-fashion/fashion-accessories/hijab-accessories/bands", "bands", 514, new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0") },
                    { new Guid("a9202ed3-6777-7e53-89b0-1e9715b7d433"), null, true, "Cut vegetables", "food-beverage/fresh-produce/fresh-cut/cut-vegetables", "cut-vegetables", 149, new Guid("165b72d2-7a36-a783-4101-1a1dd081e0f6") },
                    { new Guid("aa4c1ad3-e1f2-52ea-4281-1f84d8f0f78a"), null, true, "Aged", "food-beverage/dairy-eggs/cheese/aged", "aged", 59, new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5") },
                    { new Guid("ab0f41d3-8bf5-ccb1-f8e1-37eda771c13d"), null, true, "Eau de parfum", "personal-care/fragrance/perfume/edp", "edp", 344, new Guid("b779ecd2-8416-1ca5-589c-54e76e66565a") },
                    { new Guid("ab2bfad3-2fa3-3807-fe29-129bf87fa1c8"), null, true, "Removers", "personal-care/cosmetics-makeup/tools/removers", "removers", 341, new Guid("5e90d9d2-661a-aeb2-6ba2-3904d1106e2f") },
                    { new Guid("ab32a5d3-4d1b-a721-39f0-983740a8b013"), null, true, "Natural fragrances", "b2b/cosmetic-ingredients/fragrance-ingredients/natural-fragrances", "natural-fragrances", 641, new Guid("3cad13d2-6a60-bd9c-c433-3342228d351f") },
                    { new Guid("ab6a3ed3-2b88-174b-b528-3cd81ddb7b6a"), null, true, "Lollipops", "food-beverage/snacks-confectionery/candy/lollipops", "lollipops", 205, new Guid("4648f2d2-08d7-49f0-6a55-f68e9604a94b") },
                    { new Guid("ab9a19d3-12ca-7bf3-3911-5f37e384e826"), null, true, "Prayer mats", "household/home-essentials/textiles/prayer-mats", "prayer-mats", 461, new Guid("5e4f2ad2-aad9-95d2-dcda-da060568463d") },
                    { new Guid("ad7ac5d3-6c21-62a7-bc63-ac76fc8b858a"), null, true, "History", "islamic-religious/islamic-books/history/history", "history", 556, new Guid("733bdad2-957e-8900-8965-86f883c49c56") },
                    { new Guid("ae18a5d3-81b8-dd50-ff1a-d5da21652219"), null, true, "Vegan", "food-beverage/dairy-eggs/ice-cream/vegan", "vegan", 77, new Guid("2ea98cd2-0b7e-5ed3-6c9d-22c5096ae03a") },
                    { new Guid("ae71eed3-1c22-d0c8-645f-5d1e2ade10ee"), null, true, "Cotton", "modest-fashion/womens-clothing/hijabs/cotton", "cotton", 474, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("aecff9d3-62f2-3908-004e-13c28b72d88b"), null, true, "Juice", "food-beverage/baby-food/children-drinks/juice", "juice", 303, new Guid("4108d4d2-d111-f301-c2c6-231985a2b227") },
                    { new Guid("aeee8cd3-4872-cf23-442a-8550b0bc14eb"), null, true, "Nappies", "baby-mother/baby-care/diapers-wipes/nappies", "nappies", 584, new Guid("6a3304d2-ffb2-a73a-84e7-299d7270a3a0") },
                    { new Guid("aef02bd3-d3d0-d122-e782-4cbfdc2059b6"), null, true, "Lotion", "baby-mother/baby-care/bath-skin/lotion", "lotion", 578, new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd") },
                    { new Guid("aefe79d3-ef15-bba4-125f-c0d6fe5db096"), null, true, "Argan oil", "b2b/cosmetic-ingredients/oils-butters/argan-oil", "argan-oil", 628, new Guid("097a78d2-4e6b-9f77-9178-022420d06d74") },
                    { new Guid("b07034d3-dad5-5b4b-643e-a30ab6445416"), null, true, "Pressure cookers", "household/kitchen/cookware/pressure-cookers", "pressure-cookers", 444, new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652") },
                    { new Guid("b09edbd3-8a1b-6788-5527-9b7ae3be9d1e"), null, true, "Contract manufacturing", "b2b/oem/by-service/contract-manufacturing", "contract-manufacturing", 658, new Guid("980928d2-75e3-08f0-1ccd-a25d3962cc24") },
                    { new Guid("b0e1a5d3-095d-3774-f276-a40be77bb903"), null, true, "Colourants", "b2b/halal-ingredients/additives/colourants", "colourants", 621, new Guid("f62090d2-8c0b-d9c7-2cdc-a63d3ec59e8c") },
                    { new Guid("b1a223d3-e383-0e1f-8084-5ceedf0cd431"), null, true, "Vanilla", "b2b/halal-ingredients/flavour/vanilla", "vanilla", 618, new Guid("7b3aa8d2-4cdc-c22a-f308-77485f6ff511") },
                    { new Guid("b2348ad3-40c5-bedb-d5b6-518ebc1aa638"), null, true, "Almonds", "food-beverage/snacks-confectionery/nuts-seeds/almonds", "almonds", 207, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("b2dfeed3-2adf-51ae-5f85-06f217f14da4"), null, true, "Body oils", "personal-care/bath-body/spa/body-oils", "body-oils", 359, new Guid("f1ea79d2-5a1b-9032-e4da-39a839c285fa") },
                    { new Guid("b3803ed3-de4d-f1d6-108e-5e1bdbc98dc6"), null, true, "Shower gel", "personal-care/bath-body/cleansing/shower-gel", "shower-gel", 356, new Guid("757aeed2-5da0-eb6a-e049-17f920363d1c") },
                    { new Guid("b3839dd3-eaec-3297-7413-b315648b32c5"), null, true, "Frozen foods", "b2b/horeca/restaurant/frozen-foods", "frozen-foods", 645, new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f") },
                    { new Guid("b3f59fd3-af23-ad69-962b-ff7a2777e7e6"), null, true, "Soda", "food-beverage/beverages/soft-drinks/soda", "soda", 274, new Guid("ad02f5d2-9f01-75f9-35b3-1812a5c4f337") },
                    { new Guid("b44328d3-26fd-f0ba-0524-f747e27dde13"), null, true, "Goat ghee", "food-beverage/oils-fats/ghee/goat-ghee", "goat-ghee", 258, new Guid("248af1d2-cb0e-0f0a-8f82-84c11c6caecf") },
                    { new Guid("b46c7bd3-41a9-b7b5-5b85-72c9d50da595"), null, true, "Apparel wholesale", "b2b/wholesale/by-category/apparel-wholesale", "apparel-wholesale", 662, new Guid("9b8c11d2-56e7-f6bc-b367-2562cafeb595") },
                    { new Guid("b48149d3-98d2-a04c-1504-96f7df1b7578"), null, true, "Vegetable stock", "food-beverage/sauces-spices/stock/vegetable-stock", "vegetable-stock", 247, new Guid("3120ffd2-9ab4-a2ce-5185-1bb4c691f055") },
                    { new Guid("b4baf6d3-7dcc-a58d-1611-cd2bb1f6cd5f"), null, true, "Pans", "household/kitchen/cookware/pans", "pans", 442, new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652") },
                    { new Guid("b4fc54d3-86dd-733c-79e3-b06dc361714c"), null, true, "Sponges", "personal-care/cosmetics-makeup/tools/sponges", "sponges", 343, new Guid("5e90d9d2-661a-aeb2-6ba2-3904d1106e2f") },
                    { new Guid("b501e6d3-558a-ee2c-37f8-ce5421ae6c37"), null, true, "Covers", "baby-mother/maternity/nursing/covers", "covers", 590, new Guid("69bea1d2-68d0-38f0-464f-17bbc431cef0") },
                    { new Guid("b5993fd3-b467-a1a3-1f05-d2f1af4626d0"), null, true, "Bran", "food-beverage/rice-grains-staples/cereals/bran", "bran", 112, new Guid("036c3ad2-b97c-4bcb-0ece-e1be8d0bb0df") },
                    { new Guid("b5a6c4d3-35b4-9ef7-5c02-16b5cb258ff0"), null, true, "Acne care", "personal-care/skincare/treatments/acne-care", "acne-care", 317, new Guid("269c54d2-b716-467c-5d4f-9b5afacd0ff2") },
                    { new Guid("b5e057d3-cb4f-b626-93df-19fce744f70e"), null, true, "Prawns", "food-beverage/frozen-food/frozen-seafood/prawns", "prawns", 157, new Guid("e77cafd2-ec5f-39af-b161-5c8527add63f") },
                    { new Guid("b5edfbd3-b025-eaf0-d8b9-6711b8c45e1e"), null, true, "Jersey", "modest-fashion/womens-clothing/hijabs/jersey", "jersey", 472, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("b63d81d3-1590-c885-6272-eb94d907b4a4"), null, true, "Mixed nuts", "food-beverage/snacks-confectionery/nuts-seeds/mixed-nuts", "mixed-nuts", 210, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("b648d6d3-3869-bc73-11d9-72b2aea5e370"), null, true, "Boots", "modest-fashion/footwear/by-type/boots", "boots", 526, new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e") },
                    { new Guid("b6e4a6d3-ec78-9404-8db4-33e7218b66ad"), null, true, "Bedding", "household/home-essentials/textiles/bedding", "bedding", 458, new Guid("5e4f2ad2-aad9-95d2-dcda-da060568463d") },
                    { new Guid("b718aad3-34aa-e8f1-439b-e06ed51ae5f5"), null, true, "Pins", "modest-fashion/fashion-accessories/hijab-accessories/pins", "pins", 510, new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0") },
                    { new Guid("b73efcd3-ed21-ebe3-120e-1f617373f2df"), null, true, "Reed diffusers", "household/home-fragrance/active/reed-diffusers", "reed-diffusers", 434, new Guid("b455e9d2-1637-96bc-1d91-17eccff9ab22") },
                    { new Guid("b848dcd3-382d-cad1-169d-8061000abb8d"), null, true, "Tops", "modest-fashion/kids-clothing/girls/tops", "tops", 502, new Guid("85aa6cd2-5fb1-222a-106d-e8174cd191fb") },
                    { new Guid("b8632cd3-a0d9-ba91-842c-8d24791ce77d"), null, true, "Chicken", "food-beverage/dairy-eggs/eggs/chicken", "chicken", 82, new Guid("034140d2-ae78-36e1-1f4a-1baecde863f8") },
                    { new Guid("b8918ed3-424a-86e7-0668-d22dfc5ec427"), null, true, "Probiotic", "food-beverage/beverages/functional/probiotic", "probiotic", 290, new Guid("e3d391d2-ab2e-d995-1ae6-1b4059e0c915") },
                    { new Guid("b9f2e4d3-d43a-5e37-3964-dd096cf1b518"), null, true, "Biography", "islamic-religious/islamic-books/history/biography", "biography", 557, new Guid("733bdad2-957e-8900-8965-86f883c49c56") },
                    { new Guid("ba34dcd3-b136-a9a6-1801-b84e3226eaf7"), null, true, "Minerals", "health-wellness/vitamins/by-type/minerals", "minerals", 374, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("bb9808d3-809c-299e-b121-03503f027ee5"), null, true, "Probiotics", "health-wellness/vitamins/specialty/probiotics", "probiotics", 382, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("bbc5aed3-c99a-f980-4fd5-8295905de2ed"), null, true, "Black pepper", "food-beverage/sauces-spices/spices/black-pepper", "black-pepper", 241, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("bc30c3d3-9c1c-6d44-d7f0-49e57d595cfe"), null, true, "Organic", "food-beverage/dairy-eggs/eggs/organic", "organic", 85, new Guid("034140d2-ae78-36e1-1f4a-1baecde863f8") },
                    { new Guid("bc5df4d3-f728-f472-6f30-6cc55d3a6be0"), null, true, "Whole", "food-beverage/meat-poultry/goat/whole", "whole", 10, new Guid("1bbd4dd2-ab32-53a3-04d0-e004a74e16f8") },
                    { new Guid("bce626d3-3cbb-6471-036a-21fee22691b6"), null, true, "Whole", "food-beverage/meat-poultry/chicken/whole", "whole", 13, new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4") },
                    { new Guid("bcee8ed3-713a-9d82-4a4f-6c5fdb5bc8c8"), null, true, "Tulsi", "health-wellness/herbal-traditional/herbal-teas/tulsi", "tulsi", 402, new Guid("9be6d2d2-078d-fa70-816b-665c204e5482") },
                    { new Guid("bcffb1d3-9c5b-c825-8b7d-21cf2b235ee1"), null, true, "Brown", "food-beverage/rice-grains-staples/rice/brown", "brown", 91, new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926") },
                    { new Guid("bd02b9d3-ef60-25bf-6889-2170ef968b78"), null, true, "Quinoa", "food-beverage/rice-grains-staples/oats-grains/quinoa", "quinoa", 102, new Guid("1d30e6d2-c067-2177-403e-f82269279ff4") },
                    { new Guid("bd1027d3-6ad8-d674-9b52-8deafb9ba25e"), null, true, "Livestock feed", "b2b/pet-animal-b2b/animal-feed/livestock-feed", "livestock-feed", 666, new Guid("801120d2-55d3-1b47-cc42-72a206df3883") },
                    { new Guid("bd3286d3-29f4-b941-be14-d12af7b26de9"), null, true, "Jojoba oil", "b2b/cosmetic-ingredients/oils-butters/jojoba-oil", "jojoba-oil", 629, new Guid("097a78d2-4e6b-9f77-9178-022420d06d74") },
                    { new Guid("bd4254d3-99df-1435-2959-e70e90642317"), null, true, "Dresses", "modest-fashion/kids-clothing/girls/dresses", "dresses", 501, new Guid("85aa6cd2-5fb1-222a-106d-e8174cd191fb") },
                    { new Guid("bd50f7d3-755c-232b-3844-73d94911a705"), null, true, "Roots & tubers", "food-beverage/fresh-produce/fresh-vegetables/roots-tubers", "roots-tubers", 125, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("be49ddd3-9116-8389-0557-c7e7e73ee789"), null, true, "Cup noodles", "food-beverage/ready-to-eat/instant-meals/cup-noodles", "cup-noodles", 184, new Guid("771918d2-92fb-f616-b356-33dc5d6ad7b8") },
                    { new Guid("be7ad2d3-e929-8391-2aac-2b63c88bbfe5"), null, true, "Tuna", "food-beverage/ready-to-eat/canned-meals/tuna", "tuna", 181, new Guid("b2ea6bd2-3ee3-0222-1343-8abc7030d643") },
                    { new Guid("be9d34d3-1960-e0aa-fe94-057355790b82"), null, true, "Body mist", "personal-care/fragrance/perfume/body-mist", "body-mist", 346, new Guid("b779ecd2-8416-1ca5-589c-54e76e66565a") },
                    { new Guid("bf05e0d3-3436-5148-c25d-523e9d246a3d"), null, true, "Parsley", "food-beverage/fresh-produce/fresh-herbs/parsley", "parsley", 135, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("bf1b97d3-fdbf-e660-1efe-5164629ccf59"), null, true, "Basil", "food-beverage/fresh-produce/fresh-herbs/basil", "basil", 132, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("bf2c10d3-286d-e34e-1894-dfec98645594"), null, true, "Prayer cap", "islamic-religious/prayer/prayer-accessories/prayer-cap", "prayer-cap", 547, new Guid("f367d2d2-7b83-309b-1c2f-8dc0ba59c0b5") },
                    { new Guid("c24146d3-a938-6266-dc0a-613fabf30796"), null, true, "Rice cookers", "household/kitchen/cookware/rice-cookers", "rice-cookers", 445, new Guid("73b9f7d2-5b8f-0cfb-8756-940820923652") },
                    { new Guid("c2e3aad3-357c-cfab-0f8e-3a58b52d2053"), null, true, "Belly binding", "baby-mother/maternity/postpartum/belly-binding", "belly-binding", 593, new Guid("0e5eced2-ca03-32b6-7af0-33c79642edfb") },
                    { new Guid("c2e990d3-bf7c-eb8c-29fc-07dd23c59551"), null, true, "Mobility aids", "health-wellness/healthcare/medical-devices/mobility-aids", "mobility-aids", 410, new Guid("a63cddd2-fee7-1d9f-eacc-a5d6a951ca38") },
                    { new Guid("c34700d3-6c11-8ed6-85c0-bcc9029d5163"), null, true, "Deodorant", "personal-care/bath-body/spa/deodorant", "deodorant", 360, new Guid("f1ea79d2-5a1b-9032-e4da-39a839c285fa") },
                    { new Guid("c34bb8d3-4968-dc40-14a1-fc94f0303874"), null, true, "Organisers", "household/home-essentials/home-accessories/organisers", "organisers", 463, new Guid("18658ed2-2781-1d0f-7b71-645486a75898") },
                    { new Guid("c386cdd3-4ae4-7488-e432-a90e0a161394"), null, true, "Carnauba", "b2b/cosmetic-ingredients/waxes/carnauba", "carnauba", 634, new Guid("aab407d2-b3b4-55dc-b2e4-86cd8f991836") },
                    { new Guid("c39053d3-02a2-2286-85aa-a612d25d9fe7"), null, true, "Bedding", "baby-mother/baby-essentials/sleep-comfort/bedding", "bedding", 600, new Guid("1b553bd2-c435-79ce-5d72-e4de2539c86c") },
                    { new Guid("c4c729d3-3217-acac-2578-b21e2669ecb5"), null, true, "Prayer dress", "modest-fashion/womens-clothing/prayer-wear-women/prayer-dress", "prayer-dress", 478, new Guid("925043d2-44cd-d364-0cb2-08cd7d1e7d4b") },
                    { new Guid("c4d20cd3-bb7c-2943-5ee4-6bcabedb2dc4"), null, true, "Eid gifts", "islamic-religious/islamic-gifts/ornaments/eid-gifts", "eid-gifts", 564, new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5") },
                    { new Guid("c632b9d3-ee29-7db6-8876-47141bbbf743"), null, true, "Brooches", "modest-fashion/fashion-accessories/hijab-accessories/brooches", "brooches", 511, new Guid("ffa531d2-5194-4d7e-6eef-fe7e150e9cf0") },
                    { new Guid("c63e83d3-57e6-4605-cef3-dbb1c7664377"), null, true, "Udon", "food-beverage/rice-grains-staples/pasta-noodles/udon", "udon", 110, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("c64d0cd3-63ca-c43e-4a0d-90fc3d6057df"), null, true, "Sleep sacks", "baby-mother/baby-essentials/sleep-comfort/sleep-sacks", "sleep-sacks", 602, new Guid("1b553bd2-c435-79ce-5d72-e4de2539c86c") },
                    { new Guid("c64e18d3-4f97-bbec-a5a9-08bad011872b"), null, true, "Ice cream", "food-beverage/frozen-food/frozen-desserts/ice-cream", "ice-cream", 172, new Guid("195badd2-1777-8b8b-076e-578874a8f7d4") },
                    { new Guid("c68eb0d3-7745-c839-e5b2-4f1d7a64c334"), null, true, "Cloth diapers", "baby-mother/baby-care/diapers-wipes/cloth-diapers", "cloth-diapers", 582, new Guid("6a3304d2-ffb2-a73a-84e7-299d7270a3a0") },
                    { new Guid("c6af92d3-0e46-6807-2857-c320ceaa45ae"), null, true, "Enzymes", "b2b/halal-ingredients/functional-ingredients/enzymes", "enzymes", 615, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("c6dae9d3-9944-6e60-89d8-196a894e3ac9"), null, true, "Moisturizers", "personal-care/skincare/facial/moisturizers", "moisturizers", 309, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("c73355d3-60ee-3962-d16b-248ec0d15447"), null, true, "Cut fruit", "food-beverage/fresh-produce/fresh-cut/cut-fruit", "cut-fruit", 148, new Guid("165b72d2-7a36-a783-4101-1a1dd081e0f6") },
                    { new Guid("c75b60d3-ac73-a0a8-551f-ac9ac45f56f5"), null, true, "Granola", "food-beverage/snacks-confectionery/snack-bars/granola", "granola", 213, new Guid("5c9b1dd2-9b94-dcc8-73c1-42d054df30d5") },
                    { new Guid("c7d6b7d3-3a60-99f2-6923-ba8581d60923"), null, true, "Bean", "food-beverage/ready-to-eat/canned-meals/bean", "bean", 182, new Guid("b2ea6bd2-3ee3-0222-1343-8abc7030d643") },
                    { new Guid("c7f07ad3-8bf3-45b6-7e7d-5f0de6980af7"), null, true, "Knives", "household/kitchen/utensils/knives", "knives", 453, new Guid("a5afa7d2-a47f-4bfe-7589-e75cdf750380") },
                    { new Guid("c8c869d3-c9d0-1fcb-eb3e-62580912687e"), null, true, "Quran stand", "islamic-religious/quran/quran-accessories/quran-stand", "quran-stand", 536, new Guid("915801d2-844d-ec5d-fcc2-656ec7e8910e") },
                    { new Guid("c91a1ed3-49cb-3bd5-922d-ea2462641a44"), null, true, "Curry", "food-beverage/ready-to-eat/ready-meals/curry", "curry", 177, new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4") },
                    { new Guid("cad963d3-cb83-b9d3-484a-d673e61f228f"), null, true, "Mixed", "food-beverage/frozen-food/frozen-seafood/mixed", "mixed", 158, new Guid("e77cafd2-ec5f-39af-b161-5c8527add63f") },
                    { new Guid("cbbe22d3-ada0-dca1-a9fb-58e9369cf0cd"), null, true, "Chamomile", "health-wellness/herbal-traditional/herbal-teas/chamomile", "chamomile", 399, new Guid("9be6d2d2-078d-fa70-816b-665c204e5482") },
                    { new Guid("cbfb97d3-78a3-8671-3a90-9239ff763d66"), null, true, "Ambient", "b2b/wholesale/by-logistics/ambient", "ambient", 664, new Guid("be412ad2-16ea-0a0b-80cc-e870767baf55") },
                    { new Guid("cc1205d3-ae04-a773-2b36-baebe3e5ec83"), null, true, "Diabetic-friendly", "health-wellness/specialty-nutrition/special-dietary/diabetic-friendly", "diabetic-friendly", 418, new Guid("eb2125d2-5dd5-092e-83b8-5db65c8ca7ed") },
                    { new Guid("cc244fd3-df9e-d7d2-d917-ffd685c7fe19"), null, true, "Disposable food service", "b2b/horeca/catering/disposables", "disposables", 648, new Guid("6d5f2dd2-bc68-2e75-a3fe-adac70a08b37") },
                    { new Guid("cc294dd3-ae80-a318-91b0-1d7c9c6ee625"), null, true, "Ice cream tubs", "food-beverage/dairy-eggs/ice-cream/tubs", "tubs", 74, new Guid("2ea98cd2-0b7e-5ed3-6c9d-22c5096ae03a") },
                    { new Guid("cc362cd3-3f83-f485-e3b6-9b59a32bb972"), null, true, "Alliums", "food-beverage/fresh-produce/fresh-vegetables/alliums", "alliums", 127, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("cc553cd3-08a1-9071-45fd-7e7bc9040172"), null, true, "Cuts", "food-beverage/meat-poultry/turkey/cuts", "cuts", 19, new Guid("ba652ad2-de24-d0b3-e225-cbdeb99d1e5f") },
                    { new Guid("cca8b3d3-2771-b811-0037-837dafe40f3e"), null, true, "Habbatus sauda", "health-wellness/herbal-traditional/herbal-supps/habbatus-sauda", "habbatus-sauda", 385, new Guid("4589dbd2-42ce-30c4-aa8e-a7091d05724f") },
                    { new Guid("cd0d83d3-8981-013f-9419-013ae57fc28f"), null, true, "Plain", "modest-fashion/mens-clothing/thobes/plain", "plain", 489, new Guid("a630a3d2-82bb-fc2d-65c2-ade5c151211f") },
                    { new Guid("cd9813d3-ce4d-d599-7558-5bb2295d476e"), null, true, "Mixed", "food-beverage/frozen-food/frozen-fruit/mixed", "mixed", 164, new Guid("18fb36d2-b5a9-258a-ed9c-01cb0738e9ab") },
                    { new Guid("ce482bd3-bb06-85e5-7207-70efadb126ff"), null, true, "Gluten-free", "food-beverage/rice-grains-staples/wheat-flour/gluten-free", "gluten-free", 98, new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be") },
                    { new Guid("d03c2fd3-adad-28d9-94f2-80c48f0c59c7"), null, true, "Food essences", "b2b/halal-ingredients/flavour/essences", "essences", 619, new Guid("7b3aa8d2-4cdc-c22a-f308-77485f6ff511") },
                    { new Guid("d04fd5d3-3aa6-a22f-b1ac-b03777f2b957"), null, true, "Cough & cold", "health-wellness/healthcare/first-aid/cough-cold", "cough-cold", 406, new Guid("49e85fd2-dc0c-2e5c-6284-5669988a659f") },
                    { new Guid("d056f3d3-316d-2a07-2de7-2f46a6268ed8"), null, true, "Boxes", "food-beverage/snacks-confectionery/chocolate/boxes", "boxes", 200, new Guid("058e65d2-fcd1-59a1-12be-14e797000487") },
                    { new Guid("d05c9ed3-622b-8fe9-a83e-93832b41db9f"), null, true, "Pome", "food-beverage/fresh-produce/fresh-fruits/pome", "pome", 122, new Guid("c2dab8d2-089a-c27c-e1c4-9bacb4050b99") },
                    { new Guid("d07ec5d3-5a75-0f6a-2a6e-fc7a70762051"), null, true, "Frozen", "food-beverage/seafood/squid-octopus/frozen", "frozen", 44, new Guid("f12708d2-d7dd-f11c-ab9d-1fb251073f15") },
                    { new Guid("d0abd7d3-354e-676d-74c5-5dee1a96f3ce"), null, true, "Sunflower oil", "food-beverage/oils-fats/cooking-oils/sunflower-oil", "sunflower-oil", 250, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("d13f2dd3-1315-444e-28ba-aec0ba41bbae"), null, true, "Pain relief", "health-wellness/healthcare/first-aid/pain-relief", "pain-relief", 405, new Guid("49e85fd2-dc0c-2e5c-6284-5669988a659f") },
                    { new Guid("d24dccd3-644a-6601-4973-24ec5f5d76d6"), null, true, "Hyaluronic acid", "b2b/cosmetic-ingredients/extracts/hyaluronic", "hyaluronic", 637, new Guid("c278d2d2-6301-8192-242e-145233b94bfa") },
                    { new Guid("d2b024d3-39bc-576d-70ac-c6dca5e1a9a5"), null, true, "Calcium", "health-wellness/vitamins/by-type/calcium", "calcium", 376, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("d2cc12d3-919b-d90d-8bc5-96d0eb118344"), null, true, "Serums", "personal-care/skincare/facial/serums", "serums", 308, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("d37806d3-4c41-389f-1baf-249d1e518a06"), null, true, "Seeds", "food-beverage/snacks-confectionery/nuts-seeds/seeds", "seeds", 211, new Guid("b95165d2-b5f3-4b14-ba2b-15bbb0b43fa4") },
                    { new Guid("d39dd4d3-a0f4-8635-4480-ad27d8513185"), null, true, "Instant", "modest-fashion/womens-clothing/hijabs/instant", "instant", 476, new Guid("eda500d2-ba65-ff68-a733-6e6a7385c941") },
                    { new Guid("d3bc2cd3-c11a-1266-4204-cc35eaeeb0cd"), null, true, "Canned shellfish", "food-beverage/seafood/canned-preserved/canned-shellfish", "canned-shellfish", 51, new Guid("b3ecf0d2-691f-ff7d-507a-e934cf8e93dc") },
                    { new Guid("d3ca4bd3-2b1c-6ccb-16c8-9230c894efd0"), null, true, "Vitamin D", "health-wellness/vitamins/by-type/vitamin-d", "vitamin-d", 372, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("d4506dd3-d613-488a-18f3-0c3c1f9a3eac"), null, true, "Herbal", "baby-mother/maternity/postpartum/herbal", "herbal", 594, new Guid("0e5eced2-ca03-32b6-7af0-33c79642edfb") },
                    { new Guid("d46900d3-1ec0-6c25-967e-ae7521bb42e3"), null, true, "Glass", "household/cleaning/surface/glass", "glass", 425, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("d47060d3-0bf8-1715-aa44-7c1083a462f6"), null, true, "Bulk ingredients", "b2b/horeca/restaurant/bulk-ingredients", "bulk-ingredients", 643, new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f") },
                    { new Guid("d5db17d3-9f29-fd9d-d2c4-d60d90b66f09"), null, true, "Gift sets", "islamic-religious/islamic-gifts/ornaments/gift-sets", "gift-sets", 562, new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5") },
                    { new Guid("d609ced3-724a-6cd4-479c-dc5a28de94c0"), null, true, "Pastry", "food-beverage/frozen-food/frozen-desserts/pastry", "pastry", 174, new Guid("195badd2-1777-8b8b-076e-578874a8f7d4") },
                    { new Guid("d61cd9d3-3ad6-2840-26f4-3b51c3b48c31"), null, true, "F&B wholesale", "b2b/wholesale/by-category/fb-wholesale", "fb-wholesale", 660, new Guid("9b8c11d2-56e7-f6bc-b367-2562cafeb595") },
                    { new Guid("d61ea5d3-b46e-5c6b-768e-b782b178ba6e"), null, true, "Cornflakes", "food-beverage/rice-grains-staples/cereals/cornflakes", "cornflakes", 111, new Guid("036c3ad2-b97c-4bcb-0ece-e1be8d0bb0df") },
                    { new Guid("d64297d3-cf8b-7203-ea71-2431ea39f8fc"), null, true, "Olive oil", "food-beverage/oils-fats/cooking-oils/olive-oil", "olive-oil", 249, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("d64c4ed3-1634-548d-843e-9266fb49cccc"), null, true, "Marinated", "food-beverage/meat-poultry/chicken/marinated", "marinated", 16, new Guid("09297bd2-de1b-5878-4a52-ec2927eadfa4") },
                    { new Guid("d6d7bbd3-90cb-2b4c-ff9c-14993a0010e6"), null, true, "Thyme", "food-beverage/fresh-produce/fresh-herbs/thyme", "thyme", 136, new Guid("341decd2-a9d9-e834-6813-186654d2a475") },
                    { new Guid("d81332d3-f4ba-2ebb-0539-5601995d61e6"), null, true, "Dry shampoo", "personal-care/haircare/shampoo-conditioner/dry-shampoo", "dry-shampoo", 323, new Guid("401953d2-1437-5628-d42c-e4c6f4a1a243") },
                    { new Guid("d8204dd3-534a-9b3b-d157-e9718fec48f8"), null, true, "Za'atar", "food-beverage/sauces-spices/spices/zaatar", "zaatar", 244, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("d830b8d3-0e03-b8b4-2ab6-4717dfa3fe6b"), null, true, "Puff", "food-beverage/frozen-food/frozen-snacks/puff", "puff", 171, new Guid("931af1d2-1327-3650-c921-7013f1061837") },
                    { new Guid("d88cf1d3-281b-e779-6c92-8a029ce8ef2e"), null, true, "Wipes", "baby-mother/baby-care/diapers-wipes/wipes", "wipes", 583, new Guid("6a3304d2-ffb2-a73a-84e7-299d7270a3a0") },
                    { new Guid("d8c767d3-28c3-13b0-d7be-8a83419dd9d4"), null, true, "Disposable diapers", "baby-mother/baby-care/diapers-wipes/disposable-diapers", "disposable-diapers", 581, new Guid("6a3304d2-ffb2-a73a-84e7-299d7270a3a0") },
                    { new Guid("d8d739d3-6bce-6b1e-bf92-64bbc0256d06"), null, true, "Drinkable", "food-beverage/dairy-eggs/yogurt/drinkable", "drinkable", 66, new Guid("fe0861d2-40c3-d2b1-0b67-1d20ee82a28f") },
                    { new Guid("d92cd4d3-c762-70b3-092d-2492d96fd1be"), null, true, "Cucumbers & squash", "food-beverage/fresh-produce/fresh-vegetables/cucumbers-squash", "cucumbers-squash", 130, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("da0399d3-0b0e-7d93-9e8c-039b951cc085"), null, true, "Sunscreen", "personal-care/skincare/facial/sunscreen", "sunscreen", 311, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("da57d0d3-af50-1ba6-3fae-83f92cdc3f35"), null, true, "Children's", "health-wellness/vitamins/specialty/children", "children", 384, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("daf627d3-7361-c5e4-44d3-8dca3e3539e7"), null, true, "Minced", "food-beverage/meat-poultry/turkey/minced", "minced", 20, new Guid("ba652ad2-de24-d0b3-e225-cbdeb99d1e5f") },
                    { new Guid("db33aed3-ed92-fe58-c14a-ddbb6bc82040"), null, true, "Anti-aging", "personal-care/skincare/treatments/anti-aging", "anti-aging", 318, new Guid("269c54d2-b716-467c-5d4f-9b5afacd0ff2") },
                    { new Guid("db33d8d3-8eea-1fce-ad9d-25ebbd6c2c3a"), null, true, "Backpack", "modest-fashion/fashion-accessories/bags-wallets/backpack", "backpack", 518, new Guid("d47113d2-1fae-5f46-19f6-aaa8f71300b9") },
                    { new Guid("dcb2bad3-259b-4c99-3368-9acfab0895b2"), null, true, "Halloumi", "food-beverage/dairy-eggs/cheese/halloumi", "halloumi", 60, new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5") },
                    { new Guid("dcbadcd3-ab8e-4337-228b-989476f2e7f2"), null, true, "Canned meat", "food-beverage/meat-poultry/processed-meat/canned-meat", "canned-meat", 26, new Guid("d55da6d2-b95b-d042-92be-af6c6774eaf6") },
                    { new Guid("dd7864d3-88f7-faa1-a0da-61fef2af9a93"), null, true, "Stage 3", "food-beverage/baby-food/infant-formula/stage-3", "stage-3", 300, new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161") },
                    { new Guid("ddc586d3-9c0f-7965-4540-d84590487d0d"), null, true, "Hijabs", "modest-fashion/kids-clothing/girls/hijabs", "hijabs", 499, new Guid("85aa6cd2-5fb1-222a-106d-e8174cd191fb") },
                    { new Guid("df0a94d3-6a73-f51e-be58-4c205f188955"), null, true, "Instant", "food-beverage/beverages/coffee/instant", "instant", 284, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("df56a2d3-e7ec-228e-0853-bb96430c670b"), null, true, "Translation", "islamic-religious/quran/qurans/translation", "translation", 532, new Guid("d26529d2-7b80-8562-4b88-23ce074e5ccf") },
                    { new Guid("e02758d3-90ab-86e6-29ac-61907bbb71a0"), null, true, "F&B formulations", "b2b/oem/by-product/fb-formulations", "fb-formulations", 654, new Guid("543c5cd2-7728-3b67-2497-8c8e79b1ff89") },
                    { new Guid("e0b6f6d3-bae4-21ea-79e0-d1adb1861e4c"), null, true, "Lactose-free", "food-beverage/dairy-eggs/milk/lactose-free", "lactose-free", 57, new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff") },
                    { new Guid("e114a7d3-2eba-0736-42d2-682779e6ba0d"), null, true, "Toddler", "food-beverage/baby-food/infant-formula/toddler", "toddler", 301, new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161") },
                    { new Guid("e11c61d3-9564-1dfc-8e71-d5c2a66486ba"), null, true, "Fresh", "food-beverage/dairy-eggs/milk/fresh", "fresh", 53, new Guid("d0951bd2-65a7-625f-2e91-03222f1642ff") },
                    { new Guid("e19897d3-ffa7-f533-d5fa-e160717372d4"), null, true, "Whole wheat", "food-beverage/rice-grains-staples/wheat-flour/whole-wheat", "whole-wheat", 95, new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be") },
                    { new Guid("e19b29d3-3f6d-2ab5-776e-c28368e7ffa3"), null, true, "Water", "food-beverage/baby-food/children-drinks/water", "water", 305, new Guid("4108d4d2-d111-f301-c2c6-231985a2b227") },
                    { new Guid("e1c8d3d3-8b54-4086-f61c-759382ff6a00"), null, true, "Cuts", "food-beverage/frozen-food/frozen-poultry/cuts", "cuts", 155, new Guid("983f12d2-694d-8266-346d-a947ef94cbeb") },
                    { new Guid("e25b7ad3-8c91-13c2-ed21-3e00e06cd8aa"), null, true, "Cumin", "food-beverage/sauces-spices/spices/cumin", "cumin", 239, new Guid("3c06cfd2-ccd3-495d-819d-76553b497504") },
                    { new Guid("e27138d3-7278-9197-e847-d96166415aa2"), null, true, "Truffles", "food-beverage/snacks-confectionery/chocolate/truffles", "truffles", 201, new Guid("058e65d2-fcd1-59a1-12be-14e797000487") },
                    { new Guid("e2a441d3-f118-fa27-1e6b-97a5eb055f57"), null, true, "Beans", "food-beverage/beverages/coffee/beans", "beans", 282, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("e319c4d3-1895-59f6-fba9-969b6083bb8d"), null, true, "Personal care wholesale", "b2b/wholesale/by-category/pc-wholesale", "pc-wholesale", 661, new Guid("9b8c11d2-56e7-f6bc-b367-2562cafeb595") },
                    { new Guid("e33236d3-20ee-64c8-67ae-806bef1aba39"), null, true, "Hand creams", "personal-care/skincare/body-care/hand-creams", "hand-creams", 315, new Guid("1e9766d2-8e14-bc32-0e66-d58542d25d58") },
                    { new Guid("e35b60d3-5a03-3010-6a2a-c465e77fb3cc"), null, true, "Noodle", "food-beverage/ready-to-eat/ready-meals/noodle", "noodle", 176, new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4") },
                    { new Guid("e35bced3-4c3e-fe0f-6f65-daa77fe7f1e3"), null, true, "All-purpose", "food-beverage/rice-grains-staples/wheat-flour/all-purpose", "all-purpose", 94, new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be") },
                    { new Guid("e3d456d3-09d7-1716-d16f-6bf50c590d2a"), null, true, "Curry", "food-beverage/frozen-food/frozen-meals/curry", "curry", 165, new Guid("272b18d2-b8d2-297b-7e32-94c38f278611") },
                    { new Guid("e3f9f3d3-61fc-18de-ccc7-1b4474170b70"), null, true, "Soy", "food-beverage/dairy-eggs/milk-alt/soy", "soy", 80, new Guid("e67ff7d2-9d3d-e8b9-e8b9-388ad6a76031") },
                    { new Guid("e4077fd3-8ba3-43d9-b298-34e48e69e139"), null, true, "Children", "modest-fashion/footwear/by-audience/children", "children", 530, new Guid("39b160d2-54bf-974b-9bda-82ac72849672") },
                    { new Guid("e420d8d3-badf-f66a-1c04-23a03622a391"), null, true, "Button", "food-beverage/fresh-produce/mushrooms/button", "button", 138, new Guid("b2840bd2-908f-8c33-828d-79bad56d006c") },
                    { new Guid("e4d960d3-aaf1-48c6-3ce5-25fd2fdd6292"), null, true, "Fresh", "food-beverage/seafood/prawns-shrimp/fresh", "fresh", 34, new Guid("a16371d2-6324-444c-cebf-da38e1307dbb") },
                    { new Guid("e6553ed3-893b-2d36-918a-1cd83eda8b1c"), null, true, "Coconut", "food-beverage/dairy-eggs/milk-alt/coconut", "coconut", 81, new Guid("e67ff7d2-9d3d-e8b9-e8b9-388ad6a76031") },
                    { new Guid("e7f5c2d3-0465-e840-a079-8001f7df09ff"), null, true, "Face masks", "personal-care/skincare/facial/face-masks", "face-masks", 310, new Guid("4b0400d2-4b11-4207-00e8-3dddb9975dd8") },
                    { new Guid("e86840d3-919c-2467-e997-733f3760ca8e"), null, true, "Rice", "food-beverage/ready-to-eat/ready-meals/rice", "rice", 175, new Guid("3c1be8d2-d996-b424-a42c-b85af8e7cdd4") },
                    { new Guid("e8a24ad3-613b-029a-a52f-152943af627e"), null, true, "Cocoa butter", "b2b/cosmetic-ingredients/oils-butters/cocoa-butter", "cocoa-butter", 631, new Guid("097a78d2-4e6b-9f77-9178-022420d06d74") },
                    { new Guid("e8e76ed3-5e91-d046-5c4b-51900c0cec06"), null, true, "Royal jelly", "health-wellness/herbal-traditional/traditional/royal-jelly", "royal-jelly", 393, new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429") },
                    { new Guid("e97a70d3-29b3-b92a-73bd-ba94060b6816"), null, true, "Fresh", "food-beverage/dairy-eggs/cheese/fresh", "fresh", 58, new Guid("05aafed2-d5fc-e6c7-10e0-c8029dd8e2a5") },
                    { new Guid("e9fb69d3-b2bc-f9ea-e7b8-9e7eeb1bb13f"), null, true, "Floor", "household/cleaning/surface/floor", "floor", 424, new Guid("d7cfafd2-f20c-d229-e62b-4b5f8dff13aa") },
                    { new Guid("ea03b4d3-629a-07e1-c5fd-16e32725d34a"), null, true, "Abayas", "modest-fashion/kids-clothing/girls/abayas", "abayas", 500, new Guid("85aa6cd2-5fb1-222a-106d-e8174cd191fb") },
                    { new Guid("ea45dfd3-7283-b2eb-7d4b-1d47ebbfee28"), null, true, "Tapioca", "food-beverage/snacks-confectionery/chips/tapioca", "tapioca", 193, new Guid("328929d2-a919-8811-3c80-20c5fd6981ff") },
                    { new Guid("eb5411d3-90f3-76af-0fd6-695d3910c92d"), null, true, "Topi", "modest-fashion/mens-clothing/prayer-wear-men/topi", "topi", 498, new Guid("669517d2-a3ae-d7e1-f93f-536676cb582e") },
                    { new Guid("eb6e90d3-f76b-3ab7-f3fb-eb6d7c7bd4d3"), null, true, "Mango", "food-beverage/frozen-food/frozen-fruit/mango", "mango", 163, new Guid("18fb36d2-b5a9-258a-ed9c-01cb0738e9ab") },
                    { new Guid("ebd184d3-50fd-6a43-bd7f-471c619839db"), null, true, "Decaf", "food-beverage/beverages/coffee/decaf", "decaf", 287, new Guid("50a758d2-e0b2-e472-305d-879c98566871") },
                    { new Guid("eccf82d3-cc18-7317-1ba4-65389b528c50"), null, true, "Whole / carcase", "food-beverage/meat-poultry/beef/whole-carcase", "whole-carcase", 5, new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda") },
                    { new Guid("ed5852d3-0da2-a366-c932-777485cfe8ed"), null, true, "Shampoo", "baby-mother/baby-care/bath-skin/shampoo", "shampoo", 576, new Guid("36e509d2-2835-3733-4ae9-e68bb3ef2dfd") },
                    { new Guid("edc051d3-e042-077c-2f4b-59d47da3e58f"), null, true, "Almond milk", "food-beverage/beverages/plant-drinks/almond-milk", "almond-milk", 293, new Guid("aa7cacd2-f2a0-11fe-8505-69723bb65d6a") },
                    { new Guid("edff8ed3-c1ff-b2b6-f51f-6eb3d7c1205b"), null, true, "Cupcakes", "food-beverage/bakery/cakes/cupcakes", "cupcakes", 223, new Guid("90edcdd2-edd0-bf35-80ca-30905103bb15") },
                    { new Guid("ee2d7cd3-892a-c2c1-7768-2f7f90c427f0"), null, true, "Seerah", "islamic-religious/islamic-books/history/seerah", "seerah", 555, new Guid("733bdad2-957e-8900-8965-86f883c49c56") },
                    { new Guid("eec651d3-0c6b-b17e-16b1-d6ab8eb8c087"), null, true, "Tops", "baby-mother/maternity/maternity-clothing/tops", "tops", 586, new Guid("271a15d2-2ac1-fafb-cb6d-068d7133640e") },
                    { new Guid("efb825d3-5e70-2cb5-c378-25f1bbe3d744"), null, true, "Fabric softener", "household/cleaning/laundry/softener", "softener", 428, new Guid("6b4e2fd2-fb7e-d6b7-99e7-1348fa428a43") },
                    { new Guid("efc8ecd3-7529-cf4c-5f80-88cc6b4f5097"), null, true, "Trousers", "modest-fashion/womens-clothing/modest-everyday/trousers", "trousers", 483, new Guid("bd0958d2-0973-9d43-bff7-7602bdb86ab6") },
                    { new Guid("efde22d3-be1a-8773-6a80-b35e59a2701d"), null, true, "Bottles", "baby-mother/baby-essentials/feeding/bottles", "bottles", 596, new Guid("ad53b4d2-044d-b4ad-4755-0ed5f1105690") },
                    { new Guid("efefcad3-287a-40d0-afcf-d2699ef3c562"), null, true, "Fruit", "food-beverage/snacks-confectionery/snack-bars/fruit", "fruit", 216, new Guid("5c9b1dd2-9b94-dcc8-73c1-42d054df30d5") },
                    { new Guid("f05869d3-46c7-7271-9176-e5e0731a9481"), null, true, "Dried figs", "food-beverage/fresh-produce/dates-dried/dried-figs", "dried-figs", 146, new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142") },
                    { new Guid("f07782d3-8aaa-9b6c-2c5d-0d88b4bfb91f"), null, true, "Collagen-alternative", "food-beverage/beverages/functional/collagen-alt", "collagen-alt", 291, new Guid("e3d391d2-ab2e-d995-1ae6-1b4059e0c915") },
                    { new Guid("f098d5d3-00bb-3b4e-2b3e-61a19f5e0916"), null, true, "Cruciferous", "food-beverage/fresh-produce/fresh-vegetables/cruciferous", "cruciferous", 126, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("f17533d3-d909-cc22-c247-c2f926c3a62f"), null, true, "Vegetable shortening", "food-beverage/oils-fats/shortening/vegetable-shortening", "vegetable-shortening", 259, new Guid("46d91bd2-cec3-9851-1cd1-f89f54bb31d9") },
                    { new Guid("f2062ad3-60b7-5452-cd7f-830b58afabd0"), null, true, "Glutinous", "food-beverage/rice-grains-staples/rice/glutinous", "glutinous", 92, new Guid("8612ecd2-aae8-92c0-ced9-87d51f6e9926") },
                    { new Guid("f21c9cd3-99b8-f967-e6f5-96d441483738"), null, true, "Penne", "food-beverage/rice-grains-staples/pasta-noodles/penne", "penne", 105, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("f26904d3-1763-e755-6ecc-06a927689dcf"), null, true, "Curtains", "household/home-essentials/textiles/curtains", "curtains", 460, new Guid("5e4f2ad2-aad9-95d2-dcda-da060568463d") },
                    { new Guid("f2a1c2d3-9d90-2648-6ab1-3492b6bd4160"), null, true, "Fiqh", "islamic-religious/islamic-books/hadith-fiqh/fiqh", "fiqh", 550, new Guid("779198d2-09c8-8c6b-ee20-dbc5f841c99c") },
                    { new Guid("f33118d3-6610-1aea-7a26-0a8ef03a5814"), null, true, "Mousse", "food-beverage/dairy-eggs/dairy-desserts/mousse", "mousse", 87, new Guid("aab9bfd2-a883-6348-6215-92e1aaca2a0c") },
                    { new Guid("f3622fd3-2d6e-7fac-791f-336720ee9f0e"), null, true, "Air freshener spray", "household/home-fragrance/active/air-spray", "air-spray", 433, new Guid("b455e9d2-1637-96bc-1d91-17eccff9ab22") },
                    { new Guid("f38bb0d3-1769-86a9-7db3-347cc64e7bab"), null, true, "Eyeshadow", "personal-care/cosmetics-makeup/eyes/eyeshadow", "eyeshadow", 337, new Guid("3a5915d2-dcfd-9506-e745-953bab655b81") },
                    { new Guid("f4441ed3-9685-36d1-b51c-98445d9a2455"), null, true, "Mascara", "personal-care/cosmetics-makeup/eyes/mascara", "mascara", 336, new Guid("3a5915d2-dcfd-9506-e745-953bab655b81") },
                    { new Guid("f488c5d3-83e7-eddf-5c80-73ce653bad0f"), null, true, "Fusilli", "food-beverage/rice-grains-staples/pasta-noodles/fusilli", "fusilli", 106, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("f57085d3-b341-921f-a197-d7ef9858571e"), null, true, "Green", "food-beverage/beverages/tea/green", "green", 276, new Guid("f37dd0d2-d829-a397-efc5-83729d4acdfe") },
                    { new Guid("f5a11dd3-4c5b-daa4-dc3a-b9f1e9873377"), null, true, "Poultry feed", "b2b/pet-animal-b2b/animal-feed/poultry-feed", "poultry-feed", 667, new Guid("801120d2-55d3-1b47-cc42-72a206df3883") },
                    { new Guid("f5a679d3-47ff-ef53-d09b-1ad0fa3e69f2"), null, true, "Dried apricots", "food-beverage/fresh-produce/dates-dried/dried-apricots", "dried-apricots", 145, new Guid("55e9fdd2-058a-d34f-f37f-650d9cf70142") },
                    { new Guid("f62de2d3-030b-e5b5-1259-b8bd086f276e"), null, true, "Dishwasher tablets", "household/cleaning/dishwashing/dishwasher-tablets", "dishwasher-tablets", 431, new Guid("d213fad2-d5e5-a59a-a642-5c63c488948d") },
                    { new Guid("f694d3d3-9e43-8a77-9b4b-fc5bb431b88e"), null, true, "Lip balm", "personal-care/cosmetics-makeup/lips/lip-balm", "lip-balm", 340, new Guid("e47115d2-f163-dc61-232d-96825d1f1f7f") },
                    { new Guid("f6f885d3-06ef-9d29-9472-9eb0a413fc53"), null, true, "Multivitamins", "health-wellness/vitamins/by-type/multivitamins", "multivitamins", 370, new Guid("9376bdd2-eab4-0f22-2cfd-7bb2c6070da1") },
                    { new Guid("f71e1ad3-732a-1a6b-7d65-f6d9c1e4182a"), null, true, "Weight-management", "health-wellness/specialty-nutrition/special-dietary/weight-management", "weight-management", 419, new Guid("eb2125d2-5dd5-092e-83b8-5db65c8ca7ed") },
                    { new Guid("f74fcdd3-2fa4-196e-517f-88d7e9d0d111"), null, true, "Prayer kits", "b2b/horeca/hotel/prayer-kits", "prayer-kits", 652, new Guid("47d502d2-644a-bca0-fa90-41882f752db5") },
                    { new Guid("f81837d3-683c-ef57-5141-02e31035ec88"), null, true, "Aqeedah", "islamic-religious/islamic-books/hadith-fiqh/aqeedah", "aqeedah", 551, new Guid("779198d2-09c8-8c6b-ee20-dbc5f841c99c") },
                    { new Guid("f86c9dd3-a21c-0545-b954-526ee599ca6c"), null, true, "Frozen", "food-beverage/meat-poultry/beef/frozen", "frozen", 3, new Guid("95d8b1d2-3c4f-9446-21bf-abc6c799beda") },
                    { new Guid("f8d3cdd3-dee3-f151-b17e-5fe10ccfb32c"), null, true, "Self-raising", "food-beverage/rice-grains-staples/wheat-flour/self-raising", "self-raising", 97, new Guid("dff322d2-4c57-7a72-24e6-125c8e5d24be") },
                    { new Guid("f94662d3-638a-9930-69f8-d7ef45d901a0"), null, true, "Collagen alternatives", "health-wellness/vitamins/specialty/collagen-alt", "collagen-alt", 383, new Guid("c0896ed2-e5c4-00a5-357d-038ae0237a7c") },
                    { new Guid("f95542d3-a0d2-d8b5-3768-ff318ba99e6d"), null, true, "Slippers", "modest-fashion/footwear/by-type/slippers", "slippers", 525, new Guid("addbc5d2-6086-f686-1d85-d3b8d253b09e") },
                    { new Guid("f9d9e9d3-2969-6a83-6634-0fb7781d43f8"), null, true, "Emulsifiers", "b2b/halal-ingredients/functional-ingredients/emulsifiers", "emulsifiers", 612, new Guid("97a3d5d2-353b-3b46-1ae5-d98c5bf0d3b2") },
                    { new Guid("f9ffd0d3-f077-9023-1595-f92c9234eb4b"), null, true, "Renal-friendly", "health-wellness/specialty-nutrition/special-dietary/renal-friendly", "renal-friendly", 420, new Guid("eb2125d2-5dd5-092e-83b8-5db65c8ca7ed") },
                    { new Guid("faa88ad3-2710-c560-2017-836acbdec1f2"), null, true, "Ramadan gifts", "islamic-religious/islamic-gifts/ornaments/ramadan-gifts", "ramadan-gifts", 563, new Guid("87c45dd2-f18f-e35f-00ee-678017b8efc5") },
                    { new Guid("fc0188d3-c402-cf7c-78a4-709771bfe2aa"), null, true, "Rice dishes", "food-beverage/frozen-food/frozen-meals/rice-dishes", "rice-dishes", 167, new Guid("272b18d2-b8d2-297b-7e32-94c38f278611") },
                    { new Guid("fc1cbad3-fae5-75c8-033e-6d48fc4947dc"), null, true, "Leafy greens", "food-beverage/fresh-produce/fresh-vegetables/leafy-greens", "leafy-greens", 124, new Guid("4772e0d2-f462-e81d-0afe-42ac386e90cf") },
                    { new Guid("fc5ab1d3-78cc-2434-f180-9538a6d6a670"), null, true, "Stage 2", "food-beverage/baby-food/infant-formula/stage-2", "stage-2", 299, new Guid("5b3e1fd2-b3aa-9799-ebb3-c26238d17161") },
                    { new Guid("fdff80d3-ef8b-262f-40c1-0b7d226c82a5"), null, true, "Sauces & pastes", "b2b/horeca/restaurant/sauces-pastes", "sauces-pastes", 644, new Guid("fbe17bd2-22da-8604-71d7-b4b1668c8f4f") },
                    { new Guid("fe1f15d3-07a1-a6d0-4ad1-e0cc16d6b575"), null, true, "Hair oils", "personal-care/haircare/treatments-styling/hair-oils", "hair-oils", 325, new Guid("cc7110d2-e6e7-5dca-8346-d94956ab579a") },
                    { new Guid("fea81ed3-f7b1-edb3-ad3b-95c19c3df5e0"), null, true, "Coconut oil", "food-beverage/oils-fats/cooking-oils/coconut-oil", "coconut-oil", 252, new Guid("f19fe4d2-bcd4-bbc1-8af0-fb62cb24d0d3") },
                    { new Guid("febe9fd3-0f3b-3163-43e1-8f39a66829a0"), null, true, "Egg noodles", "food-beverage/rice-grains-staples/pasta-noodles/egg-noodles", "egg-noodles", 108, new Guid("0979bdd2-8b4f-0dc0-ee2e-374c42d128c1") },
                    { new Guid("fefe5dd3-994c-95e2-ac32-4a56bb99cd47"), null, true, "Halal minibar", "b2b/horeca/hotel/minibar", "minibar", 650, new Guid("47d502d2-644a-bca0-fa90-41882f752db5") },
                    { new Guid("ff2ee5d3-9bb8-2e75-afe6-cf8a38aa9951"), null, true, "Antioxidants", "b2b/halal-ingredients/additives/antioxidants", "antioxidants", 623, new Guid("f62090d2-8c0b-d9c7-2cdc-a63d3ec59e8c") },
                    { new Guid("ff48e2d3-991b-1375-5920-98b26988ff12"), null, true, "Cereal", "baby-mother/baby-food-d7/cereals-meals/cereal", "cereal", 571, new Guid("7df5f9d2-7a91-dfde-cdbd-608238ba566c") },
                    { new Guid("ffaa3ed3-5a85-e504-7041-e8c614711eda"), null, true, "Propolis", "health-wellness/herbal-traditional/traditional/propolis", "propolis", 394, new Guid("8f4626d2-c65c-680e-cef5-bc4de5cdb429") }
                });

            migrationBuilder.InsertData(
                table: "VerificationEvidence",
                columns: new[] { "Id", "CollectedAt", "Confidence", "EvidenceType", "Source", "VerificationId" },
                values: new object[,]
                {
                    { new Guid("e0010000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0010000-0000-0000-0000-000000000000") },
                    { new Guid("e0010000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0010000-0000-0000-0000-000000000000") },
                    { new Guid("e0020000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0020000-0000-0000-0000-000000000000") },
                    { new Guid("e0020000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0020000-0000-0000-0000-000000000000") },
                    { new Guid("e0030000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0030000-0000-0000-0000-000000000000") },
                    { new Guid("e0030000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0030000-0000-0000-0000-000000000000") },
                    { new Guid("e0040000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0040000-0000-0000-0000-000000000000") },
                    { new Guid("e0040000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0040000-0000-0000-0000-000000000000") },
                    { new Guid("e0050000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0050000-0000-0000-0000-000000000000") },
                    { new Guid("e0050000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0050000-0000-0000-0000-000000000000") },
                    { new Guid("e0060000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0060000-0000-0000-0000-000000000000") },
                    { new Guid("e0060000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0060000-0000-0000-0000-000000000000") },
                    { new Guid("e0070000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0070000-0000-0000-0000-000000000000") },
                    { new Guid("e0070000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0070000-0000-0000-0000-000000000000") },
                    { new Guid("e0080000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0080000-0000-0000-0000-000000000000") },
                    { new Guid("e0080000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0080000-0000-0000-0000-000000000000") },
                    { new Guid("e0090000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0090000-0000-0000-0000-000000000000") },
                    { new Guid("e0090000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0090000-0000-0000-0000-000000000000") },
                    { new Guid("e0100000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0100000-0000-0000-0000-000000000000") },
                    { new Guid("e0100000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0100000-0000-0000-0000-000000000000") },
                    { new Guid("e0110000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0110000-0000-0000-0000-000000000000") },
                    { new Guid("e0110000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0110000-0000-0000-0000-000000000000") },
                    { new Guid("e0120000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0120000-0000-0000-0000-000000000000") },
                    { new Guid("e0120000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0120000-0000-0000-0000-000000000000") },
                    { new Guid("e0130000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0130000-0000-0000-0000-000000000000") },
                    { new Guid("e0130000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0130000-0000-0000-0000-000000000000") },
                    { new Guid("e0140000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0140000-0000-0000-0000-000000000000") },
                    { new Guid("e0140000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0140000-0000-0000-0000-000000000000") },
                    { new Guid("e0150000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0150000-0000-0000-0000-000000000000") },
                    { new Guid("e0150000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0150000-0000-0000-0000-000000000000") },
                    { new Guid("e0160000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0160000-0000-0000-0000-000000000000") },
                    { new Guid("e0160000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0160000-0000-0000-0000-000000000000") },
                    { new Guid("e0170000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0170000-0000-0000-0000-000000000000") },
                    { new Guid("e0170000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0170000-0000-0000-0000-000000000000") },
                    { new Guid("e0180000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.94999999999999996, "certificate", "Cert body database", new Guid("d0180000-0000-0000-0000-000000000000") },
                    { new Guid("e0180000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.88, "ingredient", "Label analysis", new Guid("d0180000-0000-0000-0000-000000000000") },
                    { new Guid("e0190000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0190000-0000-0000-0000-000000000000") },
                    { new Guid("e0190000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0190000-0000-0000-0000-000000000000") },
                    { new Guid("e0200000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0200000-0000-0000-0000-000000000000") },
                    { new Guid("e0200000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0200000-0000-0000-0000-000000000000") },
                    { new Guid("e0210000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0210000-0000-0000-0000-000000000000") },
                    { new Guid("e0210000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0210000-0000-0000-0000-000000000000") },
                    { new Guid("e0220000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0220000-0000-0000-0000-000000000000") },
                    { new Guid("e0220000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0220000-0000-0000-0000-000000000000") },
                    { new Guid("e0230000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0230000-0000-0000-0000-000000000000") },
                    { new Guid("e0230000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0230000-0000-0000-0000-000000000000") },
                    { new Guid("e0240000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0240000-0000-0000-0000-000000000000") },
                    { new Guid("e0240000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0240000-0000-0000-0000-000000000000") },
                    { new Guid("e0250000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.40000000000000002, "certificate", "Cert body database", new Guid("d0250000-0000-0000-0000-000000000000") },
                    { new Guid("e0250000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 12, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.55000000000000004, "ingredient", "Label analysis", new Guid("d0250000-0000-0000-0000-000000000000") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                table: "Brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CustomerId_ProductId",
                table: "CartItems",
                columns: new[] { "CustomerId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateNumber",
                table: "Certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductId",
                table: "Certificates",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificatesOnChain_CertId",
                table: "CertificatesOnChain",
                column: "CertId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificatesOnChain_ProductId",
                table: "CertificatesOnChain",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationBodies_Country",
                table: "CertificationBodies",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationBodies_Slug",
                table: "CertificationBodies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChainTxOutbox_Fingerprint",
                table: "ChainTxOutbox",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_ChainTxOutbox_Status_SubmittedAt",
                table: "ChainTxOutbox",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChainTxOutbox_TxHash",
                table: "ChainTxOutbox",
                column: "TxHash");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso3",
                table: "Countries",
                column: "Iso3",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Slug",
                table: "Departments",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_SortOrder",
                table: "Departments",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_VendorId_Country",
                table: "Facilities",
                columns: new[] { "VendorId", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_HalalVerifications_CertificateId",
                table: "HalalVerifications",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_HalalVerifications_ProductId",
                table: "HalalVerifications",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VendorOrderId",
                table: "OrderItems",
                column: "VendorOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ProductId",
                table: "ProductEmbeddings",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId_Price",
                table: "Products",
                columns: new[] { "ProductTypeId", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_VendorId",
                table: "Products",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductsOnChain_ProductId",
                table: "ProductsOnChain",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductsOnChain_SupplierId",
                table: "ProductsOnChain",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuppliersOnChain_SupplierId",
                table: "SuppliersOnChain",
                column: "SupplierId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCategories_DepartmentId_Slug",
                table: "TaxonomyCategories",
                columns: new[] { "DepartmentId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyCategories_Slug",
                table: "TaxonomyCategories",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProductTypes_PathSlug",
                table: "TaxonomyProductTypes",
                column: "PathSlug");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProductTypes_Slug",
                table: "TaxonomyProductTypes",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyProductTypes_SubcategoryId_Slug",
                table: "TaxonomyProductTypes",
                columns: new[] { "SubcategoryId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomySubcategories_CategoryId_Slug",
                table: "TaxonomySubcategories",
                columns: new[] { "CategoryId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomySubcategories_Slug",
                table: "TaxonomySubcategories",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_TraceabilityEventsOnChain_ProductId_TimestampUnix",
                table: "TraceabilityEventsOnChain",
                columns: new[] { "ProductId", "TimestampUnix" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorOrders_OrderId",
                table: "VendorOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOrders_VendorId",
                table: "VendorOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Slug",
                table: "Vendors",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationAudits_VerificationId",
                table: "VerificationAudits",
                column: "VerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationEvidence_VerificationId",
                table: "VerificationEvidence",
                column: "VerificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CertificatesOnChain");

            migrationBuilder.DropTable(
                name: "CertificationBodies");

            migrationBuilder.DropTable(
                name: "ChainTxOutbox");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProductEmbeddings");

            migrationBuilder.DropTable(
                name: "ProductsOnChain");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "SuppliersOnChain");

            migrationBuilder.DropTable(
                name: "TraceabilityEventsOnChain");

            migrationBuilder.DropTable(
                name: "VerificationAudits");

            migrationBuilder.DropTable(
                name: "VerificationEvidence");

            migrationBuilder.DropTable(
                name: "VendorOrders");

            migrationBuilder.DropTable(
                name: "HalalVerifications");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TaxonomyProductTypes");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "TaxonomySubcategories");

            migrationBuilder.DropTable(
                name: "TaxonomyCategories");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
